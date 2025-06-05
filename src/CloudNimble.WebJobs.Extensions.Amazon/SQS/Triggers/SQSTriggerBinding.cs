// Copyright (c) CloudNimble, Inc. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Amazon.SQS.Model;
using CloudNimble.EasyAF.Core;
using CloudNimble.WebJobs.Extensions.Amazon.SQS.Listeners;
using CloudNimble.WebJobs.Extensions.Common.Converters;
using CloudNimble.WebJobs.Extensions.Common.Queues;
using CloudNimble.WebJobs.Extensions.Common.Triggers;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Azure.WebJobs.Host.Bindings;
using Microsoft.Azure.WebJobs.Host.Listeners;
using Microsoft.Azure.WebJobs.Host.Protocols;
using Microsoft.Azure.WebJobs.Host.Scale;
using Microsoft.Azure.WebJobs.Host.Timers;
using Microsoft.Azure.WebJobs.Host.Triggers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CloudNimble.WebJobs.Extensions.Amazon.SQS.Triggers
{

    /// <summary>
    /// Provides trigger binding capabilities for Amazon SQS queues in the WebJobs framework.
    /// Handles the binding of SQS messages to function parameters and the creation of listeners for message processing.
    /// </summary>
    internal class SQSTriggerBinding : ITriggerBinding
    {

        #region Fields

        private readonly string _parameterName;
        private readonly SQSQueue _queue;
        private readonly ITriggerDataArgumentBinding<SQSMessage> _argumentBinding;
        private readonly IReadOnlyDictionary<string, Type> _bindingDataContract;
        private readonly QueuesOptionsBase _queueOptions;
        private readonly IWebJobsExceptionHandler _exceptionHandler;
        private readonly SharedQueueWatcher _messageEnqueuedWatcherSetter;
        private readonly ILoggerFactory _loggerFactory;
        private readonly ILogger<SQSTriggerBinding> _logger;
        private readonly IObjectToTypeConverter<SQSMessage> _converter;
        private readonly IQueueProcessorFactory _queueProcessorFactory;
        private readonly QueueMessageCausalityManager _queueCausalityManager;
        private readonly IQueueRequestExceptionClassifier _exceptionClassifier;
        private readonly ConcurrencyManager _concurrencyManager;
        private readonly IDrainModeManager _drainModeManager;
        private readonly IOptions<SQSOptions> _sqsOptions;

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="SQSTriggerBinding"/> class.
        /// </summary>
        /// <param name="parameterName">The name of the parameter being bound.</param>
        /// <param name="queue">The SQS queue to bind to.</param>
        /// <param name="argumentBinding">The argument binding for converting SQS messages to the target parameter type.</param>
        /// <param name="queueOptions">The queue processing options and configuration.</param>
        /// <param name="exceptionHandler">The handler for managing unhandled exceptions.</param>
        /// <param name="messageEnqueuedWatcherSetter">The watcher for message enqueue notifications.</param>
        /// <param name="loggerFactory">The factory for creating loggers.</param>
        /// <param name="queueProcessorFactory">The factory for creating queue processors.</param>
        /// <param name="queueCausalityManager">The manager for tracking message causality.</param>
        /// <param name="exceptionClassifier">The classifier for determining exception types and handling strategies.</param>
        /// <param name="concurrencyManager">The manager for controlling function execution concurrency.</param>
        /// <param name="drainModeManager">The manager for handling graceful shutdown and drain mode operations.</param>
        /// <param name="sqsOptions">The SQS-specific configuration options.</param>
        /// <exception cref="ArgumentNullException">Thrown when any required parameter is null.</exception>
        public SQSTriggerBinding(
            string parameterName,
            SQSQueue queue,
            ITriggerDataArgumentBinding<SQSMessage> argumentBinding,
            QueuesOptionsBase queueOptions,
            IWebJobsExceptionHandler exceptionHandler,
            SharedQueueWatcher messageEnqueuedWatcherSetter,
            ILoggerFactory loggerFactory,
            IQueueProcessorFactory queueProcessorFactory,
            QueueMessageCausalityManager queueCausalityManager,
            IQueueRequestExceptionClassifier exceptionClassifier,
            ConcurrencyManager concurrencyManager,
            IDrainModeManager drainModeManager,
            IOptions<SQSOptions> sqsOptions)
        {
            _queue = queue ?? throw new ArgumentNullException(nameof(queue));
            _argumentBinding = argumentBinding ?? throw new ArgumentNullException(nameof(argumentBinding));
            _bindingDataContract = CreateBindingDataContract((Dictionary<string, Type>)argumentBinding.BindingDataContract);
            _queueOptions = queueOptions ?? throw new ArgumentNullException(nameof(queueOptions));
            _exceptionHandler = exceptionHandler ?? throw new ArgumentNullException(nameof(exceptionHandler));
            _messageEnqueuedWatcherSetter = messageEnqueuedWatcherSetter ?? throw new ArgumentNullException(nameof(messageEnqueuedWatcherSetter));
            _queueCausalityManager = queueCausalityManager ?? throw new ArgumentNullException(nameof(queueCausalityManager));
            _exceptionClassifier = exceptionClassifier ?? throw new ArgumentNullException(nameof(exceptionClassifier));
            _concurrencyManager = concurrencyManager ?? throw new ArgumentNullException(nameof(concurrencyManager));
            _sqsOptions = sqsOptions ?? throw new ArgumentNullException(nameof(sqsOptions));

            _parameterName = parameterName;
            _loggerFactory = loggerFactory;
            _queueProcessorFactory = queueProcessorFactory;
            _converter = CreateConverter(_queue);
            _logger = loggerFactory.CreateLogger<SQSTriggerBinding>();
            _drainModeManager = drainModeManager;
        }

        #endregion

        /// <summary>
        /// Gets the type of the trigger value.
        /// </summary>
        public Type TriggerValueType => typeof(SQSMessage);

        /// <summary>
        /// Gets the binding data contract that defines the available binding data for this trigger.
        /// </summary>
        public IReadOnlyDictionary<string, Type> BindingDataContract => _bindingDataContract;

        /// <summary>
        /// Gets the name of the queue being monitored by this trigger.
        /// </summary>
        public string QueueName => _queue.Name;

        /// <summary>
        /// Creates the binding data contract for the SQS trigger.
        /// </summary>
        /// <param name="argumentBindingContract">The argument binding contract from the parameter binding.</param>
        /// <returns>A dictionary containing the binding data contract with SQS-specific properties.</returns>
        private static Dictionary<string, Type> CreateBindingDataContract(Dictionary<string, Type> argumentBindingContract)
        {
            var contract = new Dictionary<string, Type>(StringComparer.OrdinalIgnoreCase)
            {
                { "QueueTrigger", typeof(string) },
                { "DequeueCount", typeof(int) },
                { "ExpirationTime", typeof(DateTimeOffset) },
                { "Id", typeof(string) },
                { "InsertionTime", typeof(DateTimeOffset) },
                { "NextVisibleTime", typeof(DateTimeOffset) },
                { "PopReceipt", typeof(string) }
            };

            if (argumentBindingContract != null)
            {
                foreach (KeyValuePair<string, Type> item in argumentBindingContract)
                {
                    // In case of conflict, binding data from the value type overrides the built-in binding data above.
                    contract[item.Key] = item.Value;
                }
            }

            return contract;
        }

        /// <summary>
        /// Creates a converter for transforming objects to SQS messages.
        /// </summary>
        /// <param name="queue">The SQS queue for context.</param>
        /// <returns>An object-to-type converter for SQS messages.</returns>
        private static IObjectToTypeConverter<SQSMessage> CreateConverter(SQSQueue queue)
        {
            return new CompositeObjectToTypeConverter<SQSMessage>(
                new OutputConverter<SQSMessage>(new IdentityConverter<SQSMessage>()),
                new OutputConverter<string>(new StringToSQSMessageConverter())
            );
        }

        /// <summary>
        /// Binds the specified value to trigger data for function execution.
        /// </summary>
        /// <param name="value">The value to bind (typically an SQS message).</param>
        /// <param name="context">The value binding context.</param>
        /// <returns>A task that returns trigger data containing the bound value and binding data.</returns>
        /// <exception cref="InvalidOperationException">Thrown when the value cannot be converted to an SQS message.</exception>
        public async Task<ITriggerData> BindAsync(object value, ValueBindingContext context)
        {
            if (!_converter.TryConvert(value, out SQSMessage message))
            {
                throw new InvalidOperationException($"Unable to convert trigger value of type '{value?.GetType().Name ?? "null"}' to SQSMessage.");
            }

            var triggerData = await _argumentBinding.BindAsync(message, context).ConfigureAwait(false);
            var bindingData = CreateBindingData(message, triggerData.BindingData);

            return new TriggerData(triggerData.ValueProvider, bindingData);
        }

        /// <summary>
        /// Creates a listener for processing SQS messages.
        /// </summary>
        /// <param name="context">The listener factory context containing execution information.</param>
        /// <returns>A task that returns a configured SQS listener.</returns>
        /// <exception cref="ArgumentNullException">Thrown when context is null.</exception>
        public Task<IListener> CreateListenerAsync(ListenerFactoryContext context)
        {
            Ensure.ArgumentNotNull(context, nameof(context));

            var factory = new SQSListenerFactory(
                _queue,
                _queueOptions,
                _exceptionHandler,
                _messageEnqueuedWatcherSetter,
                _loggerFactory,
                context.Executor,
                _queueProcessorFactory,
                _queueCausalityManager,
                context.Descriptor,
                _concurrencyManager,
                _drainModeManager,
                _exceptionClassifier,
                _sqsOptions);

            return factory.CreateAsync(context.CancellationToken);
        }

        /// <summary>
        /// Creates a parameter descriptor for this trigger binding.
        /// </summary>
        /// <returns>A parameter descriptor containing metadata about the SQS trigger.</returns>
        public ParameterDescriptor ToParameterDescriptor()
        {
            return new SQSTriggerParameterDescriptor
            {
                Name = _parameterName,
                //AccountName = _queue.ServiceClient.GetAccountName(),
                QueueName = _queue.Name
            };
        }

        /// <summary>
        /// Creates binding data from an SQS message and additional binding data.
        /// </summary>
        /// <param name="sqsMessage">The SQS message to extract binding data from.</param>
        /// <param name="bindingDataFromValueType">Additional binding data from the value type.</param>
        /// <returns>A dictionary containing all binding data for the function execution.</returns>
        private static Dictionary<string, object> CreateBindingData(SQSMessage sqsMessage,
            IReadOnlyDictionary<string, object> bindingDataFromValueType)
        {
            var bindingData = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

            string queueMessageString = sqsMessage.Body;

            // Don't provide the QueueTrigger binding data when the queue message is not a valid string.
            if (queueMessageString != null)
            {
                bindingData.Add("QueueTrigger", queueMessageString);
            }

            bindingData.Add("DequeueCount",     sqsMessage.DequeueCount);
            //bindingData.Add("ExpirationTime",   sqsMessage.ExpirationTime.GetValueOrDefault(DateTimeOffset.MaxValue));
            bindingData.Add("Id",               sqsMessage.Id);
            bindingData.Add("InsertionTime",    sqsMessage.DateInserted.GetValueOrDefault(DateTimeOffset.UtcNow));
            //bindingData.Add("NextVisibleTime",  sqsMessage.NextVisibleTime.GetValueOrDefault(DateTimeOffset.MaxValue));
            bindingData.Add("PopReceipt",       sqsMessage.PopReceipt);

            if (bindingDataFromValueType != null)
            {
                foreach (KeyValuePair<string, object> item in bindingDataFromValueType)
                {
                    // In case of conflict, binding data from the value type overrides the built-in binding data above.
                    bindingData[item.Key] = item.Value;
                }
            }

            return bindingData;
        }
    }
}
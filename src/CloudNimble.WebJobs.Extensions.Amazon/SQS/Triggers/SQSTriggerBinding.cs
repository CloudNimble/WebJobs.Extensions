// Copyright (c) CloudNimble, Inc. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Amazon.SQS.Model;
using CloudNimble.EasyAF.Core;
using CloudNimble.WebJobs.Extensions.Amazon.SQS.Listeners;
using CloudNimble.WebJobs.Extensions.Common.Converters;
using CloudNimble.WebJobs.Extensions.Common.Queues;
using CloudNimble.WebJobs.Extensions.Common.Triggers;
using Microsoft.Azure.WebJobs.Extensions.Storage.Queues.Triggers;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Azure.WebJobs.Host.Bindings;
using Microsoft.Azure.WebJobs.Host.Listeners;
using Microsoft.Azure.WebJobs.Host.Protocols;
using Microsoft.Azure.WebJobs.Host.Scale;
using Microsoft.Azure.WebJobs.Host.Timers;
using Microsoft.Azure.WebJobs.Host.Triggers;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CloudNimble.WebJobs.Extensions.Amazon.SQS.Triggers
{

    /// <summary>
    /// 
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

        #endregion

        #region Constructors

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
            IDrainModeManager drainModeManager)
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

            _parameterName = parameterName;
            _loggerFactory = loggerFactory;
            _queueProcessorFactory = queueProcessorFactory;
            _converter = CreateConverter(_queue);
            _logger = loggerFactory.CreateLogger<SQSTriggerBinding>();
            _drainModeManager = drainModeManager;
        }

        #endregion

        /// <summary>
        /// 
        /// </summary>
        public Type TriggerValueType => typeof(Message);

        /// <summary>
        /// 
        /// </summary>
        public IReadOnlyDictionary<string, Type> BindingDataContract => _bindingDataContract;

        /// <summary>
        /// 
        /// </summary>
        public string QueueName => _queue.Name;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="argumentBindingContract"></param>
        /// <returns></returns>
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

        private static IObjectToTypeConverter<SQSMessage> CreateConverter(SQSQueue queue)
        {
            return new CompositeObjectToTypeConverter<SQSMessage>(
                new OutputConverter<SQSMessage>(new IdentityConverter<SQSMessage>()),
                new OutputConverter<string>(new StringToSQSMessageConverter())
            );
        }

        public async Task<ITriggerData> BindAsync(object value, ValueBindingContext context)
        {
            if (!_converter.TryConvert(value, out SQSMessage message))
            {
                throw new InvalidOperationException("Unable to convert trigger to IStorageQueueMessage.");
            }

            var triggerData = await _argumentBinding.BindAsync(message, context).ConfigureAwait(false);
            var bindingData = CreateBindingData(message, triggerData.BindingData);

            return new TriggerData(triggerData.ValueProvider, bindingData);
        }

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
                _exceptionClassifier);

            return factory.CreateAsync(context.CancellationToken);
        }

        public ParameterDescriptor ToParameterDescriptor()
        {
            return new SQSTriggerParameterDescriptor
            {
                Name = _parameterName,
                //AccountName = _queue.ServiceClient.GetAccountName(),
                QueueName = _queue.Name
            };
        }

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

// Copyright (c) CloudNimble, Inc. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Amazon.SQS;
using CloudNimble.WebJobs.Extensions.Common;
using CloudNimble.WebJobs.Extensions.Common.Queues;
using CloudNimble.WebJobs.Extensions.Common.Triggers;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Azure.WebJobs.Host.Scale;
using Microsoft.Azure.WebJobs.Host.Timers;
using Microsoft.Azure.WebJobs.Host.Triggers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Threading.Tasks;

namespace CloudNimble.WebJobs.Extensions.Amazon.SQS.Triggers
{

    /// <summary>
    /// Provides binding for SQS trigger attributes, handling the creation of trigger bindings for Amazon SQS queues.
    /// </summary>
    internal class SQSTriggerAttributeBindingProvider : ITriggerBindingProvider
    {

        #region Private Fields

        private readonly ConcurrencyManager _concurrencyManager;
        private readonly IDrainModeManager _drainModeManager;
        private readonly IQueueRequestExceptionClassifier _exceptionClassifier;
        private readonly IWebJobsExceptionHandler _exceptionHandler;
        private readonly IQueueTriggerArgumentBindingProvider _innerProvider;
        private readonly ILoggerFactory _loggerFactory;
        private readonly SharedQueueWatcher _messageEnqueuedWatcherSetter;
        private readonly INameResolver _nameResolver;
        private readonly QueueMessageCausalityManager _queueCausalityManager;
        private readonly QueuesOptionsBase _queueOptions;
        private readonly IQueueProcessorFactory _queueProcessorFactory;
        private readonly IAmazonSQS _sqsClient;

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="SQSTriggerAttributeBindingProvider"/> class.
        /// </summary>
        /// <param name="nameResolver">The name resolver for resolving queue names.</param>
        /// <param name="queueOptions">The queue configuration options.</param>
        /// <param name="exceptionHandler">The exception handler for managing unhandled exceptions.</param>
        /// <param name="messageEnqueuedWatcherSetter">The watcher for message enqueue notifications.</param>
        /// <param name="loggerFactory">The factory for creating loggers.</param>
        /// <param name="queueProcessorFactory">The factory for creating queue processors.</param>
        /// <param name="queueCausalityManager">The manager for tracking message causality.</param>
        /// <param name="exceptionClassifier">The classifier for SQS exceptions.</param>
        /// <param name="concurrencyManager">The manager for controlling function concurrency.</param>
        /// <param name="drainModeManager">The manager for handling drain mode operations.</param>
        /// <param name="sqsClient">The Amazon SQS client for queue operations.</param>
        /// <exception cref="ArgumentNullException">Thrown when any required parameter is null.</exception>
        public SQSTriggerAttributeBindingProvider(
            INameResolver nameResolver,
            IOptions<QueuesOptionsBase> queueOptions,
            IWebJobsExceptionHandler exceptionHandler,
            SharedQueueWatcher messageEnqueuedWatcherSetter,
            ILoggerFactory loggerFactory,
            IQueueProcessorFactory queueProcessorFactory,
            QueueMessageCausalityManager queueCausalityManager,
            IQueueRequestExceptionClassifier exceptionClassifier,
            ConcurrencyManager concurrencyManager,
            IDrainModeManager drainModeManager,
            IAmazonSQS sqsClient)
        {
            ArgumentNullException.ThrowIfNull(queueOptions);
            ArgumentNullException.ThrowIfNull(exceptionHandler);
            ArgumentNullException.ThrowIfNull(messageEnqueuedWatcherSetter);
            ArgumentNullException.ThrowIfNull(queueCausalityManager);
            ArgumentNullException.ThrowIfNull(exceptionClassifier);
            ArgumentNullException.ThrowIfNull(concurrencyManager);
            ArgumentNullException.ThrowIfNull(drainModeManager);
            ArgumentNullException.ThrowIfNull(sqsClient);

            _nameResolver = nameResolver;
            _queueOptions = queueOptions.Value;
            _exceptionHandler = exceptionHandler;
            _messageEnqueuedWatcherSetter = messageEnqueuedWatcherSetter;
            _loggerFactory = loggerFactory;
            _queueProcessorFactory = queueProcessorFactory;
            _queueCausalityManager = queueCausalityManager;
            _exceptionClassifier = exceptionClassifier;
            _concurrencyManager = concurrencyManager;
            _drainModeManager = drainModeManager;
            _sqsClient = sqsClient;

            _innerProvider = new CompositeQueueTriggerArgumentBindingProvider(
                new ConverterArgumentBindingProvider<SQSMessage>(new SQSMessageDirectConverter(), loggerFactory),
                new ConverterArgumentBindingProvider<string>(new SQSMessageToStringConverter(), loggerFactory),
                new ConverterArgumentBindingProvider<ParameterBindingData>(new SQSMessageToParameterBindingDataConverter(), loggerFactory),
                new UserTypeArgumentBindingProvider(loggerFactory));
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Tries to create a trigger binding for the specified context.
        /// </summary>
        /// <param name="context">The trigger binding provider context containing parameter information.</param>
        /// <returns>A task that returns the trigger binding if successful; otherwise, null.</returns>
        /// <exception cref="InvalidOperationException">Thrown when the parameter type cannot be bound to a queue trigger.</exception>
        /// <example>
        /// <code>
        /// // This method is typically called by the WebJobs runtime when processing:
        /// [SQSTrigger("my-queue")] string message
        /// </code>
        /// </example>
        public Task<ITriggerBinding> TryCreateAsync(TriggerBindingProviderContext context)
        {
            var parameter = context.Parameter;
            var queueTrigger = TypeUtility.GetResolvedAttribute<SQSTriggerAttribute>(context.Parameter);

            if (queueTrigger is null)
            {
                return Task.FromResult<ITriggerBinding>(null);
            }

            var queueName = Resolve(queueTrigger.QueueName);
            queueName = NormalizeAndValidate(queueName);

            var argumentBinding = _innerProvider.TryCreate(parameter)
                ?? throw new InvalidOperationException($"Can't bind QueueTrigger to type '{parameter.ParameterType}'.");

            var queue = new SQSQueue(queueName, _sqsClient, _loggerFactory);

            var binding = new SQSTriggerBinding(
                parameter.Name,
                queue,
                (ITriggerDataArgumentBinding<SQSMessage>)argumentBinding,
                _queueOptions,
                _exceptionHandler,
                _messageEnqueuedWatcherSetter,
                _loggerFactory,
                _queueProcessorFactory,
                _queueCausalityManager,
                _exceptionClassifier,
                _concurrencyManager,
                _drainModeManager);

            return Task.FromResult<ITriggerBinding>(binding);
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Normalizes and validates the queue name according to SQS requirements.
        /// </summary>
        /// <param name="queueName">The queue name to normalize and validate.</param>
        /// <returns>The normalized and validated queue name.</returns>
        private static string NormalizeAndValidate(string queueName)
        {
            queueName = queueName.ToLowerInvariant();
            SQSQueue.ValidateQueueName(queueName);

            return queueName;
        }

        /// <summary>
        /// Resolves the queue name using the configured name resolver.
        /// </summary>
        /// <param name="queueName">The queue name to resolve.</param>
        /// <returns>The resolved queue name.</returns>
        private string Resolve(string queueName)
        {
            if (_nameResolver is null)
            {
                return queueName;
            }

            return _nameResolver.ResolveWholeString(queueName);
        }

        #endregion

    }

}
// Copyright (c) CloudNimble, Inc. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Amazon.SQS;
using CloudNimble.WebJobs.Extensions.Amazon.SQS;
using CloudNimble.WebJobs.Extensions.Amazon.SQS.Triggers;
using CloudNimble.WebJobs.Extensions.Common;
using CloudNimble.WebJobs.Extensions.Common.Queues;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Azure.WebJobs.Host.Executors;
using Microsoft.Azure.WebJobs.Host.Listeners;
using Microsoft.Azure.WebJobs.Host.Protocols;
using Microsoft.Azure.WebJobs.Host.Scale;
using Microsoft.Azure.WebJobs.Host.Timers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace CloudNimble.WebJobs.Extensions.Amazon.SQS.Listeners
{

    /// <summary>
    /// Factory for creating SQS listeners that handle message processing from Amazon SQS queues.
    /// Manages the setup of listeners, poison queues, and queue processors.
    /// </summary>
    internal class SQSListenerFactory : IListenerFactory
    {

        #region Private Fields

        private static readonly string PoisonQueueSuffix = "-poison";

        private readonly ConcurrencyManager _concurrencyManager;
        private readonly FunctionDescriptor _descriptor;
        private readonly IDrainModeManager _drainModeManager;
        private readonly IQueueRequestExceptionClassifier _exceptionClassifier;
        private readonly IWebJobsExceptionHandler _exceptionHandler;
        private readonly ITriggeredFunctionExecutor _executor;
        private readonly ILoggerFactory _loggerFactory;
        private readonly SharedQueueWatcher _messageEnqueuedWatcherSetter;
        private readonly SQSQueue _poisonQueue;
        private readonly SQSQueue _queue;
        private readonly QueueMessageCausalityManager _queueCausalityManager;
        private readonly QueuesOptionsBase _queueOptions;
        private readonly IQueueProcessorFactory _queueProcessorFactory;
        private readonly IOptions<SQSOptions> _sqsOptions;

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="SQSListenerFactory"/> class.
        /// </summary>
        /// <param name="queue">The SQS queue to create a listener for.</param>
        /// <param name="queueOptions">The queue processing options and configuration.</param>
        /// <param name="exceptionHandler">The handler for managing unhandled exceptions.</param>
        /// <param name="messageEnqueuedWatcherSetter">The watcher for message enqueue notifications.</param>
        /// <param name="loggerFactory">The factory for creating loggers.</param>
        /// <param name="executor">The executor for running triggered functions.</param>
        /// <param name="queueProcessorFactory">The factory for creating queue processors.</param>
        /// <param name="queueCausalityManager">The manager for tracking message causality.</param>
        /// <param name="descriptor">The function descriptor providing metadata about the triggered function.</param>
        /// <param name="concurrencyManager">The manager for controlling function execution concurrency.</param>
        /// <param name="drainModeManager">The manager for handling graceful shutdown and drain mode operations.</param>
        /// <param name="exceptionClassifier">The classifier for determining exception types and handling strategies.</param>
        /// <param name="sqsOptions">The SQS-specific configuration options.</param>
        /// <exception cref="ArgumentNullException">Thrown when any required parameter is null.</exception>
        /// <example>
        /// <code>
        /// var factory = new SQSListenerFactory(
        ///     sqsQueue,
        ///     queueOptions,
        ///     exceptionHandler,
        ///     messageWatcher,
        ///     loggerFactory,
        ///     functionExecutor,
        ///     queueProcessorFactory,
        ///     causalityManager,
        ///     functionDescriptor,
        ///     concurrencyManager,
        ///     drainModeManager,
        ///     exceptionClassifier,
        ///     sqsOptions);
        /// 
        /// var listener = await factory.CreateAsync();
        /// </code>
        /// </example>
        public SQSListenerFactory(
            SQSQueue queue,
            QueuesOptionsBase queueOptions,
            IWebJobsExceptionHandler exceptionHandler,
            SharedQueueWatcher messageEnqueuedWatcherSetter,
            ILoggerFactory loggerFactory,
            ITriggeredFunctionExecutor executor,
            IQueueProcessorFactory queueProcessorFactory,
            QueueMessageCausalityManager queueCausalityManager,
            FunctionDescriptor descriptor,
            ConcurrencyManager concurrencyManager,
            IDrainModeManager drainModeManager,
            IQueueRequestExceptionClassifier exceptionClassifier,
            IOptions<SQSOptions> sqsOptions)
        {
            ArgumentNullException.ThrowIfNull(queue);
            ArgumentNullException.ThrowIfNull(queueOptions);
            ArgumentNullException.ThrowIfNull(exceptionHandler);
            ArgumentNullException.ThrowIfNull(messageEnqueuedWatcherSetter);
            ArgumentNullException.ThrowIfNull(executor);
            ArgumentNullException.ThrowIfNull(queueCausalityManager);
            ArgumentNullException.ThrowIfNull(descriptor);
            ArgumentNullException.ThrowIfNull(concurrencyManager);
            ArgumentNullException.ThrowIfNull(exceptionClassifier);
            ArgumentNullException.ThrowIfNull(sqsOptions);

            _queue = queue;
            _queueOptions = queueOptions;
            _exceptionHandler = exceptionHandler;
            _messageEnqueuedWatcherSetter = messageEnqueuedWatcherSetter;
            _loggerFactory = loggerFactory;
            _executor = executor;
            _queueProcessorFactory = queueProcessorFactory;
            _queueCausalityManager = queueCausalityManager;
            _descriptor = descriptor;
            _concurrencyManager = concurrencyManager;
            _drainModeManager = drainModeManager;
            _exceptionClassifier = exceptionClassifier;
            _sqsOptions = sqsOptions;

            // Create poison queue reference if applicable
            _poisonQueue = CreatePoisonQueueReference(_queue);
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Creates an SQS listener instance configured for message processing.
        /// </summary>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>A task that represents the asynchronous creation operation. The task result contains the configured listener.</returns>
        /// <example>
        /// <code>
        /// var listener = await factory.CreateAsync();
        /// await listener.StartAsync(CancellationToken.None);
        /// 
        /// // Process messages...
        /// 
        /// await listener.StopAsync(CancellationToken.None);
        /// </code>
        /// </example>
        public async Task<IListener> CreateAsync(CancellationToken cancellationToken = default)
        {
            // Create the trigger executor with proper interface implementation
            var triggerExecutor = new SQSTriggerExecutor(_executor, _queueCausalityManager);

            // Create queue processor with proper setup
            var queueProcessor = CreateQueueProcessor(
                _queue,
                _loggerFactory,
                _queueProcessorFactory,
                _queueOptions,
                _messageEnqueuedWatcherSetter,
                _exceptionClassifier);

            // Create listener with all required parameters
            var listener = new SQSListener(
                _queue,
                _poisonQueue,
                triggerExecutor,
                _exceptionHandler,
                _loggerFactory,
                _messageEnqueuedWatcherSetter,
                _queueOptions,
                queueProcessor,
                _descriptor,
                _exceptionClassifier,
                _concurrencyManager,
                _drainModeManager);

            return await Task.FromResult(listener);
        }

        #endregion

        #region Internal Methods

        /// <summary>
        /// Creates a queue processor for handling message lifecycle operations.
        /// </summary>
        /// <param name="queue">The SQS queue to create a processor for.</param>
        /// <param name="loggerFactory">The factory for creating loggers.</param>
        /// <param name="queueProcessorFactory">The factory for creating queue processors.</param>
        /// <param name="queuesOptions">The queue processing options.</param>
        /// <param name="sharedWatcher">The shared watcher for message enqueue notifications.</param>
        /// <param name="exceptionClassifier">The classifier for exception handling.</param>
        /// <returns>A configured queue processor instance.</returns>
        /// <remarks>
        /// This method handles both host-level control queues and application queues differently.
        /// Host queues use the default processor while application queues delegate to the factory.
        /// </remarks>
        internal static QueueProcessor CreateQueueProcessor(
            SQSQueue queue,
            ILoggerFactory loggerFactory,
            IQueueProcessorFactory queueProcessorFactory,
            QueuesOptionsBase queuesOptions,
            IMessageEnqueuedWatcher sharedWatcher,
            IQueueRequestExceptionClassifier exceptionClassifier)
        {
            var context = new QueueProcessorOptions(queue, loggerFactory, queuesOptions, queue);

            QueueProcessor queueProcessor;
            if (HostQueueNames.IsHostQueue(queue.Name))
            {
                // We only delegate to the processor factory for application queues,
                // not our built in control queues
                queueProcessor = new QueueProcessor(context, exceptionClassifier);
            }
            else
            {
                queueProcessor = queueProcessorFactory.Create(context);
            }

            QueueListener.RegisterSharedWatcherWithQueueProcessor(queueProcessor, sharedWatcher);

            return queueProcessor;
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Creates a reference to the poison queue for the given source queue.
        /// </summary>
        /// <param name="sourceQueue">The source queue to create a poison queue reference for.</param>
        /// <returns>A poison queue reference, or null if no poison queue is applicable.</returns>
        /// <remarks>
        /// Only creates a poison queue if:
        /// 1. The poison queue name would be valid (adding "-poison" doesn't make the name too long)
        /// 2. The queue itself isn't already a poison queue
        /// </remarks>
        private SQSQueue CreatePoisonQueueReference(SQSQueue sourceQueue)
        {
            // Only use a corresponding poison queue if:
            // 1. The poison queue name would be valid (adding "-poison" doesn't make the name too long), and
            // 2. The queue itself isn't already a poison queue.

            if (sourceQueue?.Name is null || sourceQueue.Name.EndsWith(PoisonQueueSuffix, StringComparison.Ordinal))
            {
                return null;
            }

            string possiblePoisonQueueName = sourceQueue.Name + PoisonQueueSuffix;

            if (!SQSQueue.IsValidQueueName(possiblePoisonQueueName, out string errorMessage))
            {
                return null;
            }

            // Create poison queue with the same SQS client as the source queue
            return new SQSQueue(
                possiblePoisonQueueName,
                GetSQSClientFromQueue(sourceQueue),
                _loggerFactory,
                _sqsOptions);
        }

        /// <summary>
        /// Helper method to extract the SQS client from an existing queue instance.
        /// </summary>
        /// <param name="queue">The queue to extract the SQS client from.</param>
        /// <returns>The IAmazonSQS client instance.</returns>
        /// <exception cref="InvalidOperationException">Thrown when unable to access the SQS client from the queue.</exception>
        /// <remarks>
        /// This uses reflection to access the private _sqsClient field since SQSQueue doesn't expose its client.
        /// This is not ideal but necessary given the current class structure.
        /// </remarks>
        private static IAmazonSQS GetSQSClientFromQueue(SQSQueue queue)
        {
            // We need to access the private _sqsClient field using reflection
            // This is not ideal but necessary given the current class structure
            var clientField = typeof(SQSQueue).GetField("_sqsClient",
                BindingFlags.NonPublic | BindingFlags.Instance);

            if (clientField?.GetValue(queue) is IAmazonSQS client)
            {
                return client;
            }

            throw new InvalidOperationException("Unable to access SQS client from queue instance");
        }

        #endregion

    }

}
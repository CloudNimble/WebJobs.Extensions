// Copyright (c) CloudNimble, Inc. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Amazon.SQS;
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
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace CloudNimble.WebJobs.Extensions.Amazon.SQS.Listeners
{

    internal class SQSListenerFactory : IListenerFactory
    {

        #region Private Members

        private static string poisonQueueSuffix = "-poison";
        private readonly SQSQueue _queue;
        private readonly SQSQueue _poisonQueue;
        private readonly QueuesOptionsBase _queueOptions;
        private readonly IWebJobsExceptionHandler _exceptionHandler;
        private readonly SharedQueueWatcher _messageEnqueuedWatcherSetter;
        private readonly ILoggerFactory _loggerFactory;
        private readonly ITriggeredFunctionExecutor _executor;
        private readonly FunctionDescriptor _descriptor;
        private readonly IQueueProcessorFactory _queueProcessorFactory;
        private readonly QueueMessageCausalityManager _queueCausalityManager;
        private readonly ConcurrencyManager _concurrencyManager;
        private readonly IDrainModeManager _drainModeManager;
        private readonly IQueueRequestExceptionClassifier _exceptionClassifier;

        #endregion

        #region Constructors

        /// <summary>
        /// 
        /// </summary>
        /// <param name="queue"></param>
        /// <param name="queueOptions"></param>
        /// <param name="exceptionHandler"></param>
        /// <param name="messageEnqueuedWatcherSetter"></param>
        /// <param name="loggerFactory"></param>
        /// <param name="executor"></param>
        /// <param name="queueProcessorFactory"></param>
        /// <param name="queueCausalityManager"></param>
        /// <param name="descriptor"></param>
        /// <param name="concurrencyManager"></param>
        /// <param name="drainModeManager"></param>
        /// <param name="exceptionClassifier"></param>
        /// <exception cref="ArgumentNullException"></exception>
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
            IQueueRequestExceptionClassifier exceptionClassifier)
        {
            _queue = queue ?? throw new ArgumentNullException(nameof(queue));
            _queueOptions = queueOptions ?? throw new ArgumentNullException(nameof(queueOptions));
            _exceptionHandler = exceptionHandler ?? throw new ArgumentNullException(nameof(exceptionHandler));
            _messageEnqueuedWatcherSetter = messageEnqueuedWatcherSetter ?? throw new ArgumentNullException(nameof(messageEnqueuedWatcherSetter));
            _executor = executor ?? throw new ArgumentNullException(nameof(executor));
            _descriptor = descriptor ?? throw new ArgumentNullException(nameof(descriptor));
            _concurrencyManager = concurrencyManager ?? throw new ArgumentNullException(nameof(concurrencyManager));
            _queueCausalityManager = queueCausalityManager ?? throw new ArgumentNullException(nameof(queueCausalityManager));
            _exceptionClassifier = exceptionClassifier ?? throw new ArgumentNullException(nameof(exceptionClassifier));

            _poisonQueue = queue; //CreatePoisonQueueReference(queue.ServiceClient, queue.Name);
            _loggerFactory = loggerFactory;
            _queueProcessorFactory = queueProcessorFactory;
        }

        #endregion

        public async Task<IListener> CreateAsync(CancellationToken cancellationToken = default)
        {
            var triggerExecutor = new SQSTriggerExecutor(_executor, _queueCausalityManager);

            var queueProcessor = CreateQueueProcessor(
                _queue,
                _loggerFactory,
                _queueProcessorFactory,
                _queueOptions,
                _messageEnqueuedWatcherSetter,
                _exceptionClassifier);

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
                drainModeManager: _drainModeManager
                );

            return await Task.FromResult(listener);
        }

        internal static QueueProcessor CreateQueueProcessor(SQSQueue queue, ILoggerFactory loggerFactory, IQueueProcessorFactory queueProcessorFactory,
            QueuesOptionsBase queuesOptions, IMessageEnqueuedWatcher sharedWatcher, IQueueRequestExceptionClassifier exceptionClassifier)
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

            QueueListener<SQSMessage>.RegisterSharedWatcherWithQueueProcessor(queueProcessor, sharedWatcher);

            return queueProcessor;
        }

        //private static SQSQueue CreatePoisonQueueReference(AmazonSQSClient client, string name)
        //{
        //    Debug.Assert(client is not null);

        //    // Only use a corresponding poison queue if:
        //    // 1. The poison queue name would be valid (adding "-poison" doesn't make the name too long), and
        //    // 2. The queue itself isn't already a poison queue.

        //    if (name is null || name.EndsWith(poisonQueueSuffix, StringComparison.Ordinal))
        //    {
        //        return null;
        //    }

        //    string possiblePoisonQueueName = name + poisonQueueSuffix;

        //    if (!SQSQueue.IsValidQueueName(possiblePoisonQueueName, out string errorMessage))
        //    {
        //        return null;
        //    }

        //    return client.GetQueueReference(possiblePoisonQueueName);
        //}

    }

}

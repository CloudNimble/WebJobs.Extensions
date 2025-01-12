// Copyright (c) CloudNimble, Inc. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using CloudNimble.WebJobs.Extensions.Common;
using CloudNimble.WebJobs.Extensions.Common.Queues;
using CloudNimble.WebJobs.Extensions.Common.Triggers;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Storage.Queues.Triggers;
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
    /// Provides binding for SQS trigger attributes.
    /// </summary>
    internal class SQSTriggerAttributeBindingProvider : ITriggerBindingProvider
    {
        private readonly IQueueTriggerArgumentBindingProvider _innerProvider;
        private readonly INameResolver _nameResolver;
        private readonly QueuesOptionsBase _queueOptions;
        private readonly IWebJobsExceptionHandler _exceptionHandler;
        private readonly SharedQueueWatcher _messageEnqueuedWatcherSetter;
        private readonly ILoggerFactory _loggerFactory;
        private readonly IQueueProcessorFactory _queueProcessorFactory;
        private readonly QueueMessageCausalityManager _queueCausalityManager;
        private readonly IQueueRequestExceptionClassifier _exceptionClassifier;
        private readonly ConcurrencyManager _concurrencyManager;
        private readonly IDrainModeManager _drainModeManager;

        /// <summary>
        /// Initializes a new instance of the <see cref="SQSTriggerAttributeBindingProvider"/> class.
        /// </summary>
        /// <param name="nameResolver">The name resolver.</param>
        /// <param name="queueOptions">The queue options.</param>
        /// <param name="exceptionHandler">The exception handler.</param>
        /// <param name="messageEnqueuedWatcherSetter">The message enqueued watcher setter.</param>
        /// <param name="loggerFactory">The logger factory.</param>
        /// <param name="queueProcessorFactory">The queue processor factory.</param>
        /// <param name="queueCausalityManager">The queue causality manager.</param>
        /// <param name="exceptionClassifier">The exception classifier.</param>
        /// <param name="concurrencyManager">The concurrency manager.</param>
        /// <param name="drainModeManager">The drain mode manager.</param>
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
            IDrainModeManager drainModeManager)
        {
            _nameResolver = nameResolver;
            _queueOptions = (queueOptions ?? throw new ArgumentNullException(nameof(queueOptions))).Value;
            _exceptionHandler = exceptionHandler ?? throw new ArgumentNullException(nameof(exceptionHandler));
            _messageEnqueuedWatcherSetter = messageEnqueuedWatcherSetter ?? throw new ArgumentNullException(nameof(messageEnqueuedWatcherSetter));
            _loggerFactory = loggerFactory;
            _queueProcessorFactory = queueProcessorFactory;
            _queueCausalityManager = queueCausalityManager ?? throw new ArgumentNullException(nameof(queueCausalityManager));
            _exceptionClassifier = exceptionClassifier ?? throw new ArgumentNullException(nameof(exceptionClassifier));
            _concurrencyManager = concurrencyManager ?? throw new ArgumentNullException(nameof(concurrencyManager));
            _drainModeManager = drainModeManager ?? throw new ArgumentNullException(nameof(drainModeManager));

            _innerProvider =
            new CompositeQueueTriggerArgumentBindingProvider(
                new ConverterArgumentBindingProvider<SQSMessage>(new SQSMessageDirectConverter(), loggerFactory),
                new ConverterArgumentBindingProvider<string>(new SQSMessageToStringConverter(), loggerFactory),
                new ConverterArgumentBindingProvider<ParameterBindingData>(new SQSMessageToParameterBindingDataConverter(), loggerFactory),
                new UserTypeArgumentBindingProvider(loggerFactory));
        }

        /// <summary>
        /// Tries to create a trigger binding.
        /// </summary>
        /// <param name="context">The trigger binding provider context.</param>
        /// <returns>A task that returns the trigger binding if successful; otherwise, null.</returns>
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

            var queue = new SQSQueue(queueName, null, _loggerFactory);

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

        /// <summary>
        /// Normalizes and validates the queue name.
        /// </summary>
        /// <param name="queueName">The queue name.</param>
        /// <returns>The normalized and validated queue name.</returns>
        private static string NormalizeAndValidate(string queueName)
        {
            queueName = queueName.ToLowerInvariant();
            SQSQueue.ValidateQueueName(queueName);
            return queueName;
        }

        /// <summary>
        /// Resolves the queue name using the name resolver.
        /// </summary>
        /// <param name="queueName">The queue name.</param>
        /// <returns>The resolved queue name.</returns>
        private string Resolve(string queueName)
        {
            if (_nameResolver is null)
            {
                return queueName;
            }

            return _nameResolver.ResolveWholeString(queueName);
        }
    }
}

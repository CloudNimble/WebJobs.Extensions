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
    internal class SQSTriggerAttributeBindingProvider : ITriggerBindingProvider
    {

        private readonly IQueueTriggerArgumentBindingProvider _innerProvider;
        private readonly INameResolver _nameResolver;
        //private readonly QueueServiceClientProvider _queueServiceClientProvider;
        private readonly QueuesOptionsBase _queueOptions;
        private readonly IWebJobsExceptionHandler _exceptionHandler;
        private readonly SharedQueueWatcher _messageEnqueuedWatcherSetter;
        private readonly ILoggerFactory _loggerFactory;
        private readonly IQueueProcessorFactory _queueProcessorFactory;
        private readonly QueueMessageCausalityManager _queueCausalityManager;
        private readonly ConcurrencyManager _concurrencyManager;
        private readonly IDrainModeManager _drainModeManager;

        public SQSTriggerAttributeBindingProvider(
            INameResolver nameResolver,
            //QueueServiceClientProvider queueServiceClientProvider,
            IOptions<QueuesOptionsBase> queueOptions,
            IWebJobsExceptionHandler exceptionHandler,
            SharedQueueWatcher messageEnqueuedWatcherSetter,
            ILoggerFactory loggerFactory,
            IQueueProcessorFactory queueProcessorFactory,
            QueueMessageCausalityManager queueCausalityManager,
            ConcurrencyManager concurrencyManager,
            IDrainModeManager drainModeManager)
        {
            //_queueServiceClientProvider = queueServiceClientProvider ?? throw new ArgumentNullException(nameof(queueServiceClientProvider));
            _queueOptions = (queueOptions ?? throw new ArgumentNullException(nameof(queueOptions))).Value;
            _exceptionHandler = exceptionHandler ?? throw new ArgumentNullException(nameof(exceptionHandler));
            _messageEnqueuedWatcherSetter = messageEnqueuedWatcherSetter ?? throw new ArgumentNullException(nameof(messageEnqueuedWatcherSetter));
            _queueCausalityManager = queueCausalityManager ?? throw new ArgumentNullException(nameof(queueCausalityManager));
            _concurrencyManager = concurrencyManager ?? throw new ArgumentNullException(nameof(concurrencyManager));

            _nameResolver = nameResolver;
            _loggerFactory = loggerFactory;
            _queueProcessorFactory = queueProcessorFactory;

            _innerProvider =
            new CompositeQueueTriggerArgumentBindingProvider(
                new ConverterArgumentBindingProvider<SQSMessage>(new SQSMessageDirectConverter(), loggerFactory), // $$$: Is this the best way to handle a direct CloudQueueMessage? TODO (kasobol-msft) is this needed?
                new ConverterArgumentBindingProvider<string>(new SQSMessageToStringConverter(), loggerFactory),
                new ConverterArgumentBindingProvider<ParameterBindingData>(new SQSMessageToParameterBindingDataConverter(), loggerFactory),
                new UserTypeArgumentBindingProvider(loggerFactory)); // Must come last, because it will attempt to bind all types.
        }

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

            var client = _queueServiceClientProvider.Get(queueTrigger.Connection, _nameResolver);
            var queue = client.GetQueueClient(queueName);

            var binding = new SQSTriggerBinding(
                parameter.Name,
                queue,
                (ITriggerDataArgumentBinding<SQSMessage>)argumentBinding, // Cast to the correct type
                _queueOptions,
                _exceptionHandler,
                _messageEnqueuedWatcherSetter,
                _loggerFactory,
                _queueProcessorFactory,
                _queueCausalityManager,
                _concurrencyManager,
                _drainModeManager);
            return Task.FromResult<ITriggerBinding>(binding);
        }

        private static string NormalizeAndValidate(string queueName)
        {
            queueName = queueName.ToLowerInvariant(); // must be lowercase. coerce here to be nice.
            SQSQueue.ValidateQueueName(queueName);
            return queueName;
        }

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

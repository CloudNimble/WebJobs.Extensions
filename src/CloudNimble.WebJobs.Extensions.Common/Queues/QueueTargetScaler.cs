// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Azure.WebJobs.Host.Scale;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace CloudNimble.WebJobs.Extensions.Common.Queues
{
    /// <summary>
    /// Provides queue length metrics for the <see cref="QueueTargetScaler{TQueueMessage}"/>.
    /// </summary>
    public class QueueTargetScaler<TQueueMessage> : ITargetScaler
        where TQueueMessage : IQueueMessage
    {
        private readonly string _functionId;
        private readonly string _queueName;
        private readonly QueueMetricsProvider<TQueueMessage> _queueMetricsProvider;
        private readonly TargetScalerDescriptor _targetScalerDescriptor;
        private readonly QueuesOptionsBase _options;
        private readonly ILogger _logger;

        // internal mock testing only
        internal QueueTargetScaler()
        {
        }

        /// <summary>
        /// Constructs a new instance.
        /// </summary>
        public TargetScalerDescriptor TargetScalerDescriptor => _targetScalerDescriptor;

        /// <summary>
        /// Initializes a new instance of the <see cref="QueueTargetScaler{TQueueMessage}"/> class.
        /// </summary>
        /// <param name="functionId">The function identifier.</param>
        /// <param name="queueClient">The queue client.</param>
        /// <param name="options">The queue options.</param>
        /// <param name="exceptionClassifier"></param>
        /// <param name="loggerFactory">The logger factory.</param>
        public QueueTargetScaler(string functionId, IQueueClient<TQueueMessage> queueClient, QueuesOptionsBase options, IQueueRequestExceptionClassifier exceptionClassifier,
            ILoggerFactory loggerFactory)
        {
            _functionId = functionId;
            _queueName = queueClient.Name;
            _queueMetricsProvider = new QueueMetricsProvider<TQueueMessage>(queueClient, exceptionClassifier, loggerFactory);
            _targetScalerDescriptor = new TargetScalerDescriptor(functionId);
            _options = options;
            _logger = loggerFactory.CreateLogger<QueueTargetScaler<TQueueMessage>>();
        }

        /// <summary>
        /// Makes a target scale decision based on most recent metrics for the specified queue.
        /// </summary>
        /// <param name="context">The TargetScalerContext, which contains the InstanceConcurrency, or the targetMetric used in target based scaling.</param>
        /// <returns>Returns a TargetScalerResult with a TargetWorkerCount.</returns>
        public async Task<TargetScalerResult> GetScaleResultAsync(TargetScalerContext context)
        {
            int queueLength = await _queueMetricsProvider.GetQueueLengthAsync().ConfigureAwait(false);
            return GetScaleResultInternal(context, queueLength);
        }

        internal TargetScalerResult GetScaleResultInternal(TargetScalerContext context, int queueLength)
        {
            int concurrency = context.InstanceConcurrency ?? _options.BatchSize + _options.NewBatchThreshold;

            if (concurrency < 0)
            {
                throw new ArgumentOutOfRangeException($"Concurrency value='{concurrency}' used for target based scale must be > 0.");
            }

            int targetWorkerCount = (int)Math.Ceiling(queueLength / (decimal)concurrency);

            _logger.LogInformation($"Target worker count for function '{_functionId}' is '{targetWorkerCount}' (QueueName='{_queueName}', QueueLength ='{queueLength}', Concurrency='{concurrency}').");
            return new TargetScalerResult
            {
                TargetWorkerCount = targetWorkerCount
            };
        }
    }
}

// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace CloudNimble.WebJobs.Extensions.Common.Queues
{

    /// <summary>
    /// Provides QueueTriggerMetrics from a specific queue entity.
    /// </summary>
    public class QueueMetricsProvider<TQueueMessage> : IQueueMetricsProvider
        where TQueueMessage : IQueueMessage
    {
        private readonly IQueueClient<TQueueMessage> _queue;
        private readonly IQueueRequestExceptionClassifier _exceptionClassifier;
        private readonly ILogger _logger;

        /// <summary>
        /// Instantiates a QueueMetricsProvider.
        /// </summary>
        /// <param name="queue">The QueueClient to use for metrics polling.</param>
        /// <param name="exceptionClassifier"></param>
        /// <param name="loggerFactory">Used to create an ILogger instance.</param>
        public QueueMetricsProvider(IQueueClient<TQueueMessage> queue, IQueueRequestExceptionClassifier exceptionClassifier, ILoggerFactory loggerFactory)
        {
            _queue = queue;
            _exceptionClassifier = exceptionClassifier;
            _logger = loggerFactory.CreateLogger<QueueMetricsProvider<TQueueMessage>>();

        }

        /// <summary>
        /// Retrieve queue length from the specified queue entity.
        /// </summary>
        /// <returns>The queue length from the associated queue entity.</returns>
        public async Task<int> GetQueueLengthAsync()
        {
            try
            {
                QueueTriggerMetrics queueMetrics = await GetMetricsAsync().ConfigureAwait(false);
                return queueMetrics.QueueLength;
            }
            catch (Exception ex)
            {
                LogError(ex);
            }

            return 0;
        }

        /// <summary>
        /// Retrieves metrics from the queue entity.
        /// </summary>
        /// <returns>Returns a <see cref="QueueTriggerMetrics"/> object.</returns>
        public async Task<QueueTriggerMetrics> GetMetricsAsync()
        {
            var queueLength = 0;
            var queueTime = TimeSpan.Zero;

            try
            {
                var queueProperties = await _queue.GetPropertiesAsync().ConfigureAwait(false);
                queueLength = queueProperties.ApproximateMessagesCount;

                if (queueLength > 0)
                {
                    var message = (await _queue.PeekMessagesAsync(1).ConfigureAwait(false)).Value.FirstOrDefault();
                    if (message != null)
                    {
                        if (message.DateInserted.HasValue)
                        {
                            queueTime = DateTime.UtcNow.Subtract(message.DateInserted.Value.DateTime);
                        }
                    }
                    else
                    {
                        // ApproximateMessageCount often returns a stale value,
                        // especially when the queue is empty.
                        queueLength = 0;
                    }
                }
            }
            catch (Exception ex)
            {
                LogError(ex);
            }

            return new QueueTriggerMetrics
            {
                QueueLength = queueLength,
                QueueTime = queueTime,
                Timestamp = DateTime.UtcNow
            };
        }

        private void LogError(Exception ex)
        {
            if (_exceptionClassifier.IsNotFoundException(ex) ||
                _exceptionClassifier.IsConflictException(ex) ||
                _exceptionClassifier.IsServerSideException(ex))
            {
                // ignore transient errors, and return default metrics
                // E.g. if the queue doesn't exist, we'll return a zero queue length
                // and scale in
                _logger.LogWarning($"Error querying for queue scale status: {ex}");
            }
            else
            {
                _logger.LogWarning($"Fatal error querying for queue scale status: {ex}");
            }
        }


    }

}

// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System.Threading.Tasks;

namespace CloudNimble.WebJobs.Extensions.Common.Queues
{

    /// <summary>
    /// Provides QueueTriggerMetrics from a specific queue entity.
    /// </summary>
    public interface IQueueMetricsProvider
    {

        /// <summary>
        /// Retrieves metrics from the queue entity.
        /// </summary>
        /// <returns>Returns a <see cref="QueueTriggerMetrics"/> object.</returns>
        Task<QueueTriggerMetrics> GetMetricsAsync();

        /// <summary>
        /// Retrieve queue length from the specified queue entity.
        /// </summary>
        /// <returns>The queue length from the associated queue entity.</returns>
        Task<int> GetQueueLengthAsync();

    }
}
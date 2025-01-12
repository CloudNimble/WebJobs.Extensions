// Copyright (c) CloudNimble, Inc. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Microsoft.Azure.WebJobs.Host.Scale;
using System;

namespace CloudNimble.WebJobs.Extensions.Amazon.SQS.Triggers
{

    /// <summary>
    /// Metrics type representing the status of an Amazon SQS Queue.
    /// </summary>
    internal class SQSTriggerMetrics : ScaleMetrics
    {

        /// <summary>
        /// The current length of the queue.
        /// </summary>
        public int MessageCount { get; set; }

        /// <summary>
        /// The length of time the next message in the queue has been
        /// sitting there.
        /// </summary>
        public TimeSpan QueueTime { get; set; }

    }

}

// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;

namespace CloudNimble.WebJobs.Extensions.Common.Queues
{

    /// <summary>
    /// Provides predefined polling intervals for queue processing.
    /// </summary>
    public static class QueuePollingIntervals
    {

        /// <summary>
        /// The minimum polling interval, set to 100 milliseconds.
        /// </summary>
        public static readonly TimeSpan Minimum = TimeSpan.FromMilliseconds(100);

        /// <summary>
        /// The default maximum polling interval, set to 1 minute.
        /// </summary>
        public static readonly TimeSpan DefaultMaximum = TimeSpan.FromMinutes(1);

    }
}

// Copyright (c) CloudNimble, Inc. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;

namespace CloudNimble.WebJobs.Extensions.Amazon.SQS.Listeners
{

    internal static class SQSPollingIntervals
    {
        public static readonly TimeSpan Minimum = TimeSpan.FromMilliseconds(100);
        public static readonly TimeSpan DefaultMaximum = TimeSpan.FromMinutes(1);

    }

}

// Copyright (c) CloudNimble, Inc. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using CloudNimble.WebJobs.Extensions.Common.Queues;
using System;

namespace CloudNimble.WebJobs.Extensions.Tests.Common.Models
{
    /// <summary>
    /// Test implementation of IQueueMessage for testing purposes.
    /// </summary>
    public class TestQueueMessage : IQueueMessage
    {
        public string Body { get; set; }
        public int DequeueCount { get; set; }
        public string Id { get; set; }
        public string PopReceipt { get; set; }
        public DateTimeOffset? DateInserted { get; set; }
    }

}
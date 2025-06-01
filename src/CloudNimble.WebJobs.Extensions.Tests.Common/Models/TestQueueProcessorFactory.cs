// Copyright (c) CloudNimble, Inc. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using CloudNimble.WebJobs.Extensions.Common.Queues;

namespace CloudNimble.WebJobs.Extensions.Tests.Common.Models
{

    /// <summary>
    /// Test implementation of IQueueProcessorFactory for testing purposes.
    /// </summary>
    public class TestQueueProcessorFactory : IQueueProcessorFactory
    {
        public QueueProcessor Create(QueueProcessorOptions context)
        {
            if (context == null)
            {
                context = new QueueProcessorOptions(
                    new TestQueueClient(),
                    new TestLoggerFactory(),
                    new QueuesOptionsBase { BatchSize = 16, MaxDequeueCount = 5 }
                );
            }
            return new QueueProcessor(context, new TestQueueRequestExceptionClassifier());
        }
    }

}
// Copyright (c) CloudNimble, Inc. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using CloudNimble.EasyAF.Core;
using CloudNimble.WebJobs.Extensions.Common;
using Microsoft.Azure.WebJobs;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace CloudNimble.WebJobs.Extensions.Amazon.SQS.Config
{

    // The core Async Collector for queueing messages. 
    internal class SQSMessageAsyncCollector : IAsyncCollector<SQSMessage>
    {

        private readonly SQSQueue _queue;
        private readonly IMessageEnqueuedWatcher _messageEnqueuedWatcher;

        public SQSMessageAsyncCollector(SQSQueue queue, IMessageEnqueuedWatcher messageEnqueuedWatcher)
        {
            _queue = queue;
            _messageEnqueuedWatcher = messageEnqueuedWatcher;
        }

        public async Task AddAsync(SQSMessage message, CancellationToken cancellationToken = default)
        {
            Ensure.ArgumentNotNull(message, nameof(message));

            await _queue.AddMessageAndCreateIfNotExistsAsync(message.Body, cancellationToken);

            _messageEnqueuedWatcher?.Notify(_queue.Name);
        }

        public Task FlushAsync(CancellationToken cancellationToken = default)
        {
            // Batching not supported. 
            return Task.FromResult(0);
        }

    }

}

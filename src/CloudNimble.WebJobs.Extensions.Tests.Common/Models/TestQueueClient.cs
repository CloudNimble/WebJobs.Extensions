// Copyright (c) CloudNimble, Inc. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using CloudNimble.WebJobs.Extensions.Common.Queues;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace CloudNimble.WebJobs.Extensions.Tests.Common.Models
{
    /// <summary>
    /// Enhanced test implementation of IQueueClient with tracking for testing purposes.
    /// </summary>
    internal class TestQueueClient : IQueueClient
    {
        public string Name { get; set; }
        public string AccountName { get; set; }
        public bool ShouldThrowOnUpdate { get; set; } = false;
        public bool ShouldThrowOnDelete { get; set; } = false;
        public Exception ExceptionToThrow { get; set; }

        public List<string> AddedMessages { get; } = new();
        public List<(string Id, string PopReceipt)> DeletedMessages { get; } = new();
        public List<UpdatedMessage> UpdatedMessages { get; } = new();

        public Task AddMessageAndCreateIfNotExistsAsync(string body, CancellationToken cancellationToken)
        {
            AddedMessages.Add(body);
            return Task.CompletedTask;
        }

        public Task DeleteMessageAsync(string id, string popReceipt, CancellationToken cancellationToken)
        {
            if (ShouldThrowOnDelete && ExceptionToThrow is not null)
            {
                throw ExceptionToThrow;
            }

            DeletedMessages.Add((id, popReceipt));
            return Task.CompletedTask;
        }

        public Task<bool?> ExistsAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult<bool?>(true);
        }

        public Task<QueueProperties> GetPropertiesAsync()
        {
            return Task.FromResult(new QueueProperties());
        }

        public Task<QueueResponse<TQueueMessage>> PeekMessagesAsync<TQueueMessage>(int v) where TQueueMessage : IQueueMessage
        {
            return Task.FromResult(new QueueResponse<TQueueMessage>());
        }

        public Task<QueueResponse<TQueueMessage>> ReceiveMessagesAsync<TQueueMessage>(int numMessagesToReceive, TimeSpan visibilityTimeout, CancellationToken token) where TQueueMessage : IQueueMessage
        {
            return Task.FromResult(new QueueResponse<TQueueMessage>());
        }

        public Task<QueueMessageUpdateReceipt> UpdateMessageAsync(string id, string popReceipt, TimeSpan visibilityTimeout, CancellationToken cancellationToken)
        {
            if (ShouldThrowOnUpdate && ExceptionToThrow is not null)
            {
                throw ExceptionToThrow;
            }

            UpdatedMessages.Add(new UpdatedMessage { Id = id, PopReceipt = popReceipt, VisibilityTimeout = visibilityTimeout });

            return Task.FromResult(new QueueMessageUpdateReceipt
            {
                PopReceipt = "new-receipt-" + Guid.NewGuid().ToString("N")[..8],
                NextVisibleOn = DateTimeOffset.UtcNow.Add(visibilityTimeout)
            });
        }
    }

}
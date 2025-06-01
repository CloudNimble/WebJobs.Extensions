// Copyright (c) CloudNimble, Inc. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using CloudNimble.WebJobs.Extensions.Common.Listeners;
using CloudNimble.WebJobs.Extensions.Common.Queues;
using Microsoft.Azure.WebJobs.Host.Executors;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace CloudNimble.WebJobs.Extensions.Tests.Common.Models
{

    /// <summary>
    /// Test implementation of ITriggerExecutor for testing purposes.
    /// </summary>
    public class TestTriggerExecutor<T> : ITriggerExecutor<T> where T : IQueueMessage
    {
        public int ExecuteCallCount { get; private set; }
        public T LastMessage { get; private set; }
        public bool ThrowOnExecute { get; set; }
        public bool ReturnSuccess { get; set; } = true;

        public Task<FunctionResult> ExecuteAsync(T value, CancellationToken cancellationToken)
        {
            ExecuteCallCount++;
            LastMessage = value;

            if (ThrowOnExecute)
            {
                throw new InvalidOperationException("Test exception");
            }

            var result = new FunctionResult(ReturnSuccess);
            return Task.FromResult(result);
        }
    }

}
// Copyright (c) CloudNimble, Inc. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Microsoft.Azure.WebJobs;
using System.Threading;
using System.Threading.Tasks;

namespace CloudNimble.WebJobs.Extensions.Amazon.SQS.Config
{

    /// <summary>
    /// 
    /// </summary>
    internal class SQSQueueBuilder : IAsyncConverter<SQSAttribute, SQSQueue>
    {
        private readonly PerHostConfig _bindingProvider;

        public SQSQueueBuilder(PerHostConfig bindingProvider)
        {
            _bindingProvider = bindingProvider;
        }

        async Task<SQSQueue> IAsyncConverter<SQSAttribute, SQSQueue>.ConvertAsync(
            SQSAttribute attrResolved,
            CancellationToken cancellation)
        {
            var queue = _bindingProvider.GetQueue(attrResolved);
            await queue.CreateIfNotExistsAsync(cancellation);
            return queue;
        }
    }
}

// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using CloudNimble.EasyAF.Core;
using CloudNimble.WebJobs.Extensions.Common.Queues;
using System;

namespace CloudNimble.WebJobs.Extensions.Common
{
    /// <summary>
    /// The default <see cref="IQueueProcessorFactory"/> implementation used by <see cref="QueuesOptionsBase"/>.
    /// </summary>
    internal class DefaultQueueProcessorFactory : IQueueProcessorFactory
    {

        private IQueueRequestExceptionClassifier _exceptionClassifier;

        public DefaultQueueProcessorFactory(IQueueRequestExceptionClassifier exceptionClassifier)
        {
            _exceptionClassifier = exceptionClassifier ?? throw new ArgumentNullException(nameof(exceptionClassifier));
        }

        /// <inheritdoc/>
        public virtual QueueProcessor Create(QueueProcessorOptions context)
        {
            Ensure.ArgumentNotNull(context, nameof(context));
            return new QueueProcessor(context, _exceptionClassifier);
        }

    }
}

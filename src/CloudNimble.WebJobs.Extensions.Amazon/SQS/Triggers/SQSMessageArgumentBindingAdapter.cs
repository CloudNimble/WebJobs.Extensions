// Copyright (c) CloudNimble, Inc. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using CloudNimble.WebJobs.Extensions.Common.Queues;
using CloudNimble.WebJobs.Extensions.Common.Triggers;
using Microsoft.Azure.WebJobs.Host.Bindings;
using Microsoft.Azure.WebJobs.Host.Triggers;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CloudNimble.WebJobs.Extensions.Amazon.SQS.Triggers
{
    /// <summary>
    /// Adapter that converts ITriggerDataArgumentBinding&lt;IQueueMessage&gt; to ITriggerDataArgumentBinding&lt;SQSMessage&gt;.
    /// This allows the generic queue binding infrastructure to work with the specific SQS message type.
    /// </summary>
    internal class SQSMessageArgumentBindingAdapter : ITriggerDataArgumentBinding<SQSMessage>
    {
        private readonly ITriggerDataArgumentBinding<IQueueMessage> _innerBinding;

        /// <summary>
        /// Initializes a new instance of the <see cref="SQSMessageArgumentBindingAdapter"/> class.
        /// </summary>
        /// <param name="innerBinding">The inner binding that works with IQueueMessage.</param>
        public SQSMessageArgumentBindingAdapter(ITriggerDataArgumentBinding<IQueueMessage> innerBinding)
        {
            _innerBinding = innerBinding ?? throw new ArgumentNullException(nameof(innerBinding));
        }

        /// <summary>
        /// Gets the type of the value.
        /// </summary>
        public Type ValueType => _innerBinding.ValueType;

        /// <summary>
        /// Gets the binding data contract.
        /// </summary>
        public IReadOnlyDictionary<string, Type> BindingDataContract => _innerBinding.BindingDataContract;

        /// <summary>
        /// Binds the SQS message to create trigger data.
        /// </summary>
        /// <param name="value">The SQS message to bind.</param>
        /// <param name="context">The value binding context.</param>
        /// <returns>The trigger data.</returns>
        public Task<ITriggerData> BindAsync(SQSMessage value, ValueBindingContext context)
        {
            // SQSMessage implements IQueueMessage, so we can pass it directly
            return _innerBinding.BindAsync(value, context);
        }
    }
}
// Copyright (c) CloudNimble, Inc. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using CloudNimble.WebJobs.Extensions.Common.Queues;
using Microsoft.Azure.WebJobs;
using System;

namespace CloudNimble.WebJobs.Extensions.Amazon.SQS.Triggers
{
    /// <summary>
    /// Adapter that converts an IConverter&lt;SQSMessage, T&gt; to IConverter&lt;IQueueMessage, T&gt;.
    /// This is needed because C# doesn't support variance on interfaces with multiple type parameters.
    /// </summary>
    /// <typeparam name="T">The target type of the conversion.</typeparam>
    internal class QueueMessageConverterAdapter<T> : IConverter<IQueueMessage, T>
    {
        private readonly IConverter<SQSMessage, T> _innerConverter;

        /// <summary>
        /// Initializes a new instance of the <see cref="QueueMessageConverterAdapter{T}"/> class.
        /// </summary>
        /// <param name="innerConverter">The inner converter that performs the actual conversion.</param>
        public QueueMessageConverterAdapter(IConverter<SQSMessage, T> innerConverter)
        {
            _innerConverter = innerConverter ?? throw new ArgumentNullException(nameof(innerConverter));
        }

        /// <summary>
        /// Converts an IQueueMessage to the target type by casting to SQSMessage first.
        /// </summary>
        /// <param name="input">The queue message to convert.</param>
        /// <returns>The converted value.</returns>
        /// <exception cref="InvalidCastException">Thrown when the input is not an SQSMessage.</exception>
        public T Convert(IQueueMessage input)
        {
            if (input is not SQSMessage sqsMessage)
            {
                throw new InvalidCastException($"Expected SQSMessage but got {input?.GetType().Name ?? "null"}");
            }

            return _innerConverter.Convert(sqsMessage);
        }
    }
}
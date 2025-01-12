// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Azure.WebJobs.Host.Bindings;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace CloudNimble.WebJobs.Extensions.Common.Queues
{

    /// <summary>
    /// Provides a value provider for queue messages.
    /// </summary>
    public class QueueMessageValueProvider : IValueProvider
    {

        private readonly IQueueMessage _message;
        private readonly object _value;
        private readonly Type _valueType;

        /// <summary>
        /// 
        /// </summary>
        protected readonly ILogger<QueueMessageValueProvider> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="QueueMessageValueProvider"/> class.
        /// </summary>
        /// <param name="message">The queue message.</param>
        /// <param name="value">The value to be provided.</param>
        /// <param name="valueType">The type of the value.</param>
        /// <param name="loggerFactory">The logger factory.</param>
        /// <exception cref="InvalidOperationException">Thrown when the value is not of the correct type.</exception>
        public QueueMessageValueProvider(IQueueMessage message, object value, Type valueType, ILoggerFactory loggerFactory)
        {
            if (value is not null && !valueType.IsAssignableFrom(value.GetType()))
            {
                throw new InvalidOperationException("value is not of the correct type.");
            }

            _message = message;
            _value = value;
            _valueType = valueType;
            _logger = loggerFactory.CreateLogger<QueueMessageValueProvider>();
        }

        /// <summary>
        /// Gets the type of the value.
        /// </summary>
        public Type Type => _valueType;

        /// <summary>
        /// Asynchronously gets the value.
        /// </summary>
        /// <returns>A task that represents the asynchronous operation. The task result contains the value.</returns>
        public Task<object> GetValueAsync() => Task.FromResult(_value);

        /// <summary>
        /// Gets a string representation of the queue message to be used for invocation.
        /// </summary>
        /// <returns>The body of the queue message.</returns>
        public string ToInvokeString() => _message.Body;

    }

}

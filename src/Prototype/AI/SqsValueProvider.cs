using Microsoft.Azure.WebJobs.Host.Bindings;
using System;
using System.Threading.Tasks;

namespace CloudNimble.WebJobs.Extensions.Amazon.SQS.AI
{

    /// <summary>
    /// Provides value binding capabilities for SQS messages.
    /// </summary>
    public sealed class SqsValueProvider : IValueProvider
    {

        private readonly object _value;

        /// <summary>
        /// Gets the type of the provided value.
        /// </summary>
        public Type Type => _value?.GetType() ?? typeof(object);

        /// <summary>
        /// Initializes a new instance of the <see cref="SQSValueProvider"/> class.
        /// </summary>
        /// <param name="value">The value to provide.</param>
        public SqsValueProvider(object value)
        {
            _value = value;
        }

        /// <summary>
        /// Gets the value asynchronously.
        /// </summary>
        /// <returns>A task that represents the asynchronous operation. The task result contains the value.</returns>
        public Task<object> GetValueAsync() => Task.FromResult(_value);

        /// <summary>
        /// Converts the value to a string for invocation purposes.
        /// </summary>
        /// <returns>A string representation of the value.</returns>
        public string ToInvokeString() => _value?.ToString() ?? string.Empty;

    }

}
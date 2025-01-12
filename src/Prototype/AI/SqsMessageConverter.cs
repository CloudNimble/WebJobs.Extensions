using Amazon.SQS.Model;
using Microsoft.Azure.WebJobs;
using System;
using System.Text.Json;

namespace CloudNimble.WebJobs.Extensions.Amazon.SQS.AI
{

    /// <summary>
    /// Provides type conversion capabilities for SQS messages.
    /// </summary>
    /// <typeparam name="T">The type to convert the message to.</typeparam>
    public sealed class SqsMessageConverter<T> : IConverter<Message, T>
    {

        /// <summary>
        /// Converts an SQS message to the specified type.
        /// </summary>
        /// <param name="message">The message to convert.</param>
        /// <returns>The converted message.</returns>
        public T Convert(Message message)
        {
            ArgumentNullException.ThrowIfNull(message);

            return typeof(T) == typeof(string)
                ? (T)(object)message.Body
                : JsonSerializer.Deserialize<T>(message.Body);
        }

    }

}
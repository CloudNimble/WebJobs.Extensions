using Amazon.SQS.Model;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Amazon.SQS
{

    /// <summary>
    /// Provides extension methods for handling poison messages in Amazon SQS queues.
    /// </summary>
    public static class IAmazonSqsExtensions
    {

        /// <summary>
        /// Moves a message to its corresponding poison queue.
        /// </summary>
        /// <param name="client">The Amazon SQS client.</param>
        /// <param name="message">The message to move.</param>
        /// <param name="sourceQueueUrl">The URL of the source queue.</param>
        /// <param name="error">The error that caused the message to be poisoned.</param>
        /// <param name="logger">The logger to use for error reporting.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        public static async Task MoveToPoisonQueueAsync(
            this IAmazonSQS client,
            Message message,
            string sourceQueueUrl,
            Exception error,
            ILogger logger,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(client);
            ArgumentNullException.ThrowIfNull(message);
            ArgumentNullException.ThrowIfNull(sourceQueueUrl);
            ArgumentNullException.ThrowIfNull(error);
            ArgumentNullException.ThrowIfNull(logger);

            try
            {
                var poisonQueueUrl = GetPoisonQueueUrl(sourceQueueUrl);
                var messageAttributes = message.MessageAttributes ?? new Dictionary<string, MessageAttributeValue>();

                messageAttributes["MovedToPoisonAt"] = new MessageAttributeValue
                {
                    DataType = "String",
                    StringValue = DateTime.UtcNow.ToString("O")
                };

                messageAttributes["ProcessingError"] = new MessageAttributeValue
                {
                    DataType = "String",
                    StringValue = error.Message
                };

                messageAttributes["SourceQueue"] = new MessageAttributeValue
                {
                    DataType = "String",
                    StringValue = sourceQueueUrl
                };

                await client.SendMessageAsync(new SendMessageRequest
                {
                    QueueUrl = poisonQueueUrl,
                    MessageBody = message.Body,
                    MessageAttributes = messageAttributes
                }, cancellationToken);

                await client.DeleteMessageAsync(sourceQueueUrl, message.ReceiptHandle, cancellationToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to move message {MessageId} to poison queue", message.MessageId);
                throw;
            }
        }

        /// <summary>
        /// Gets the URL of the poison queue corresponding to the specified queue URL.
        /// </summary>
        /// <param name="queueUrl">The URL of the source queue.</param>
        /// <returns>The URL of the corresponding poison queue.</returns>
        private static string GetPoisonQueueUrl(string queueUrl) => $"{queueUrl}-poison";

    }

}
// Copyright (c) CloudNimble, Inc. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Amazon.SQS.Model;
using CloudNimble.WebJobs.Extensions.Common.Queues;
using System;
using System.Collections.Generic;

namespace CloudNimble.WebJobs.Extensions.Amazon.SQS
{

    /// <summary>
    /// Provides an implementation of a cloud queue message that wraps Amazon SQS messages.
    /// Supports both real SQS messages received from AWS and synthetic messages created from converters.
    /// </summary>
    public sealed class SQSMessage : IQueueMessage
    {

        #region Private Fields

        private readonly Message _message;
        private readonly string _queueUrl;

        #endregion

        #region Public Properties

        /// <summary>
        /// Gets the content of the message as a string.
        /// </summary>
        public string Body
        {
            get => _message.Body;
            set => _message.Body = value;
        }

        /// <summary>
        /// Gets or sets the timestamp when the message was inserted into the queue.
        /// </summary>
        public DateTimeOffset? DateInserted
        {
            get => GetInsertionTime();
            set => _message.Attributes["SentTimestamp"] = value?.ToUnixTimeMilliseconds().ToString();
        }

        /// <summary>
        /// Gets the number of times this message has been dequeued.
        /// </summary>
        public int DequeueCount => GetDequeueCount();

        /// <summary>
        /// Gets the unique identifier for the message.
        /// </summary>
        public string Id
        {
            get => _message.MessageId;
            set => _message.MessageId = value;
        }

        /// <summary>
        /// Gets the underlying Amazon SQS message.
        /// </summary>
        public Message Original => _message;

        /// <summary>
        /// Gets the receipt handle used to delete or update the message.
        /// </summary>
        public string PopReceipt
        {
            get => _message.ReceiptHandle;
            set => _message.ReceiptHandle = value;
        }

        /// <summary>
        /// Gets the URL of the queue containing this message.
        /// </summary>
        public string QueueUrl => _queueUrl;

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="SQSMessage"/> class from an existing AWS SQS message.
        /// </summary>
        /// <param name="message">The underlying Amazon SQS message.</param>
        /// <param name="queueUrl">The URL of the queue containing the message.</param>
        /// <exception cref="ArgumentNullException">Thrown when message is null.</exception>
        /// <example>
        /// <code>
        /// // Creating from an AWS SQS message received from polling
        /// var sqsMessage = new SQSMessage(awsMessage, "https://sqs.us-east-1.amazonaws.com/123456789012/my-queue");
        /// </code>
        /// </example>
        public SQSMessage(Message message, string queueUrl)
        {
            ArgumentNullException.ThrowIfNull(message);

            _message = message;
            _queueUrl = queueUrl ?? string.Empty;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SQSMessage"/> class from a message body string.
        /// This constructor creates a synthetic message for testing or converter scenarios.
        /// </summary>
        /// <param name="messageBody">The content of the message.</param>
        /// <param name="queueUrl">The URL of the queue (optional for synthetic messages).</param>
        /// <exception cref="ArgumentNullException">Thrown when messageBody is null.</exception>
        /// <example>
        /// <code>
        /// // Creating a synthetic message from string content
        /// var sqsMessage = new SQSMessage("Hello, World!");
        /// Console.WriteLine(sqsMessage.Body); // Outputs: Hello, World!
        /// </code>
        /// </example>
        public SQSMessage(string messageBody, string queueUrl = "")
        {
            ArgumentNullException.ThrowIfNull(messageBody);

            _queueUrl = queueUrl ?? string.Empty;

            // Create a synthetic message
            _message = new Message
            {
                Body = messageBody,
                MessageId = Guid.NewGuid().ToString(),
                ReceiptHandle = Guid.NewGuid().ToString(),
                Attributes = new Dictionary<string, string>
                {
                    ["SentTimestamp"] = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString(),
                    ["ApproximateReceiveCount"] = "1"
                }
            };
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Gets the number of times this message has been dequeued based on SQS attributes.
        /// </summary>
        /// <returns>The dequeue count, or 1 if not available.</returns>
        private int GetDequeueCount() =>
            _message.Attributes.TryGetValue("ApproximateReceiveCount", out var countStr) &&
            int.TryParse(countStr, out var count) ? count : 1;

        /// <summary>
        /// Gets the insertion time from the SQS message attributes.
        /// </summary>
        /// <returns>The insertion time as a DateTimeOffset, or null if not available.</returns>
        private DateTimeOffset? GetInsertionTime() =>
            _message.Attributes.TryGetValue("SentTimestamp", out var timeStr) &&
            long.TryParse(timeStr, out var timestamp)
                ? DateTimeOffset.FromUnixTimeMilliseconds(timestamp)
                : null;

        #endregion

    }

}
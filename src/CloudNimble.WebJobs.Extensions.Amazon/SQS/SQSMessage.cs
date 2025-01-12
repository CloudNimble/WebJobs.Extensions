// Copyright (c) CloudNimble, Inc. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Amazon.SQS.Model;
using CloudNimble.WebJobs.Extensions.Common;
using CloudNimble.WebJobs.Extensions.Common.Queues;
using System;
using System.Text.Json;

namespace CloudNimble.WebJobs.Extensions.Amazon.SQS
{

    /// <summary>
    /// Provides an implementation of a cloud queue message that wraps Amazon SQS messages.
    /// </summary>
    public sealed class SQSMessage : IQueueMessage
    {

        #region Private Members

        private readonly Message _message;
        private readonly string _queueUrl;

        #endregion

        #region Properties

        /// <summary>
        /// Gets the underlying Amazon SQS message.
        /// </summary>
        public Message Original => _message;

        /// <summary>
        /// Gets the content of the message as a string.
        /// </summary>
        public string Body
        {
            get => _message.Body;
            set => _message.Body = value;
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
        /// 
        /// </summary>
        public DateTimeOffset? DateInserted
        {
            get => GetInsertionTime();
            set => _message.Attributes["SentTimestamp"] = value?.ToString(Constants.DateTimeFormatString);
        }

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
        /// 
        /// </summary>
        /// <param name="message"></param>
        /// <param name="queueUrl"></param>
        public SQSMessage(string message, string queueUrl = "")
        {
            _message = JsonSerializer.Deserialize<Message>(message);
            _queueUrl = queueUrl;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SQSMessage"/> class.
        /// </summary>
        /// <param name="message">The underlying Amazon SQS message.</param>
        /// <param name="queueUrl">The URL of the queue containing the message.</param>
        public SQSMessage(Message message, string queueUrl)
        {
            _message = message;
            _queueUrl = queueUrl;
        }

        #endregion

        /// <summary>
        /// Gets the number of times this message has been dequeued based on SQS attributes.
        /// </summary>
        /// <returns>The dequeue count, or 1 if not available.</returns>
        private int GetDequeueCount() =>
            _message.Attributes.TryGetValue("ApproximateReceiveCount", out var countStr) &&
            int.TryParse(countStr, out var count) ? count : 1;


        private DateTimeOffset? GetInsertionTime() =>
            _message.Attributes.TryGetValue("SentTimestamp", out var timeStr) ? DateTimeOffset.Parse(timeStr) : null;

    }

}
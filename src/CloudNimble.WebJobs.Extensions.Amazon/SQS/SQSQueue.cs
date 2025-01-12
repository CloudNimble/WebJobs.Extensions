// Copyright (c) CloudNimble, Inc. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Amazon.SQS;
using Amazon.SQS.Model;
using CloudNimble.WebJobs.Extensions.Common.Queues;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace CloudNimble.WebJobs.Extensions.Amazon.SQS
{

    /// <summary>
    /// A wrapper for a specific Amazon SQS queue.
    /// </summary>
    internal partial class SQSQueue : IQueueClient
    {

        #region Private Members

        /// <summary>
        /// The Amazon SQS client used to interact with the SQS service.
        /// </summary>
        private readonly IAmazonSQS _sqsClient;

        #endregion

        #region Public Properties

        /// <summary>
        /// Gets or sets the account name associated with the SQS queue.
        /// </summary>
        public string AccountName { get; set; }

        /// <summary>
        /// Gets the approximate number of messages in the queue.
        /// </summary>
        public int? ApproximateMessageCount { get; private set; }

        /// <summary>
        /// Gets the approximate number of messages that are delayed from delivery.
        /// </summary>
        public int ApproximateMessagesDelayedCount { get; private set; }

        /// <summary>
        /// Gets the approximate number of messages currently being processed.
        /// </summary>
        public int? ApproximateMessagesProcessingCount { get; private set; }

        /// <summary>
        /// Gets the timestamp when the queue was created.
        /// </summary>
        public DateTime CreatedTimestamp { get; private set; }

        /// <summary>
        /// Gets or sets the delay in seconds for which the delivery of all messages in the queue is delayed.
        /// </summary>
        public int DelaySeconds { get; set; }

        /// <summary>
        /// Gets a value indicating whether the queue has the FIFO attribute.
        /// </summary>
        public bool HasFifoAttribute { get; private set; }

        /// <summary>
        /// Gets a value indicating whether the queue is a FIFO (First-In-First-Out) queue.
        /// </summary>
        public bool IsFifoName => Name.EndsWith(".fifo");

        /// <summary>
        /// Gets the timestamp when the queue was last modified.
        /// </summary>
        public DateTime LastModifiedTimestamp { get; private set; }

        /// <summary>
        /// Gets the logger instance used for logging.
        /// </summary>
        public ILogger<SQSQueue> Logger { get; private set; }

        /// <summary>
        /// Gets or sets the maximum message size in bytes.
        /// </summary>
        public int MaximumMessageSizeInBytes { get; set; } = 256 * 1024;

        /// <summary>
        /// Gets or sets the message retention period in seconds.
        /// </summary>
        public int MessageRetentionPeriod { get; private set; }

        /// <summary>
        /// Gets the name of the queue.
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// Gets or sets the policy of the queue.
        /// </summary>
        public string Policy { get; private set; }

        /// <summary>
        /// Gets the Amazon Resource Name (ARN) of the queue.
        /// </summary>
        public string QueueArn { get; private set; }

        /// <summary>
        /// Gets or sets the visibility timeout in seconds.
        /// </summary>
        public int VisibilityTimeoutInSeconds { get; set; } = 30;

        /// <summary>
        /// Gets the URL of the queue.
        /// </summary>
        public string Url => GetQueueUrl();

        /// <summary>
        /// Gets a value indicating whether the queue has content-based deduplication enabled.
        /// </summary>
        public bool UseContentBasedDeduplication { get; private set; }

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="SQSQueue"/> class.
        /// </summary>
        /// <param name="queueName">The name of the queue.</param>
        /// <param name="sqsClient">The Amazon SQS client.</param>
        /// <param name="loggerFactory">The logger factory.</param>
        public SQSQueue(string queueName, IAmazonSQS sqsClient, ILoggerFactory loggerFactory)
        {
            Name = queueName;
            _sqsClient = sqsClient;
            Logger = loggerFactory.CreateLogger<SQSQueue>();
        }

        #endregion

        #region Public Static Methods

        /// <summary>
        /// Validates an Amazon SQS queue name.
        /// </summary>
        /// <param name="queueName">The queue name to validate.</param>
        /// <param name="errorMessage">The error message if the queue name is invalid.</param>
        /// <returns>True if the queue name is valid, otherwise false.</returns>
        public static bool IsValidQueueName(string queueName, out string errorMessage)
        {
            if (string.IsNullOrWhiteSpace(queueName))
            {
                errorMessage = "A queue name can't be null or empty";
                return false;
            }

            // Queue name must be between 1 and 80 characters long
            if (queueName.Length is < 1 or > 80)
            {
                errorMessage = $"A queue name must be from 1 to 80 characters long - '{queueName}'";
                return false;
            }

            // Queue name can contain alphanumeric characters, hyphens (-), and underscores (_)
            // It must start with an alphanumeric character
            var regex = ValidQueueNameRegex();
            if (!regex.IsMatch(queueName))
            {
                errorMessage = $"A queue name can only have alphanumeric, hyphen (-), or underscore (_) characters - '{queueName}'";
                return false;
            }

            // Queue name cannot end with a hyphen (-) or underscore (_)
            if ("-_".Contains(queueName[^1]))
            {
                errorMessage = $"A queue name can't end with hyphen (-) or underscore (_) characters - '{queueName}'";
                return false;
            }

            errorMessage = null;
            return true;
        }

        /// <summary>
        /// Validates the specified queue name.
        /// </summary>
        /// <param name="name">The name of the queue to validate.</param>
        /// <exception cref="ArgumentException">Thrown when the queue name is invalid.</exception>
        public static void ValidateQueueName(string name)
        {
            if (!IsValidQueueName(name, out string errorMessage))
            {
                throw new ArgumentException(errorMessage, nameof(name));
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Fetches the attributes of the SQS queue.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        public async Task UpdateAttributesAsync(CancellationToken cancellationToken = default)
        {
            var response = await _sqsClient.GetQueueAttributesAsync(Name, ["All"], cancellationToken);
            ApproximateMessageCount = response.ApproximateNumberOfMessages;
            ApproximateMessagesDelayedCount = response.ApproximateNumberOfMessagesDelayed;
            ApproximateMessagesProcessingCount = response.ApproximateNumberOfMessagesNotVisible;
            UseContentBasedDeduplication = response.ContentBasedDeduplication ?? false;
            CreatedTimestamp = response.CreatedTimestamp;
            DelaySeconds = response.DelaySeconds;
            HasFifoAttribute = response.FifoQueue;
            LastModifiedTimestamp = response.LastModifiedTimestamp;
            MaximumMessageSizeInBytes = response.MaximumMessageSize;
            MessageRetentionPeriod = response.MessageRetentionPeriod;
            Policy = response.Policy;
            QueueArn = response.QueueARN;
            VisibilityTimeoutInSeconds = response.VisibilityTimeout;
        }

        /// <summary>
        /// Adds a message to the queue and creates the queue if it does not exist.
        /// </summary>
        /// <param name="messageBody">The message body.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        public async Task AddMessageAndCreateIfNotExistsAsync(string messageBody, CancellationToken cancellationToken = default)
        {
            await CreateIfNotExistsAsync(cancellationToken);

            var request = new SendMessageRequest
            {
                QueueUrl = Name,
                MessageBody = messageBody,
                // RWM: FIFO queues require MessageGroupId and MessageDeduplicationId
                //      The inclination here might be to use different groups for different types of messages. However,
                //      if you are in need of different priorities in processing, for consistency with the WebJobs SDK,
                //      it may be best to use separate queues instead, since MessageGroups are not a universal construct.

                MessageGroupId = "default",
                // RWM: Message body hashing will be used to ensure that the same message is not sent to the queue more than once.
                MessageDeduplicationId = GenerateDeduplicationId(messageBody)
            };

            await _sqsClient.SendMessageAsync(request, cancellationToken);
        }

        /// <summary>
        /// Deletes a message from the queue.
        /// </summary>
        /// <param name="id">The message ID.</param>
        /// <param name="popReceipt">The pop receipt.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        public async Task DeleteMessageAsync(string id, string popReceipt, CancellationToken cancellationToken)
        {
            var request = new DeleteMessageRequest
            {
                QueueUrl = Name,
                ReceiptHandle = popReceipt
            };

            await _sqsClient.DeleteMessageAsync(request, cancellationToken);
        }

        /// <summary>
        /// Checks if the queue exists.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>True if the queue exists, otherwise false.</returns>
        public async Task<bool?> ExistsAsync(CancellationToken cancellationToken)
        {
            try
            {
                var request = new GetQueueAttributesRequest
                {
                    QueueUrl = Name,
                    AttributeNames = ["All"]
                };

                await _sqsClient.GetQueueAttributesAsync(request, cancellationToken);
                return true;
            }
            catch (QueueDoesNotExistException)
            {
                return false;
            }
        }

        /// <summary>
        /// Gets the properties of the queue.
        /// </summary>
        /// <returns>The queue properties.</returns>
        public async Task<QueueProperties> GetPropertiesAsync()
        {
            await UpdateAttributesAsync();

            return new QueueProperties
            {
                ApproximateMessagesCount = ApproximateMessageCount ?? 0,
                Metadata = new Dictionary<string, string>
                {
                    { "FifoQueue", HasFifoAttribute.ToString() },
                    { "ContentBasedDeduplication", UseContentBasedDeduplication.ToString() },
                    { "DelaySeconds", DelaySeconds.ToString() }
                }
            };
        }

        /// <summary>
        /// Peeks messages from the queue.
        /// </summary>
        /// <param name="count">The number of messages to peek.</param>
        /// <returns>The queue response.</returns>
        public async Task<QueueResponse<TQueueMessage>> PeekMessagesAsync<TQueueMessage>(int count)
            where TQueueMessage : IQueueMessage
        {
            // SQS doesn't support peeking, so we'll receive with very short visibility timeout
            return await ReceiveMessagesAsync<TQueueMessage>(count, TimeSpan.FromSeconds(1), CancellationToken.None);
        }

        /// <summary>
        /// Receives messages from the queue.
        /// </summary>
        /// <param name="numMessagesToReceive">The number of messages to receive.</param>
        /// <param name="visibilityTimeout">The visibility timeout.</param>
        /// <param name="token">The cancellation token.</param>
        /// <returns>The queue response.</returns>
        public async Task<QueueResponse<TQueueMessage>> ReceiveMessagesAsync<TQueueMessage>(
            int numMessagesToReceive,
            TimeSpan visibilityTimeout,
            CancellationToken token) where TQueueMessage : IQueueMessage
        {
            var request = new ReceiveMessageRequest
            {
                QueueUrl = Name,
                MaxNumberOfMessages = Math.Min(numMessagesToReceive, 10), // SQS max is 10
                VisibilityTimeout = (int)visibilityTimeout.TotalSeconds,
                MessageSystemAttributeNames = ["All"],
                MessageAttributeNames = ["All"]
            };

            var response = await _sqsClient.ReceiveMessageAsync(request, token);

            return new QueueResponse<TQueueMessage>
            {
                ClientRequestId = response.ResponseMetadata.RequestId,
                Value = [.. response.Messages.Select(m => (TQueueMessage)Activator.CreateInstance(typeof(TQueueMessage), m))]
            };
        }

        /// <summary>
        /// Updates a message in the queue.
        /// </summary>
        /// <param name="id">The message ID.</param>
        /// <param name="popReceipt">The pop receipt.</param>
        /// <param name="visibilityTimeout">The visibility timeout.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The queue message update receipt.</returns>
        public async Task<QueueMessageUpdateReceipt> UpdateMessageAsync(
            string id,
            string popReceipt,
            TimeSpan visibilityTimeout,
            CancellationToken cancellationToken)
        {
            var request = new ChangeMessageVisibilityRequest
            {
                QueueUrl = Name,
                ReceiptHandle = popReceipt,
                VisibilityTimeout = (int)visibilityTimeout.TotalSeconds
            };

            await _sqsClient.ChangeMessageVisibilityAsync(request, cancellationToken);

            return new QueueMessageUpdateReceipt
            {
                PopReceipt = popReceipt,
                NextVisibleOn = DateTimeOffset.UtcNow.Add(visibilityTimeout)
            };
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Gets the regular expression for validating queue names.
        /// </summary>
        /// <returns>The regular expression.</returns>
        [GeneratedRegex("^[a-zA-Z0-9][a-zA-Z0-9-_]*$")]
        private static partial Regex ValidQueueNameRegex();

        internal async Task CreateIfNotExistsAsync(CancellationToken cancellationToken)
        {
            if (await ExistsAsync(cancellationToken) == true)
            {
                return;
            }

            var request = new CreateQueueRequest
            {
                QueueName = Name,
                Attributes = new Dictionary<string, string>
                {
                    { "FifoQueue", "true" },
                    { "ContentBasedDeduplication", "true" },
                    { "VisibilityTimeout", VisibilityTimeoutInSeconds.ToString() },
                    { "MessageRetentionPeriod", MessageRetentionPeriod.ToString() },
                    { "DelaySeconds", DelaySeconds.ToString() }
                }
            };

            await _sqsClient.CreateQueueAsync(request, cancellationToken);
            await UpdateAttributesAsync(cancellationToken);
        }
        /// <summary>
        /// Generates a deduplication ID based on content or custom strategy
        /// </summary>
        private string GenerateDeduplicationId(string messageBody)
        {
            if (this.UseContentBasedDeduplication)
            {
                return Convert.ToBase64String(
                    System.Security.Cryptography.SHA256.HashData(
                        System.Text.Encoding.UTF8.GetBytes(messageBody)
                    )
                );
            }
            return Guid.NewGuid().ToString();
        }

        private string GetQueueUrl()
        {
            if (Name.StartsWith("https://"))
                return Name;

            return $"https://sqs.{_sqsClient.Config.RegionEndpoint.SystemName}.amazonaws.com/{AccountName}/{Name}";
        }

        #endregion
    }

}

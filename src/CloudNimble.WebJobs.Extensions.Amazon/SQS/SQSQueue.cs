// Copyright (c) CloudNimble, Inc. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Amazon.SQS;
using CloudNimble.WebJobs.Extensions.Common.Queues;
using Microsoft.Extensions.Logging;
using System;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace CloudNimble.WebJobs.Extensions.Amazon.SQS
{

    /// <summary>
    /// A wrapper for a specific Amazon SQS queue.
    /// </summary>
    internal partial class SQSQueue : IQueueClient<SQSMessage>
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
        /// Gets the approximate number of messages currently being processed.
        /// </summary>
        public int? ApproximateMessagesProcessingCount { get; private set; }

        /// <summary>
        /// Gets a value indicating whether the queue is a FIFO (First-In-First-Out) queue.
        /// </summary>
        public bool IsFIFO => Name.EndsWith(".fifo");

        /// <summary>
        /// Gets the logger instance used for logging.
        /// </summary>
        public ILogger<SQSQueue> Logger { get; private set; }

        /// <summary>
        /// Gets or sets the maximum message size in bytes.
        /// </summary>
        public int MaximumMessageSizeInBytes { get; set; } = 256 * 1024;

        /// <summary>
        /// Gets the name of the queue.
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// Gets or sets the visibility timeout in seconds.
        /// </summary>
        public int VisibilityTimeoutInSeconds { get; set; } = 30;

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
        public async Task FetchAttributesAsync(CancellationToken cancellationToken = default)
        {
            var response = await _sqsClient.GetQueueAttributesAsync(Name, ["All"], cancellationToken);
            ApproximateMessageCount = response.ApproximateNumberOfMessages;
            ApproximateMessagesProcessingCount = response.ApproximateNumberOfMessagesNotVisible;
            MaximumMessageSizeInBytes = response.MaximumMessageSize;
            VisibilityTimeoutInSeconds = response.VisibilityTimeout;
        }

        /// <summary>
        /// Adds a message to the queue and creates the queue if it does not exist.
        /// </summary>
        /// <param name="messageBody">The message body.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        public async Task AddMessageAndCreateIfNotExistsAsync(string messageBody, CancellationToken cancellationToken = default)
        {
            await Task.CompletedTask;
        }

        /// <summary>
        /// Deletes a message from the queue.
        /// </summary>
        /// <param name="id">The message ID.</param>
        /// <param name="popReceipt">The pop receipt.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        public Task DeleteMessageAsync(string id, string popReceipt, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Checks if the queue exists.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>True if the queue exists, otherwise false.</returns>
        public Task<bool?> ExistsAsync(CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Gets the properties of the queue.
        /// </summary>
        /// <returns>The queue properties.</returns>
        public Task<QueueProperties> GetPropertiesAsync()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Peeks messages from the queue.
        /// </summary>
        /// <param name="v">The number of messages to peek.</param>
        /// <returns>The queue response.</returns>
        public Task<QueueResponse<SQSMessage>> PeekMessagesAsync(int v)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Receives messages from the queue.
        /// </summary>
        /// <param name="numMessagesToReceive">The number of messages to receive.</param>
        /// <param name="visibilityTimeout">The visibility timeout.</param>
        /// <param name="token">The cancellation token.</param>
        /// <returns>The queue response.</returns>
        public Task<QueueResponse<SQSMessage>> ReceiveMessagesAsync(int numMessagesToReceive, TimeSpan visibilityTimeout, CancellationToken token)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Updates a message in the queue.
        /// </summary>
        /// <param name="id">The message ID.</param>
        /// <param name="popReceipt">The pop receipt.</param>
        /// <param name="visibilityTimeout">The visibility timeout.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The queue message update receipt.</returns>
        public Task<QueueMessageUpdateReceipt> UpdateMessageAsync(string id, string popReceipt, TimeSpan visibilityTimeout, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Gets the regular expression for validating queue names.
        /// </summary>
        /// <returns>The regular expression.</returns>
        [GeneratedRegex("^[a-zA-Z0-9][a-zA-Z0-9-_]*$")]
        private static partial Regex ValidQueueNameRegex();

        internal async Task CreateIfNotExistsAsync(CancellationToken cancellation)
        {
            await Task.CompletedTask;
        }

        #endregion
    }

}

// Copyright (c) CloudNimble, Inc. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using CloudNimble.WebJobs.Extensions.Common;
using CloudNimble.WebJobs.Extensions.Common.Queues;
using Microsoft.Azure.WebJobs.Hosting;
using System;
using System.ComponentModel;
using System.Text.Json;

namespace CloudNimble.WebJobs.Extensions.Amazon.SQS
{

    /// <summary>
    /// Provides configuration options specific to Amazon SQS queue processing.
    /// Extends the base queue options with SQS-specific settings for authentication, regions, and FIFO queues.
    /// </summary>
    public class SQSOptions : QueuesOptionsBase, IOptionsFormatter
    {

        #region Private Fields

        private string _messageGroupId = "default";

        #endregion

        #region Public Properties

        /// <summary>
        /// Gets or sets the AWS access key ID for authentication.
        /// If not specified, the AWS SDK will use default credential resolution.
        /// </summary>
        /// <remarks>
        /// This property is optional. If not set, the AWS SDK will attempt to resolve credentials
        /// from environment variables, IAM roles, or other configured credential sources.
        /// </remarks>
        /// <example>
        /// <code>
        /// options.AccessKey = "AKIAIOSFODNN7EXAMPLE";
        /// </code>
        /// </example>
        public string AccessKey { get; set; }

        /// <summary>
        /// Gets or sets the AWS region for SQS operations.
        /// If not specified, the AWS SDK will use the default region configuration.
        /// </summary>
        /// <remarks>
        /// This should be set to the AWS region where your SQS queues are located.
        /// Common values include "us-east-1", "us-west-2", "eu-west-1", etc.
        /// </remarks>
        /// <example>
        /// <code>
        /// options.Region = "us-east-1";
        /// </code>
        /// </example>
        public string Region { get; set; }

        /// <summary>
        /// Gets or sets the AWS secret access key for authentication.
        /// If not specified, the AWS SDK will use default credential resolution.
        /// </summary>
        /// <remarks>
        /// This property is optional and should be used in conjunction with <see cref="AccessKey"/>.
        /// For security reasons, consider using IAM roles or environment variables instead of
        /// hardcoding credentials in your application.
        /// </remarks>
        /// <example>
        /// <code>
        /// options.SecretKey = "wJalrXUtnFEMI/K7MDENG/bPxRfiCYEXAMPLEKEY";
        /// </code>
        /// </example>
        public string SecretKey { get; set; }

        /// <summary>
        /// Gets or sets the custom service URL for SQS operations.
        /// This is primarily used for local development with LocalStack or other SQS-compatible services.
        /// </summary>
        /// <remarks>
        /// When specified, this URL will override the default AWS SQS endpoint.
        /// This is useful for development and testing scenarios where you want to use LocalStack
        /// or other SQS-compatible services instead of AWS SQS.
        /// </remarks>
        /// <example>
        /// <code>
        /// // For LocalStack
        /// options.ServiceUrl = "http://localhost:4566";
        /// 
        /// // For custom SQS-compatible service
        /// options.ServiceUrl = "https://my-sqs-service.example.com";
        /// </code>
        /// </example>
        public string ServiceUrl { get; set; }

        /// <summary>
        /// Gets or sets whether to use FIFO (First-In-First-Out) queue features by default.
        /// This affects message deduplication and ordering guarantees.
        /// </summary>
        /// <remarks>
        /// When set to true, messages sent to FIFO queues will include FIFO-specific attributes
        /// such as MessageGroupId and MessageDeduplicationId. This only applies to queues
        /// whose names end with ".fifo".
        /// </remarks>
        /// <example>
        /// <code>
        /// options.UseFifo = true;
        /// options.MessageGroupId = "order-processing";
        /// </code>
        /// </example>
        public bool UseFifo { get; set; }

        /// <summary>
        /// Gets or sets the default message group ID for FIFO queues.
        /// This property is only used when <see cref="UseFifo"/> is true and the queue is a FIFO queue.
        /// </summary>
        /// <remarks>
        /// The MessageGroupId is required for FIFO queues and is used to ensure that messages
        /// with the same MessageGroupId are processed in order. Messages with different
        /// MessageGroupIds can be processed in parallel.
        /// </remarks>
        /// <example>
        /// <code>
        /// options.MessageGroupId = "user-123-orders";
        /// </code>
        /// </example>
        /// <exception cref="ArgumentException">Thrown when setting to null or whitespace.</exception>
        public string MessageGroupId
        {
            get => _messageGroupId;
            set
            {
                // Use manual validation for broader .NET compatibility
                if (string.IsNullOrWhiteSpace(value))
                {
                    throw new ArgumentException("MessageGroupId cannot be null or whitespace.", nameof(value));
                }
                _messageGroupId = value;
            }
        }

        /// <summary>
        /// Gets or sets whether to enable content-based deduplication for FIFO queues.
        /// When enabled, SQS uses a SHA-256 hash of the message body to generate the MessageDeduplicationId.
        /// </summary>
        /// <remarks>
        /// This property only applies to FIFO queues. When enabled, you don't need to provide
        /// a MessageDeduplicationId when sending messages, as SQS will generate one based on
        /// the message content. Messages with identical content will be deduplicated.
        /// </remarks>
        /// <example>
        /// <code>
        /// options.UseContentBasedDeduplication = true;
        /// </code>
        /// </example>
        public bool UseContentBasedDeduplication { get; set; }

        #endregion

        #region Public Methods

        /// <summary>
        /// Creates a copy of this <see cref="SQSOptions"/> instance.
        /// </summary>
        /// <returns>A new <see cref="SQSOptions"/> instance with the same configuration values.</returns>
        /// <example>
        /// <code>
        /// var originalOptions = new SQSOptions 
        /// { 
        ///     Region = "us-east-1", 
        ///     UseFifo = true 
        /// };
        /// var copiedOptions = originalOptions.Clone();
        /// </code>
        /// </example>
        public new SQSOptions Clone()
        {
            return new SQSOptions
            {
                // Copy base class properties using private field access for proper cloning
                BatchSize = this.BatchSize,
                NewBatchThreshold = this.NewBatchThreshold,
                MaxPollingInterval = this.MaxPollingInterval,
                MaxDequeueCount = this.MaxDequeueCount,
                VisibilityTimeout = this.VisibilityTimeout,
                MessageEncoding = this.MessageEncoding,

                // Copy SQS-specific properties
                AccessKey = this.AccessKey,
                Region = this.Region,
                SecretKey = this.SecretKey,
                ServiceUrl = this.ServiceUrl,
                UseFifo = this.UseFifo,
                MessageGroupId = this.MessageGroupId,
                UseContentBasedDeduplication = this.UseContentBasedDeduplication
            };
        }

        /// <summary>
        /// Formats the SQS options as a JSON string for debugging and logging purposes.
        /// Sensitive information like access keys and secret keys are masked for security.
        /// </summary>
        /// <returns>A JSON representation of the options with sensitive data masked.</returns>
        /// <remarks>
        /// This method overrides the base implementation to include SQS-specific properties
        /// while ensuring that sensitive authentication information is not exposed in logs.
        /// </remarks>
        [EditorBrowsable(EditorBrowsableState.Never)]
        string IOptionsFormatter.Format()
        {
            var options = new
            {
                // Base properties
                BatchSize = this.BatchSize,
                NewBatchThreshold = this.NewBatchThreshold,
                MaxPollingInterval = this.MaxPollingInterval,
                MaxDequeueCount = this.MaxDequeueCount,
                VisibilityTimeout = this.VisibilityTimeout,
                MessageEncoding = this.MessageEncoding.ToString(),

                // SQS-specific properties (with sensitive data masked)
                AccessKey = string.IsNullOrWhiteSpace(this.AccessKey) ? null : "***MASKED***",
                Region = this.Region,
                SecretKey = string.IsNullOrWhiteSpace(this.SecretKey) ? null : "***MASKED***",
                ServiceUrl = this.ServiceUrl,
                UseFifo = this.UseFifo,
                MessageGroupId = this.MessageGroupId,
                UseContentBasedDeduplication = this.UseContentBasedDeduplication
            };

            return JsonSerializer.Serialize(options, JsonSerialization.Options);
        }

        #endregion

    }

}
// Copyright (c) CloudNimble, Inc. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Amazon;
using Amazon.Runtime;
using Amazon.SQS;
using Amazon.SQS.Model;
using CloudNimble.WebJobs.Extensions.Tests.Common;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CloudNimble.WebJobs.Extensions.Tests.Amazon.Integration
{

    /// <summary>
    /// Base class for integration tests that use LocalStack to emulate AWS services.
    /// </summary>
    [TestCategory("Integration")]
    public abstract class LocalStackTestBase : WebJobsTestBase
    {

        #region Protected Properties

        /// <summary>
        /// Gets the LocalStack endpoint URL. Defaults to http://localhost:4566.
        /// </summary>
        protected virtual string LocalStackEndpoint => 
            Environment.GetEnvironmentVariable("LOCALSTACK_ENDPOINT") ?? 
            Environment.GetEnvironmentVariable("SQS_ENDPOINT_URL") ?? 
            "http://localhost:4566";

        /// <summary>
        /// Gets the AWS region to use for tests. Defaults to us-east-1.
        /// </summary>
        protected virtual string AwsRegion => 
            Environment.GetEnvironmentVariable("AWS_DEFAULT_REGION") ?? 
            "us-east-1";

        /// <summary>
        /// Gets the AWS access key for LocalStack. Any value works with LocalStack.
        /// </summary>
        protected virtual string AwsAccessKey => 
            Environment.GetEnvironmentVariable("AWS_ACCESS_KEY_ID") ?? 
            "test";

        /// <summary>
        /// Gets the AWS secret key for LocalStack. Any value works with LocalStack.
        /// </summary>
        protected virtual string AwsSecretKey => 
            Environment.GetEnvironmentVariable("AWS_SECRET_ACCESS_KEY") ?? 
            "test";

        /// <summary>
        /// Gets the SQS client configured for LocalStack.
        /// </summary>
        protected IAmazonSQS SqsClient { get; private set; }

        /// <summary>
        /// Gets a unique test run ID to ensure test isolation.
        /// </summary>
        protected string TestRunId { get; } = Guid.NewGuid().ToString("N").Substring(0, 8);

        #endregion

        #region Setup and Cleanup

        [TestInitialize]
        public override void TestSetup()
        {
            base.TestSetup();
            InitializeSqsClient();
        }

        [TestCleanup]
        public override void TestCleanup()
        {
            CleanupTestResourcesAsync().GetAwaiter().GetResult();
            SqsClient?.Dispose();
            base.TestCleanup();
        }

        #endregion

        #region Protected Methods

        /// <summary>
        /// Registers LocalStack-specific services in the DI container.
        /// </summary>
        public override void RegisterBaseServices()
        {
            base.RegisterBaseServices();
            
            TestHostBuilder.ConfigureServices((context, services) =>
            {
                // Register AWS services
                services.AddSingleton<IAmazonSQS>(provider =>
                {
                    var config = new AmazonSQSConfig
                    {
                        ServiceURL = LocalStackEndpoint,
                        RegionEndpoint = RegionEndpoint.GetBySystemName(AwsRegion),
                        UseHttp = true,
                        AuthenticationRegion = AwsRegion
                    };

                    var credentials = new BasicAWSCredentials(AwsAccessKey, AwsSecretKey);
                    return new AmazonSQSClient(credentials, config);
                });

                // Configure SQS options for LocalStack
                services.Configure<CloudNimble.WebJobs.Extensions.Amazon.SQS.SQSOptions>(options =>
                {
                    options.ServiceUrl = LocalStackEndpoint;
                    options.Region = AwsRegion;
                    options.AccessKey = AwsAccessKey;
                    options.SecretKey = AwsSecretKey;
                });
            });
        }

        /// <summary>
        /// Creates a test queue with a unique name.
        /// </summary>
        /// <param name="queueNamePrefix">The prefix for the queue name.</param>
        /// <param name="isFifo">Whether to create a FIFO queue.</param>
        /// <returns>The URL of the created queue.</returns>
        protected async Task<string> CreateTestQueueAsync(string queueNamePrefix = "test", bool isFifo = false)
        {
            var queueName = $"{queueNamePrefix}-{TestRunId}";
            if (isFifo && !queueName.EndsWith(".fifo"))
            {
                queueName += ".fifo";
            }

            var request = new CreateQueueRequest
            {
                QueueName = queueName,
                Attributes = new Dictionary<string, string>()
            };

            if (isFifo)
            {
                request.Attributes["FifoQueue"] = "true";
                request.Attributes["ContentBasedDeduplication"] = "false";
            }

            var response = await SqsClient.CreateQueueAsync(request);
            return response.QueueUrl;
        }

        /// <summary>
        /// Deletes a test queue.
        /// </summary>
        /// <param name="queueUrl">The URL of the queue to delete.</param>
        protected async Task DeleteTestQueueAsync(string queueUrl)
        {
            try
            {
                await SqsClient.DeleteQueueAsync(queueUrl);
            }
            catch (Exception ex)
            {
                // Log but don't fail tests if cleanup fails
                Console.WriteLine($"Failed to delete queue {queueUrl}: {ex.Message}");
            }
        }

        /// <summary>
        /// Sends a test message to a queue.
        /// </summary>
        /// <param name="queueUrl">The URL of the queue.</param>
        /// <param name="messageBody">The message body.</param>
        /// <param name="messageGroupId">The message group ID for FIFO queues.</param>
        /// <returns>The message ID.</returns>
        protected async Task<string> SendTestMessageAsync(string queueUrl, string messageBody, string messageGroupId = null)
        {
            var request = new SendMessageRequest
            {
                QueueUrl = queueUrl,
                MessageBody = messageBody
            };

            if (!string.IsNullOrEmpty(messageGroupId))
            {
                request.MessageGroupId = messageGroupId;
            }

            var response = await SqsClient.SendMessageAsync(request);
            return response.MessageId;
        }

        /// <summary>
        /// Receives messages from a queue.
        /// </summary>
        /// <param name="queueUrl">The URL of the queue.</param>
        /// <param name="maxMessages">The maximum number of messages to receive.</param>
        /// <param name="waitTimeSeconds">The wait time for long polling.</param>
        /// <returns>The received messages.</returns>
        protected async Task<List<Message>> ReceiveTestMessagesAsync(string queueUrl, int maxMessages = 1, int waitTimeSeconds = 0)
        {
            var request = new ReceiveMessageRequest
            {
                QueueUrl = queueUrl,
                MaxNumberOfMessages = maxMessages,
                WaitTimeSeconds = waitTimeSeconds,
                MessageSystemAttributeNames = new List<string> { "All" }
            };

            var response = await SqsClient.ReceiveMessageAsync(request);
            return response.Messages;
        }

        /// <summary>
        /// Purges all messages from a queue.
        /// </summary>
        /// <param name="queueUrl">The URL of the queue to purge.</param>
        protected async Task PurgeTestQueueAsync(string queueUrl)
        {
            try
            {
                await SqsClient.PurgeQueueAsync(new PurgeQueueRequest { QueueUrl = queueUrl });
            }
            catch (Exception ex)
            {
                // Log but don't fail tests if purge fails
                Console.WriteLine($"Failed to purge queue {queueUrl}: {ex.Message}");
            }
        }

        /// <summary>
        /// Checks if LocalStack is available and healthy.
        /// </summary>
        /// <returns>True if LocalStack is available; otherwise, false.</returns>
        protected async Task<bool> IsLocalStackAvailableAsync()
        {
            try
            {
                // Try to list queues as a health check
                await SqsClient.ListQueuesAsync(new ListQueuesRequest());
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Skips the test if LocalStack is not available.
        /// </summary>
        protected async Task SkipIfLocalStackNotAvailable()
        {
            if (!await IsLocalStackAvailableAsync())
            {
                Assert.Inconclusive("LocalStack is not available. Please ensure LocalStack is running on " + LocalStackEndpoint);
            }
        }

        #endregion

        #region Private Methods

        private void InitializeSqsClient()
        {
            var config = new AmazonSQSConfig
            {
                ServiceURL = LocalStackEndpoint,
                RegionEndpoint = RegionEndpoint.GetBySystemName(AwsRegion),
                UseHttp = true,
                AuthenticationRegion = AwsRegion
            };

            var credentials = new BasicAWSCredentials(AwsAccessKey, AwsSecretKey);
            SqsClient = new AmazonSQSClient(credentials, config);
        }

        private async Task CleanupTestResourcesAsync()
        {
            try
            {
                // List and delete all queues created by this test run
                var response = await SqsClient.ListQueuesAsync(new ListQueuesRequest
                {
                    QueueNamePrefix = $"{TestRunId}"
                });

                foreach (var queueUrl in response.QueueUrls)
                {
                    await DeleteTestQueueAsync(queueUrl);
                }
            }
            catch
            {
                // Ignore cleanup errors
            }
        }

        #endregion

    }

}
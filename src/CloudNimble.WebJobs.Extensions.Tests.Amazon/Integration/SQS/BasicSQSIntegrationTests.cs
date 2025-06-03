// Copyright (c) CloudNimble, Inc. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Amazon.SQS;
using CloudNimble.WebJobs.Extensions.Amazon.SQS;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace CloudNimble.WebJobs.Extensions.Tests.Amazon.Integration.SQS
{

    /// <summary>
    /// Basic integration tests for SQS with LocalStack.
    /// </summary>
    [TestClass]
    [TestCategory("Integration")]
    public class BasicSQSIntegrationTests : LocalStackTestBase
    {

        private IAmazonSQS _sqsClient;
        private ILoggerFactory _loggerFactory;
        private IOptions<SQSOptions> _sqsOptions;

        [TestInitialize]
        public async Task Setup()
        {
            base.TestSetup();
            await SkipIfLocalStackNotAvailable();

            _sqsClient = GetService<IAmazonSQS>();
            _loggerFactory = GetService<ILoggerFactory>();
            _sqsOptions = GetService<IOptions<SQSOptions>>();
        }

        [TestMethod]
        public async Task SQSClient_CanConnectToLocalStack()
        {
            // Act & Assert - This should not throw
            var response = await _sqsClient.ListQueuesAsync(new global::Amazon.SQS.Model.ListQueuesRequest());
            response.Should().NotBeNull();
            response.HttpStatusCode.Should().Be(System.Net.HttpStatusCode.OK);
        }

        [TestMethod]
        public async Task SQSQueue_BasicOperations_Work()
        {
            // Arrange
            var queueName = $"basic-test-{TestRunId}";
            var queue = new SQSQueue(queueName, _sqsClient, _loggerFactory, _sqsOptions);

            // Act - Create queue by sending a message
            await queue.AddMessageAndCreateIfNotExistsAsync("Test message", CancellationToken.None);

            // Assert - Queue should exist
            var exists = await queue.ExistsAsync(CancellationToken.None);
            exists.Should().BeTrue();

            // Cleanup
            var queueUrl = await _sqsClient.GetQueueUrlAsync(queueName);
            await DeleteTestQueueAsync(queueUrl.QueueUrl);
        }

        [TestMethod]
        public async Task SQSMessage_CanBeCreatedFromAmazonMessage()
        {
            // Arrange
            var queueUrl = await CreateTestQueueAsync($"message-test-{TestRunId}");
            await SendTestMessageAsync(queueUrl, "Test message body");
            
            // Act
            var messages = await ReceiveTestMessagesAsync(queueUrl);
            messages.Should().HaveCount(1);
            
            var sqsMessage = new SQSMessage(messages[0], queueUrl);

            // Assert
            sqsMessage.Should().NotBeNull();
            sqsMessage.Id.Should().Be(messages[0].MessageId);
            sqsMessage.Body.Should().Be("Test message body");
            sqsMessage.PopReceipt.Should().Be(messages[0].ReceiptHandle);
            sqsMessage.QueueUrl.Should().Be(queueUrl);

            // Cleanup
            await DeleteTestQueueAsync(queueUrl);
        }

        [TestMethod]
        public async Task EndToEnd_SendAndReceiveMessage()
        {
            // Arrange
            var queueName = $"e2e-test-{TestRunId}";
            var queue = new SQSQueue(queueName, _sqsClient, _loggerFactory, _sqsOptions);
            var messageBody = "End-to-end test message";

            // Act - Send message
            await queue.AddMessageAndCreateIfNotExistsAsync(messageBody, CancellationToken.None);

            // Act - Receive message
            var response = await queue.ReceiveMessagesAsync<SQSMessage>(1, TimeSpan.Zero, CancellationToken.None);

            // Assert
            response.Should().NotBeNull();
            response.Value.Should().HaveCount(1);
            response.Value[0].Body.Should().Be(messageBody);

            // Act - Delete message
            await queue.DeleteMessageAsync(
                response.Value[0].Id, 
                response.Value[0].PopReceipt, 
                CancellationToken.None);

            // Assert - Queue should be empty
            var emptyResponse = await queue.ReceiveMessagesAsync<SQSMessage>(1, TimeSpan.Zero, CancellationToken.None);
            emptyResponse.Value.Should().BeEmpty();

            // Cleanup
            var queueUrl = await _sqsClient.GetQueueUrlAsync(queueName);
            await DeleteTestQueueAsync(queueUrl.QueueUrl);
        }

    }

}
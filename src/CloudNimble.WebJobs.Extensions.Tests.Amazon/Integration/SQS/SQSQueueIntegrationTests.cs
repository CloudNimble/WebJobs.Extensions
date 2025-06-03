// Copyright (c) CloudNimble, Inc. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Amazon.SQS;
using CloudNimble.WebJobs.Extensions.Amazon.SQS;
using CloudNimble.WebJobs.Extensions.Common.Queues;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CloudNimble.WebJobs.Extensions.Tests.Amazon.Integration.SQS
{

    /// <summary>
    /// Integration tests for <see cref="SQSQueue"/> using LocalStack.
    /// </summary>
    [TestClass]
    [TestCategory("Integration")]
    public class SQSQueueIntegrationTests : LocalStackTestBase
    {

        private IAmazonSQS _sqsClient;
        private ILoggerFactory _loggerFactory;
        private IOptions<SQSOptions> _sqsOptions;
        private string _testQueueUrl;
        private string _testQueueName;
        private SQSQueue _sqsQueue;

        [TestInitialize]
        public async Task Setup()
        {
            base.TestSetup();
            await SkipIfLocalStackNotAvailable();

            _sqsClient = GetService<IAmazonSQS>();
            _loggerFactory = GetService<ILoggerFactory>();
            _sqsOptions = GetService<IOptions<SQSOptions>>();
            
            _testQueueName = $"sqsqueue-test-{TestRunId}";
            _testQueueUrl = await CreateTestQueueAsync(_testQueueName);
            
            // Extract queue name from URL for SQSQueue constructor
            _sqsQueue = new SQSQueue(_testQueueName, _sqsClient, _loggerFactory, _sqsOptions);
        }

        [TestCleanup]
        public async Task Cleanup()
        {
            if (!string.IsNullOrEmpty(_testQueueUrl))
            {
                await DeleteTestQueueAsync(_testQueueUrl);
            }
            base.TestCleanup();
        }

        [TestMethod]
        public async Task ExistsAsync_WithExistingQueue_ReturnsTrue()
        {
            // Act
            var exists = await _sqsQueue.ExistsAsync(CancellationToken.None);

            // Assert
            exists.Should().BeTrue();
        }

        [TestMethod]
        public async Task ExistsAsync_WithNonExistentQueue_ReturnsFalse()
        {
            // Arrange
            var nonExistentQueue = new SQSQueue("non-existent-queue", _sqsClient, _loggerFactory, _sqsOptions);

            // Act
            var exists = await nonExistentQueue.ExistsAsync(CancellationToken.None);

            // Assert
            exists.Should().BeFalse();
        }

        [TestMethod]
        public async Task AddMessageAndCreateIfNotExistsAsync_SendsMessageSuccessfully()
        {
            // Arrange
            const string messageContent = "Test message content";

            // Act
            await _sqsQueue.AddMessageAndCreateIfNotExistsAsync(messageContent, CancellationToken.None);

            // Assert - Verify message was sent by receiving it
            var messages = await ReceiveTestMessagesAsync(_testQueueUrl);
            messages.Should().HaveCount(1);
            messages[0].Body.Should().Be(messageContent);
        }

        [TestMethod]
        public async Task ReceiveMessagesAsync_RetrievesMessages()
        {
            // Arrange
            var messageIds = new List<string>();
            for (int i = 0; i < 3; i++)
            {
                var id = await SendTestMessageAsync(_testQueueUrl, $"Message {i}");
                messageIds.Add(id);
            }

            // Act
            var response = await _sqsQueue.ReceiveMessagesAsync<SQSMessage>(5, TimeSpan.Zero, CancellationToken.None);

            // Assert
            response.Should().NotBeNull();
            response.Value.Should().NotBeNull();
            response.Value.Should().HaveCount(3);
            response.Value.Select(m => m.Id).Should().BeEquivalentTo(messageIds);
            response.Value.Select(m => m.Body).Should().BeEquivalentTo(new[] { "Message 0", "Message 1", "Message 2" });
        }

        [TestMethod]
        public async Task ReceiveMessagesAsync_WithVisibilityTimeout_HidesMessagesTemporarily()
        {
            // Arrange
            await SendTestMessageAsync(_testQueueUrl, "Test message");
            var visibilityTimeout = TimeSpan.FromSeconds(5);

            // Act - Get message with visibility timeout
            var firstBatch = await _sqsQueue.ReceiveMessagesAsync<SQSMessage>(1, visibilityTimeout, CancellationToken.None);
            
            // Try to get messages again immediately
            var secondBatch = await _sqsQueue.ReceiveMessagesAsync<SQSMessage>(1, TimeSpan.Zero, CancellationToken.None);

            // Assert
            firstBatch.Value.Should().HaveCount(1);
            secondBatch.Value.Should().BeEmpty(); // Message should be hidden
        }

        [TestMethod]
        public async Task DeleteMessageAsync_RemovesMessageFromQueue()
        {
            // Arrange
            await SendTestMessageAsync(_testQueueUrl, "Message to delete");
            var response = await _sqsQueue.ReceiveMessagesAsync<SQSMessage>(1, TimeSpan.Zero, CancellationToken.None);
            var message = response.Value.First();

            // Act
            await _sqsQueue.DeleteMessageAsync(message.Id, message.PopReceipt, CancellationToken.None);

            // Assert - Message should be gone
            var remainingMessages = await _sqsQueue.ReceiveMessagesAsync<SQSMessage>(10, TimeSpan.Zero, CancellationToken.None);
            remainingMessages.Value.Should().BeEmpty();
        }

        [TestMethod]
        public async Task UpdateMessageAsync_UpdatesVisibilityTimeout()
        {
            // Arrange
            await SendTestMessageAsync(_testQueueUrl, "Message to update");
            var response = await _sqsQueue.ReceiveMessagesAsync<SQSMessage>(1, TimeSpan.FromSeconds(30), CancellationToken.None);
            var message = response.Value.First();
            var newVisibilityTimeout = TimeSpan.FromSeconds(60);

            // Act
            var updateReceipt = await _sqsQueue.UpdateMessageAsync(message.Id, message.PopReceipt, newVisibilityTimeout, CancellationToken.None);

            // Assert
            updateReceipt.Should().NotBeNull();
            updateReceipt.PopReceipt.Should().NotBeNullOrEmpty();
            updateReceipt.NextVisibleOn.Should().BeAfter(DateTimeOffset.UtcNow);
            
            // The message should still be hidden
            var checkMessages = await _sqsQueue.ReceiveMessagesAsync<SQSMessage>(1, TimeSpan.Zero, CancellationToken.None);
            checkMessages.Value.Should().BeEmpty();
        }

        [TestMethod]
        public async Task GetPropertiesAsync_ReturnsQueueProperties()
        {
            // Arrange
            // Send some messages
            for (int i = 0; i < 5; i++)
            {
                await SendTestMessageAsync(_testQueueUrl, $"Count test message {i}");
            }

            // Act
            var properties = await _sqsQueue.GetPropertiesAsync();

            // Assert
            properties.Should().NotBeNull();
            properties.ApproximateMessagesCount.Should().BeGreaterThanOrEqualTo(5);
        }

        [TestMethod]
        public void Properties_ReturnCorrectValues()
        {
            // Assert
            _sqsQueue.Name.Should().Be(_testQueueName);
            _sqsQueue.AccountName.Should().NotBeNullOrEmpty();
        }

        [TestMethod]
        public async Task FifoQueue_Operations_WorkCorrectly()
        {
            // Arrange
            var fifoQueueName = $"fifo-queue-{TestRunId}.fifo";
            var fifoQueueUrl = await CreateTestQueueAsync(fifoQueueName, isFifo: true);
            var fifoQueue = new SQSQueue(fifoQueueName, _sqsClient, _loggerFactory, _sqsOptions);

            // Act - Send FIFO message with message group ID
            await fifoQueue.AddMessageAndCreateIfNotExistsAsync("FIFO message", CancellationToken.None);

            // Assert - Receive FIFO message
            var response = await fifoQueue.ReceiveMessagesAsync<SQSMessage>(1, TimeSpan.Zero, CancellationToken.None);
            response.Value.Should().HaveCount(1);
            response.Value[0].Body.Should().Be("FIFO message");

            // Cleanup
            await DeleteTestQueueAsync(fifoQueueUrl);
        }

        [TestMethod]
        public async Task BatchOperations_HandleMultipleMessagesEfficiently()
        {
            // Arrange
            var messageBodies = Enumerable.Range(1, 10).Select(i => $"Batch message {i}").ToList();

            // Act - Send messages individually (SQS batch send would require different method)
            foreach (var body in messageBodies)
            {
                await _sqsQueue.AddMessageAndCreateIfNotExistsAsync(body, CancellationToken.None);
            }

            // Act - Receive messages in batch
            var receivedMessages = await _sqsQueue.ReceiveMessagesAsync<SQSMessage>(10, TimeSpan.Zero, CancellationToken.None);

            // Assert
            receivedMessages.Value.Should().HaveCount(10);
            receivedMessages.Value.Select(m => m.Body).Should().BeEquivalentTo(messageBodies);
        }

        [TestMethod]
        public void QueueName_Validation_WorksCorrectly()
        {
            // Act & Assert - Valid queue name
            SQSQueue.IsValidQueueName("valid-queue-name", out var errorMessage).Should().BeTrue();
            errorMessage.Should().BeNull();

            // Act & Assert - Invalid queue names
            SQSQueue.IsValidQueueName("", out errorMessage).Should().BeFalse();
            errorMessage.Should().NotBeNullOrEmpty();

            SQSQueue.IsValidQueueName("queue with spaces", out errorMessage).Should().BeFalse();
            errorMessage.Should().NotBeNullOrEmpty();

            SQSQueue.IsValidQueueName("queue@invalid", out errorMessage).Should().BeFalse();
            errorMessage.Should().NotBeNullOrEmpty();
        }

    }

}
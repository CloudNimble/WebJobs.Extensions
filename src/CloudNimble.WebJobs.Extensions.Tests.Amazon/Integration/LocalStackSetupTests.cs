// Copyright (c) CloudNimble, Inc. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading.Tasks;

namespace CloudNimble.WebJobs.Extensions.Tests.Amazon.Integration
{

    /// <summary>
    /// Tests to verify LocalStack setup and connectivity.
    /// </summary>
    [TestClass]
    [TestCategory("Integration")]
    public class LocalStackSetupTests : LocalStackTestBase
    {

        [TestMethod]
        public async Task LocalStack_ShouldBeAvailable()
        {
            // Arrange & Act
            await SkipIfLocalStackNotAvailable();

            // Assert - If we got here, LocalStack is available
            var isAvailable = await IsLocalStackAvailableAsync();
            isAvailable.Should().BeTrue();
        }

        [TestMethod]
        public async Task LocalStack_CanCreateAndDeleteQueue()
        {
            // Arrange
            await SkipIfLocalStackNotAvailable();

            // Act
            var queueUrl = await CreateTestQueueAsync("setup-test");
            
            // Assert
            queueUrl.Should().NotBeNullOrEmpty();
            queueUrl.Should().Contain(LocalStackEndpoint);
            queueUrl.Should().Contain($"setup-test-{TestRunId}");

            // Cleanup
            await DeleteTestQueueAsync(queueUrl);
        }

        [TestMethod]
        public async Task LocalStack_CanSendAndReceiveMessage()
        {
            // Arrange
            await SkipIfLocalStackNotAvailable();
            var queueUrl = await CreateTestQueueAsync("messaging-test");
            const string messageBody = "Hello from LocalStack!";

            // Act - Send message
            var messageId = await SendTestMessageAsync(queueUrl, messageBody);
            
            // Assert - Message sent
            messageId.Should().NotBeNullOrEmpty();

            // Act - Receive message
            var messages = await ReceiveTestMessagesAsync(queueUrl, maxMessages: 10, waitTimeSeconds: 1);

            // Assert - Message received
            messages.Should().NotBeNullOrEmpty();
            messages.Should().HaveCount(1);
            messages[0].Body.Should().Be(messageBody);
            messages[0].MessageId.Should().Be(messageId);

            // Cleanup
            await DeleteTestQueueAsync(queueUrl);
        }

        [TestMethod]
        public async Task LocalStack_CanCreateFifoQueue()
        {
            // Arrange
            await SkipIfLocalStackNotAvailable();

            // Act
            var queueUrl = await CreateTestQueueAsync("fifo-test", isFifo: true);

            // Assert
            queueUrl.Should().NotBeNullOrEmpty();
            queueUrl.Should().Contain($"fifo-test-{TestRunId}.fifo");

            // Act - Send FIFO message
            var messageId = await SendTestMessageAsync(queueUrl, "FIFO message", messageGroupId: "test-group");

            // Assert
            messageId.Should().NotBeNullOrEmpty();

            // Cleanup
            await DeleteTestQueueAsync(queueUrl);
        }

        [TestMethod]
        public async Task LocalStack_CanListQueues()
        {
            // Arrange
            await SkipIfLocalStackNotAvailable();
            
            // Create multiple test queues
            var queue1 = await CreateTestQueueAsync("list-test-1");
            var queue2 = await CreateTestQueueAsync("list-test-2");

            // Act
            var listResponse = await SqsClient.ListQueuesAsync($"list-test-");

            // Assert
            listResponse.Should().NotBeNull();
            listResponse.QueueUrls.Should().NotBeNull();
            // Note: Other tests might create queues, so we check for at least our queues
            listResponse.QueueUrls.Should().Contain(url => url.Contains($"list-test-1-{TestRunId}"));
            listResponse.QueueUrls.Should().Contain(url => url.Contains($"list-test-2-{TestRunId}"));

            // Cleanup
            await DeleteTestQueueAsync(queue1);
            await DeleteTestQueueAsync(queue2);
        }

        [TestMethod]
        public async Task LocalStack_ConfigurationIsCorrect()
        {
            // Arrange & Act
            await SkipIfLocalStackNotAvailable();

            // Assert - Verify configuration
            LocalStackEndpoint.Should().NotBeNullOrEmpty();
            AwsRegion.Should().NotBeNullOrEmpty();
            AwsAccessKey.Should().NotBeNullOrEmpty();
            AwsSecretKey.Should().NotBeNullOrEmpty();
            
            // Common LocalStack endpoints
            LocalStackEndpoint.Should().MatchRegex(@"^https?://[^/]+:\d+/?$");
        }

    }

}
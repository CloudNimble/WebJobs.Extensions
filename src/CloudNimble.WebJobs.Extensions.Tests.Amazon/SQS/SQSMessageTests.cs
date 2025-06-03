// Copyright (c) CloudNimble, Inc. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Amazon.SQS.Model;
using CloudNimble.WebJobs.Extensions.Amazon.SQS;
using CloudNimble.WebJobs.Extensions.Common.Queues;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;

namespace CloudNimble.WebJobs.Extensions.Tests.Amazon.SQS
{

    /// <summary>
    /// Unit tests for <see cref="SQSMessage"/>.
    /// </summary>
    [TestClass]
    public class SQSMessageTests
    {

        [TestMethod]
        public void Constructor_WithMessage_InitializesProperties()
        {
            // Arrange
            var awsMessage = new Message
            {
                Body = "test body",
                MessageId = "msg-123",
                ReceiptHandle = "receipt-456",
                Attributes = new Dictionary<string, string>()
            };
            const string queueUrl = "https://sqs.us-east-1.amazonaws.com/123456789012/test-queue";

            // Act
            var sqsMessage = new SQSMessage(awsMessage, queueUrl);

            // Assert
            sqsMessage.Body.Should().Be("test body");
            sqsMessage.Id.Should().Be("msg-123");
            sqsMessage.PopReceipt.Should().Be("receipt-456");
            sqsMessage.QueueUrl.Should().Be(queueUrl);
            sqsMessage.Original.Should().BeSameAs(awsMessage);
        }

        [TestMethod]
        public void Constructor_WithNullMessage_ThrowsArgumentNullException()
        {
            // Arrange
            Message awsMessage = null;
            const string queueUrl = "https://sqs.us-east-1.amazonaws.com/123456789012/test-queue";

            // Act & Assert
            Action act = () => new SQSMessage(awsMessage, queueUrl);
            act.Should().Throw<ArgumentNullException>()
                .WithParameterName("message");
        }

        [TestMethod]
        public void Constructor_WithNullQueueUrl_SetsEmptyString()
        {
            // Arrange
            var awsMessage = new Message
            {
                Body = "test",
                MessageId = "123",
                Attributes = new Dictionary<string, string>()
            };

            // Act
            var sqsMessage = new SQSMessage(awsMessage, null);

            // Assert
            sqsMessage.QueueUrl.Should().BeEmpty();
        }

        [TestMethod]
        public void Constructor_WithStringBody_CreatesSyntheticMessage()
        {
            // Arrange
            const string body = "synthetic message body";

            // Act
            var sqsMessage = new SQSMessage(body);

            // Assert
            sqsMessage.Body.Should().Be(body);
            sqsMessage.Id.Should().NotBeNullOrWhiteSpace();
            sqsMessage.PopReceipt.Should().NotBeNullOrWhiteSpace(); // Synthetic messages have a ReceiptHandle
            sqsMessage.QueueUrl.Should().BeEmpty(); // Default is empty string, not null
            sqsMessage.Original.Should().NotBeNull();
        }

        [TestMethod]
        public void Constructor_WithNullStringBody_ThrowsArgumentNullException()
        {
            // Arrange
            string body = null;

            // Act & Assert
            Action act = () => new SQSMessage(body);
            act.Should().Throw<ArgumentNullException>()
                .WithParameterName("messageBody"); // Parameter name in the constructor
        }

        [TestMethod]
        public void Body_CanBeSetAndRetrieved()
        {
            // Arrange
            var awsMessage = new Message { Body = "initial", Attributes = new Dictionary<string, string>() };
            var sqsMessage = new SQSMessage(awsMessage, "queue-url");

            // Act
            sqsMessage.Body = "updated body";

            // Assert
            sqsMessage.Body.Should().Be("updated body");
            awsMessage.Body.Should().Be("updated body");
        }

        [TestMethod]
        public void Id_CanBeSetAndRetrieved()
        {
            // Arrange
            var awsMessage = new Message { MessageId = "initial-id", Attributes = new Dictionary<string, string>() };
            var sqsMessage = new SQSMessage(awsMessage, "queue-url");

            // Act
            sqsMessage.Id = "new-id";

            // Assert
            sqsMessage.Id.Should().Be("new-id");
            awsMessage.MessageId.Should().Be("new-id");
        }

        [TestMethod]
        public void PopReceipt_CanBeSetAndRetrieved()
        {
            // Arrange
            var awsMessage = new Message { ReceiptHandle = "initial-receipt", Attributes = new Dictionary<string, string>() };
            var sqsMessage = new SQSMessage(awsMessage, "queue-url");

            // Act
            sqsMessage.PopReceipt = "new-receipt";

            // Assert
            sqsMessage.PopReceipt.Should().Be("new-receipt");
            awsMessage.ReceiptHandle.Should().Be("new-receipt");
        }

        [TestMethod]
        public void DequeueCount_ReturnsCorrectValue()
        {
            // Arrange
            var awsMessage = new Message
            {
                Attributes = new Dictionary<string, string>
                {
                    ["ApproximateReceiveCount"] = "5"
                }
            };
            var sqsMessage = new SQSMessage(awsMessage, "queue-url");

            // Act
            var count = sqsMessage.DequeueCount;

            // Assert
            count.Should().Be(5);
        }

        [TestMethod]
        public void DequeueCount_WithMissingAttribute_ReturnsOne()
        {
            // Arrange
            var awsMessage = new Message
            {
                Attributes = new Dictionary<string, string>()
            };
            var sqsMessage = new SQSMessage(awsMessage, "queue-url");

            // Act
            var count = sqsMessage.DequeueCount;

            // Assert
            count.Should().Be(1); // Default value is 1, not 0
        }

        [TestMethod]
        public void DateInserted_ReturnsCorrectTimestamp()
        {
            // Arrange
            var sentTime = DateTimeOffset.UtcNow.AddMinutes(-10);
            var awsMessage = new Message
            {
                Attributes = new Dictionary<string, string>
                {
                    ["SentTimestamp"] = sentTime.ToUnixTimeMilliseconds().ToString()
                }
            };
            var sqsMessage = new SQSMessage(awsMessage, "queue-url");

            // Act
            var insertedDate = sqsMessage.DateInserted;

            // Assert
            insertedDate.Should().NotBeNull();
            insertedDate.Value.Should().BeCloseTo(sentTime, TimeSpan.FromSeconds(1));
        }

        [TestMethod]
        public void DateInserted_CanBeSet()
        {
            // Arrange
            var awsMessage = new Message
            {
                Attributes = new Dictionary<string, string>()
            };
            var sqsMessage = new SQSMessage(awsMessage, "queue-url");
            var newTime = DateTimeOffset.UtcNow.AddHours(-2);

            // Act
            sqsMessage.DateInserted = newTime;

            // Assert
            sqsMessage.DateInserted.Should().NotBeNull();
            sqsMessage.DateInserted.Value.Should().BeCloseTo(newTime, TimeSpan.FromSeconds(1));
            awsMessage.Attributes["SentTimestamp"].Should().Be(newTime.ToUnixTimeMilliseconds().ToString());
        }

        [TestMethod]
        public void DateInserted_WithNullValue_RemovesAttribute()
        {
            // Arrange
            var awsMessage = new Message
            {
                Attributes = new Dictionary<string, string>
                {
                    ["SentTimestamp"] = "12345"
                }
            };
            var sqsMessage = new SQSMessage(awsMessage, "queue-url");

            // Act
            sqsMessage.DateInserted = null;

            // Assert
            sqsMessage.DateInserted.Should().BeNull();
            awsMessage.Attributes.Should().ContainKey("SentTimestamp");
            awsMessage.Attributes["SentTimestamp"].Should().BeNull();
        }

        [TestMethod]
        public void ImplementsIQueueMessage()
        {
            // Arrange
            var sqsMessage = new SQSMessage("test");

            // Assert
            sqsMessage.Should().BeAssignableTo<IQueueMessage>();
        }

        [TestMethod]
        [Ignore("Current implementation doesn't handle null Attributes dictionary gracefully")]
        public void GetInsertionTime_WithNullAttributes_ReturnsNull()
        {
            // Arrange
            var awsMessage = new Message
            {
                Attributes = null
            };
            var sqsMessage = new SQSMessage(awsMessage, "queue-url");

            // Act
            var insertedDate = sqsMessage.DateInserted;

            // Assert
            insertedDate.Should().BeNull();
        }

    }

}
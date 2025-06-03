// Copyright (c) CloudNimble, Inc. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using CloudNimble.WebJobs.Extensions.Amazon.SQS;
using CloudNimble.WebJobs.Extensions.Amazon.SQS.Triggers;
using CloudNimble.WebJobs.Extensions.Tests.Amazon.SQS.TestHelpers;
using FluentAssertions;
using Microsoft.Azure.WebJobs;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace CloudNimble.WebJobs.Extensions.Tests.Amazon.SQS.Triggers
{

    /// <summary>
    /// Unit tests for <see cref="SQSMessageDirectConverter"/>.
    /// </summary>
    [TestClass]
    public class SQSMessageDirectConverterTests
    {

        private SQSMessageDirectConverter _converter;

        [TestInitialize]
        public void Initialize()
        {
            _converter = new SQSMessageDirectConverter();
        }

        [TestMethod]
        public void Convert_ReturnsSameInstance()
        {
            // Arrange
            var message = SQSTestHelpers.CreateTestMessage();

            // Act
            var result = _converter.Convert(message);

            // Assert
            result.Should().BeSameAs(message);
        }

        [TestMethod]
        public void Convert_WithNullInput_ThrowsArgumentNullException()
        {
            // Act & Assert
            _converter.Invoking(c => c.Convert(null))
                .Should().Throw<ArgumentNullException>()
                .WithParameterName("input");
        }

        [TestMethod]
        public void Convert_PreservesAllMessageProperties()
        {
            // Arrange
            var sentTime = DateTimeOffset.UtcNow.AddMinutes(-5);
            var message = SQSTestHelpers.CreateTestMessage(
                body: "test body",
                messageId: "msg-123",
                receiptHandle: "receipt-456",
                queueUrl: "https://sqs.us-east-1.amazonaws.com/123456789012/my-queue",
                dequeueCount: 3,
                sentTimestamp: sentTime);

            // Act
            var result = _converter.Convert(message);

            // Assert
            result.Body.Should().Be("test body");
            result.Id.Should().Be("msg-123");
            result.PopReceipt.Should().Be("receipt-456");
            result.QueueUrl.Should().Be("https://sqs.us-east-1.amazonaws.com/123456789012/my-queue");
            result.DequeueCount.Should().Be(3);
            result.DateInserted.Should().BeCloseTo(sentTime, TimeSpan.FromSeconds(1));
        }

        [TestMethod]
        public void Converter_ImplementsIConverter()
        {
            // Assert
            _converter.Should().BeAssignableTo<IConverter<SQSMessage, SQSMessage>>();
        }

        [TestMethod]
        public void Converter_IsInternal()
        {
            // Arrange
            var type = typeof(SQSMessageDirectConverter);

            // Assert
            type.IsNotPublic.Should().BeTrue();
        }

    }

}
// SqsListenerTests.cs
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;


// SqsQueueProcessorTests.cs
namespace CloudNimble.WebJobs.Extensions.AWS.Tests
{
    // SqsMessageConverterTests.cs
    [TestClass]
    public class SqsMessageConverterTests
    {

        [TestMethod]
        public void Convert_WhenMessageIsNull_ShouldThrowArgumentNullException()
        {
            // Arrange
            var converter = new SqsMessageConverter<string>();

            // Act & Assert
            FluentActions.Invoking(() => converter.Convert(null))
                .Should().Throw<ArgumentNullException>();
        }

        [TestMethod]
        public void Convert_WhenTargetIsString_ShouldReturnMessageBody()
        {
            // Arrange
            var converter = new SqsMessageConverter<string>();
            var message = new Message { Body = "test message" };

            // Act
            var result = converter.Convert(message);

            // Assert
            result.Should().Be("test message");
        }

        [TestMethod]
        public void Convert_WhenTargetIsObject_ShouldDeserializeJson()
        {
            // Arrange
            var converter = new SqsMessageConverter<TestMessage>();
            var message = new Message { Body = """{"id":"123","content":"test"}""" };

            // Act
            var result = converter.Convert(message);

            // Assert
            result.Should().NotBeNull();
            result.Id.Should().Be("123");
            result.Content.Should().Be("test");
        }

        [TestMethod]
        public void Convert_WhenJsonIsInvalid_ShouldThrowJsonException()
        {
            // Arrange
            var converter = new SqsMessageConverter<TestMessage>();
            var message = new Message { Body = "invalid json" };

            // Act & Assert
            FluentActions.Invoking(() => converter.Convert(message))
                .Should().Throw<JsonException>();
        }

        private class TestMessage
        {
            public string Id { get; set; }
            public string Content { get; set; }
        }
    }
}
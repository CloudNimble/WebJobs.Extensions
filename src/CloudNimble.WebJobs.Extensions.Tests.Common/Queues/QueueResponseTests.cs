// Copyright (c) CloudNimble, Inc. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using CloudNimble.WebJobs.Extensions.Common.Queues;
using CloudNimble.WebJobs.Extensions.Tests.Common.Models;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Linq;

namespace CloudNimble.WebJobs.Extensions.Tests.Common.Queues
{

    /// <summary>
    /// Tests for the <see cref="QueueResponse{TQueueMessage}"/> class.
    /// </summary>
    [TestClass]
    [TestCategory("Unit")]
    public class QueueResponseTests
    {

        #region Constructor Tests

        /// <summary>
        /// Tests that the default constructor initializes properties to default values.
        /// </summary>
        [TestMethod]
        public void Constructor_WhenCalled_ShouldInitializeToDefaults()
        {
            var response = new QueueResponse<TestQueueMessage>();

            response.ClientRequestId.Should().BeNull();
            response.Value.Should().BeNull();
        }

        #endregion

        #region ClientRequestId Property Tests

        /// <summary>
        /// Tests that ClientRequestId can be set and retrieved.
        /// </summary>
        [TestMethod]
        public void ClientRequestId_WhenSet_ShouldReturnCorrectValue()
        {
            var response = new QueueResponse<TestQueueMessage>();
            var requestId = "test-request-id-12345";

            response.ClientRequestId = requestId;

            response.ClientRequestId.Should().Be(requestId);
        }

        /// <summary>
        /// Tests that ClientRequestId can be set to null.
        /// </summary>
        [TestMethod]
        public void ClientRequestId_WhenSetToNull_ShouldAcceptNull()
        {
            var response = new QueueResponse<TestQueueMessage>
            {
                ClientRequestId = "initial-value"
            };

            response.ClientRequestId = null;

            response.ClientRequestId.Should().BeNull();
        }

        /// <summary>
        /// Tests that ClientRequestId can be set to empty string.
        /// </summary>
        [TestMethod]
        public void ClientRequestId_WhenSetToEmptyString_ShouldAcceptEmptyString()
        {
            var response = new QueueResponse<TestQueueMessage>();

            response.ClientRequestId = "";

            response.ClientRequestId.Should().BeEmpty();
        }

        /// <summary>
        /// Tests that ClientRequestId can be set to whitespace.
        /// </summary>
        [TestMethod]
        public void ClientRequestId_WhenSetToWhitespace_ShouldAcceptWhitespace()
        {
            var response = new QueueResponse<TestQueueMessage>();
            var whitespace = "   \t\n  ";

            response.ClientRequestId = whitespace;

            response.ClientRequestId.Should().Be(whitespace);
        }

        /// <summary>
        /// Tests that ClientRequestId can handle very long strings.
        /// </summary>
        [TestMethod]
        public void ClientRequestId_WhenSetToVeryLongString_ShouldHandleCorrectly()
        {
            var response = new QueueResponse<TestQueueMessage>();
            var longRequestId = new string('a', 10000);

            response.ClientRequestId = longRequestId;

            response.ClientRequestId.Should().Be(longRequestId);
            response.ClientRequestId.Should().HaveLength(10000);
        }

        #endregion

        #region Value Property Tests

        /// <summary>
        /// Tests that Value can be set to a list of messages.
        /// </summary>
        [TestMethod]
        public void Value_WhenSetToListOfMessages_ShouldReturnCorrectList()
        {
            var response = new QueueResponse<TestQueueMessage>();
            var messages = new List<TestQueueMessage>
            {
                new() { Id = "msg1", Body = "Message 1" },
                new() { Id = "msg2", Body = "Message 2" },
                new() { Id = "msg3", Body = "Message 3" }
            };

            response.Value = messages;

            response.Value.Should().NotBeNull();
            response.Value.Should().HaveCount(3);
            response.Value.Should().BeEquivalentTo(messages);
        }

        /// <summary>
        /// Tests that Value can be set to an empty list.
        /// </summary>
        [TestMethod]
        public void Value_WhenSetToEmptyList_ShouldReturnEmptyList()
        {
            var response = new QueueResponse<TestQueueMessage>();
            var emptyList = new List<TestQueueMessage>();

            response.Value = emptyList;

            response.Value.Should().NotBeNull();
            response.Value.Should().BeEmpty();
        }

        /// <summary>
        /// Tests that Value can be set to null.
        /// </summary>
        [TestMethod]
        public void Value_WhenSetToNull_ShouldAcceptNull()
        {
            var response = new QueueResponse<TestQueueMessage>
            {
                Value = new List<TestQueueMessage> { new() { Id = "test", Body = "test" } }
            };

            response.Value = null;

            response.Value.Should().BeNull();
        }

        /// <summary>
        /// Tests that Value can contain messages with null properties.
        /// </summary>
        [TestMethod]
        public void Value_WhenContainingMessagesWithNullProperties_ShouldHandleCorrectly()
        {
            var response = new QueueResponse<TestQueueMessage>();
            var messages = new List<TestQueueMessage>
            {
                new() { Id = null, Body = null },
                new() { Id = "valid-id", Body = null },
                new() { Id = null, Body = "valid-body" }
            };

            response.Value = messages;

            response.Value.Should().NotBeNull();
            response.Value.Should().HaveCount(3);
            response.Value[0].Id.Should().BeNull();
            response.Value[0].Body.Should().BeNull();
            response.Value[1].Id.Should().Be("valid-id");
            response.Value[1].Body.Should().BeNull();
            response.Value[2].Id.Should().BeNull();
            response.Value[2].Body.Should().Be("valid-body");
        }

        /// <summary>
        /// Tests that Value can be modified after being set.
        /// </summary>
        [TestMethod]
        public void Value_WhenModifiedAfterSet_ShouldReflectChanges()
        {
            var response = new QueueResponse<TestQueueMessage>();
            var messages = new List<TestQueueMessage>
            {
                new() { Id = "msg1", Body = "Message 1" }
            };

            response.Value = messages;
            response.Value.Add(new TestQueueMessage { Id = "msg2", Body = "Message 2" });

            response.Value.Should().HaveCount(2);
            response.Value.Should().Contain(msg => msg.Id == "msg2");
        }

        #endregion

        #region Generic Type Constraint Tests

        /// <summary>
        /// Tests that QueueResponse works with different IQueueMessage implementations.
        /// </summary>
        [TestMethod]
        public void QueueResponse_WhenUsedWithDifferentMessageTypes_ShouldWorkCorrectly()
        {
            var stringResponse = new QueueResponse<TestQueueMessage>();
            var customResponse = new QueueResponse<CustomTestQueueMessage>();

            stringResponse.Value = new List<TestQueueMessage>
            {
                new() { Id = "test", Body = "test body" }
            };

            customResponse.Value = new List<CustomTestQueueMessage>
            {
                new() { Id = "custom", Body = "custom body", CustomProperty = "custom value" }
            };

            stringResponse.Value.Should().HaveCount(1);
            customResponse.Value.Should().HaveCount(1);
            customResponse.Value[0].CustomProperty.Should().Be("custom value");
        }

        #endregion

        #region Integration Tests

        /// <summary>
        /// Tests that all properties work correctly together.
        /// </summary>
        [TestMethod]
        public void QueueResponse_WhenSettingMultipleProperties_ShouldMaintainAllValues()
        {
            var response = new QueueResponse<TestQueueMessage>();
            var requestId = "integration-test-request-id";
            var messages = new List<TestQueueMessage>
            {
                new() { Id = "integration1", Body = "Integration Message 1" },
                new() { Id = "integration2", Body = "Integration Message 2" }
            };

            response.ClientRequestId = requestId;
            response.Value = messages;

            response.ClientRequestId.Should().Be(requestId);
            response.Value.Should().NotBeNull();
            response.Value.Should().HaveCount(2);
            response.Value.Should().Contain(msg => msg.Id == "integration1");
            response.Value.Should().Contain(msg => msg.Id == "integration2");
        }

        /// <summary>
        /// Tests that properties remain independent when modified.
        /// </summary>
        [TestMethod]
        public void QueueResponse_WhenModifyingOneProperty_ShouldNotAffectOthers()
        {
            var response = new QueueResponse<TestQueueMessage>
            {
                ClientRequestId = "original-request-id",
                Value = new List<TestQueueMessage>
                {
                    new() { Id = "original", Body = "Original Message" }
                }
            };

            response.ClientRequestId = "modified-request-id";

            response.Value.Should().NotBeNull();
            response.Value.Should().HaveCount(1);
            response.Value[0].Id.Should().Be("original");
        }

        #endregion

        #region Performance Tests

        /// <summary>
        /// Tests that QueueResponse can handle large numbers of messages.
        /// </summary>
        [TestMethod]
        public void Value_WhenSetToLargeNumberOfMessages_ShouldHandleEfficiently()
        {
            var response = new QueueResponse<TestQueueMessage>();
            var largeMessageList = Enumerable.Range(1, 10000)
                .Select(i => new TestQueueMessage 
                { 
                    Id = $"msg-{i}", 
                    Body = $"Message content {i}" 
                })
                .ToList();

            response.Value = largeMessageList;

            response.Value.Should().NotBeNull();
            response.Value.Should().HaveCount(10000);
            response.Value.First().Id.Should().Be("msg-1");
            response.Value.Last().Id.Should().Be("msg-10000");
        }

        #endregion

        #region Edge Case Tests

        /// <summary>
        /// Tests that QueueResponse handles special characters in properties correctly.
        /// </summary>
        [TestMethod]
        public void QueueResponse_WhenUsingSpecialCharacters_ShouldHandleCorrectly()
        {
            var response = new QueueResponse<TestQueueMessage>();
            var specialRequestId = "req-id-with-unicode-🚀-and-symbols-@#$%";
            var messages = new List<TestQueueMessage>
            {
                new() 
                { 
                    Id = "special-chars-✓", 
                    Body = "Message with unicode 🎉 and newlines\n\r\t" 
                }
            };

            response.ClientRequestId = specialRequestId;
            response.Value = messages;

            response.ClientRequestId.Should().Be(specialRequestId);
            response.Value[0].Id.Should().Be("special-chars-✓");
            response.Value[0].Body.Should().Contain("🎉");
        }

        /// <summary>
        /// Tests that QueueResponse works with inherited message types.
        /// </summary>
        [TestMethod]
        public void QueueResponse_WhenUsingInheritedMessageTypes_ShouldWorkCorrectly()
        {
            var response = new QueueResponse<CustomTestQueueMessage>();
            var inheritedMessages = new List<CustomTestQueueMessage>
            {
                new() 
                { 
                    Id = "inherited1", 
                    Body = "Inherited message 1",
                    CustomProperty = "Custom value 1"
                },
                new() 
                { 
                    Id = "inherited2", 
                    Body = "Inherited message 2",
                    CustomProperty = "Custom value 2"
                }
            };

            response.Value = inheritedMessages;

            response.Value.Should().HaveCount(2);
            response.Value.Cast<CustomTestQueueMessage>().Should().AllSatisfy(msg =>
                msg.CustomProperty.Should().NotBeNullOrWhiteSpace());
        }

        #endregion

    }

    #region Test Helper Classes

    /// <summary>
    /// Custom test implementation that extends TestQueueMessage for inheritance testing.
    /// </summary>
    internal class CustomTestQueueMessage : TestQueueMessage
    {
        public string CustomProperty { get; set; }
    }

    #endregion

}
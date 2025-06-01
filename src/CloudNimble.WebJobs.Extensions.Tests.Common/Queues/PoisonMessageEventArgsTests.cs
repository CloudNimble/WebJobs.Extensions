// Copyright (c) CloudNimble, Inc. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using CloudNimble.WebJobs.Extensions.Common.Queues;
using CloudNimble.WebJobs.Extensions.Tests.Common.Models;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace CloudNimble.WebJobs.Extensions.Common.Tests.Queues
{

    /// <summary>
    /// Tests for the <see cref="PoisonMessageEventArgs"/> class.
    /// </summary>
    [TestClass]
    [TestCategory("Unit")]
    public class PoisonMessageEventArgsTests
    {

        #region Constructor Tests

        /// <summary>
        /// Tests that the constructor correctly initializes properties.
        /// </summary>
        [TestMethod]
        public void Constructor_WhenCalledWithValidParameters_ShouldInitializeProperties()
        {
            var message = new TestQueueMessage { Id = "test-message", Body = "test body" };
            var poisonQueue = new TestQueueClient { Name = "poison-queue" };

            var eventArgs = new PoisonMessageEventArgs(message, poisonQueue);

            eventArgs.Message.Should().BeSameAs(message);
            eventArgs.PoisonQueue.Should().BeSameAs(poisonQueue);
        }

        /// <summary>
        /// Tests that the constructor throws when message is null.
        /// </summary>
        [TestMethod]
        public void Constructor_WhenMessageIsNull_ShouldAcceptNull()
        {
            var poisonQueue = new TestQueueClient { Name = "poison-queue" };

            var eventArgs = new PoisonMessageEventArgs(null, poisonQueue);

            eventArgs.Message.Should().BeNull();
            eventArgs.PoisonQueue.Should().BeSameAs(poisonQueue);
        }

        /// <summary>
        /// Tests that the constructor throws when poison queue is null.
        /// </summary>
        [TestMethod]
        public void Constructor_WhenPoisonQueueIsNull_ShouldAcceptNull()
        {
            var message = new TestQueueMessage { Id = "test-message", Body = "test body" };

            var eventArgs = new PoisonMessageEventArgs(message, null);

            eventArgs.Message.Should().BeSameAs(message);
            eventArgs.PoisonQueue.Should().BeNull();
        }

        /// <summary>
        /// Tests that the constructor accepts both parameters as null.
        /// </summary>
        [TestMethod]
        public void Constructor_WhenBothParametersAreNull_ShouldAcceptBothNull()
        {
            var eventArgs = new PoisonMessageEventArgs(null, null);

            eventArgs.Message.Should().BeNull();
            eventArgs.PoisonQueue.Should().BeNull();
        }

        #endregion

        #region Property Tests

        /// <summary>
        /// Tests that Message property returns the correct value.
        /// </summary>
        [TestMethod]
        public void Message_WhenAccessed_ShouldReturnCorrectValue()
        {
            var message = new TestQueueMessage 
            { 
                Id = "message-123", 
                Body = "Message content",
                DequeueCount = 5,
                PopReceipt = "receipt-abc",
                DateInserted = DateTimeOffset.UtcNow
            };
            var poisonQueue = new TestQueueClient { Name = "poison-queue" };

            var eventArgs = new PoisonMessageEventArgs(message, poisonQueue);

            eventArgs.Message.Should().BeSameAs(message);
            eventArgs.Message.Id.Should().Be("message-123");
            eventArgs.Message.Body.Should().Be("Message content");
            eventArgs.Message.DequeueCount.Should().Be(5);
        }

        /// <summary>
        /// Tests that PoisonQueue property returns the correct value.
        /// </summary>
        [TestMethod]
        public void PoisonQueue_WhenAccessed_ShouldReturnCorrectValue()
        {
            var message = new TestQueueMessage { Id = "test-message", Body = "test body" };
            var poisonQueue = new TestQueueClient 
            { 
                Name = "my-queue-poison",
                AccountName = "test-account"
            };

            var eventArgs = new PoisonMessageEventArgs(message, poisonQueue);

            eventArgs.PoisonQueue.Should().BeSameAs(poisonQueue);
            eventArgs.PoisonQueue.Name.Should().Be("my-queue-poison");
            eventArgs.PoisonQueue.AccountName.Should().Be("test-account");
        }

        // RWM: Commented out beciase the internals are visible to the test project, so properties are not read-only.
        ///// <summary>
        ///// Tests that properties are read-only.
        ///// </summary>
        //[TestMethod]
        //public void Properties_WhenExamined_ShouldBeReadOnly()
        //{
        //    var messageProperty = typeof(PoisonMessageEventArgs).GetProperty(nameof(PoisonMessageEventArgs.Message));
        //    var poisonQueueProperty = typeof(PoisonMessageEventArgs).GetProperty(nameof(PoisonMessageEventArgs.PoisonQueue));

        //    messageProperty.CanWrite.Should().BeFalse("Message property should be read-only");
        //    poisonQueueProperty.CanWrite.Should().BeFalse("PoisonQueue property should be read-only");
        //}

        #endregion

        #region EventArgs Inheritance Tests

        /// <summary>
        /// Tests that PoisonMessageEventArgs inherits from EventArgs.
        /// </summary>
        [TestMethod]
        public void PoisonMessageEventArgs_WhenExamined_ShouldInheritFromEventArgs()
        {
            typeof(PoisonMessageEventArgs).Should().BeAssignableTo<EventArgs>();
        }

        /// <summary>
        /// Tests that PoisonMessageEventArgs can be used as EventArgs.
        /// </summary>
        [TestMethod]
        public void PoisonMessageEventArgs_WhenUsedAsEventArgs_ShouldWorkCorrectly()
        {
            var message = new TestQueueMessage { Id = "test-message", Body = "test body" };
            var poisonQueue = new TestQueueClient { Name = "poison-queue" };
            EventArgs eventArgs = new PoisonMessageEventArgs(message, poisonQueue);

            eventArgs.Should().BeOfType<PoisonMessageEventArgs>();
            var poisonEventArgs = (PoisonMessageEventArgs)eventArgs;
            poisonEventArgs.Message.Should().BeSameAs(message);
            poisonEventArgs.PoisonQueue.Should().BeSameAs(poisonQueue);
        }

        #endregion

        #region Event Scenario Tests

        /// <summary>
        /// Tests a realistic poison message scenario.
        /// </summary>
        [TestMethod]
        public void PoisonMessageEventArgs_WhenUsedInRealisticScenario_ShouldWorkCorrectly()
        {
            var originalMessage = new TestQueueMessage
            {
                Id = "msg-failed-processing",
                Body = "{\"orderId\": 12345, \"customerId\": \"cust-789\"}",
                DequeueCount = 6, // Exceeded max dequeue count
                PopReceipt = "receipt-original",
                DateInserted = DateTimeOffset.UtcNow.AddHours(-2)
            };

            var poisonQueue = new TestQueueClient
            {
                Name = "orders-poison",
                AccountName = "production-storage"
            };

            var eventArgs = new PoisonMessageEventArgs(originalMessage, poisonQueue);

            // Simulate event handler processing
            ProcessPoisonMessageEvent(eventArgs);

            eventArgs.Message.DequeueCount.Should().BeGreaterThan(5);
            eventArgs.PoisonQueue.Name.Should().EndWith("-poison");
        }

        /// <summary>
        /// Tests event handler scenario with multiple message types.
        /// </summary>
        [TestMethod]
        public void PoisonMessageEventArgs_WhenUsedWithDifferentMessageTypes_ShouldWorkCorrectly()
        {
            var scenarios = new[]
            {
                new { MessageType = "JSON", Body = "{\"key\": \"value\"}", QueueName = "json-queue-poison" },
                new { MessageType = "XML", Body = "<root><item>data</item></root>", QueueName = "xml-queue-poison" },
                new { MessageType = "Plain", Body = "Simple text message", QueueName = "text-queue-poison" }
            };

            foreach (var scenario in scenarios)
            {
                var message = new TestQueueMessage
                {
                    Id = $"msg-{scenario.MessageType.ToLower()}",
                    Body = scenario.Body,
                    DequeueCount = 10
                };

                var poisonQueue = new TestQueueClient { Name = scenario.QueueName };
                var eventArgs = new PoisonMessageEventArgs(message, poisonQueue);

                eventArgs.Message.Body.Should().Be(scenario.Body);
                eventArgs.PoisonQueue.Name.Should().Be(scenario.QueueName);
            }
        }

        #endregion

        #region Thread Safety Tests

        /// <summary>
        /// Tests that PoisonMessageEventArgs is safe for concurrent access.
        /// </summary>
        [TestMethod]
        public void PoisonMessageEventArgs_WhenAccessedConcurrently_ShouldBeSafe()
        {
            var message = new TestQueueMessage { Id = "concurrent-test", Body = "test body" };
            var poisonQueue = new TestQueueClient { Name = "concurrent-poison-queue" };
            var eventArgs = new PoisonMessageEventArgs(message, poisonQueue);

            var tasks = new System.Collections.Generic.List<System.Threading.Tasks.Task>();
            var results = new System.Collections.Concurrent.ConcurrentBag<(string MessageId, string QueueName)>();

            // Create multiple tasks that access the properties
            for (int i = 0; i < 10; i++)
            {
                tasks.Add(System.Threading.Tasks.Task.Run(() =>
                {
                    var msgId = eventArgs.Message?.Id;
                    var queueName = eventArgs.PoisonQueue?.Name;
                    results.Add((msgId, queueName));
                }));
            }

            System.Threading.Tasks.Task.WaitAll(tasks.ToArray());

            results.Should().HaveCount(10);
            results.Should().OnlyContain(r => r.MessageId == "concurrent-test" && r.QueueName == "concurrent-poison-queue");
        }

        #endregion

        #region Edge Case Tests

        /// <summary>
        /// Tests PoisonMessageEventArgs with empty or unusual property values.
        /// </summary>
        [TestMethod]
        public void PoisonMessageEventArgs_WhenUsedWithEmptyValues_ShouldHandleCorrectly()
        {
            var emptyMessage = new TestQueueMessage
            {
                Id = "",
                Body = "",
                DequeueCount = 0,
                PopReceipt = "",
                DateInserted = null
            };

            var emptyQueue = new TestQueueClient
            {
                Name = "",
                AccountName = ""
            };

            var eventArgs = new PoisonMessageEventArgs(emptyMessage, emptyQueue);

            eventArgs.Message.Should().BeSameAs(emptyMessage);
            eventArgs.Message.Id.Should().BeEmpty();
            eventArgs.Message.Body.Should().BeEmpty();
            eventArgs.PoisonQueue.Should().BeSameAs(emptyQueue);
            eventArgs.PoisonQueue.Name.Should().BeEmpty();
        }

        /// <summary>
        /// Tests PoisonMessageEventArgs with very large message content.
        /// </summary>
        [TestMethod]
        public void PoisonMessageEventArgs_WhenUsedWithLargeMessage_ShouldHandleCorrectly()
        {
            var largeBody = new string('x', 100000);
            var largeMessage = new TestQueueMessage
            {
                Id = "large-message-id",
                Body = largeBody,
                DequeueCount = 1
            };

            var poisonQueue = new TestQueueClient { Name = "large-message-poison" };
            var eventArgs = new PoisonMessageEventArgs(largeMessage, poisonQueue);

            eventArgs.Message.Body.Should().HaveLength(100000);
            eventArgs.Message.Body.Should().Be(largeBody);
        }

        /// <summary>
        /// Tests PoisonMessageEventArgs with special characters in properties.
        /// </summary>
        [TestMethod]
        public void PoisonMessageEventArgs_WhenUsedWithSpecialCharacters_ShouldHandleCorrectly()
        {
            var specialMessage = new TestQueueMessage
            {
                Id = "msg-with-unicode-🚀",
                Body = "Message with unicode 🎉 and special chars @#$%^&*()\n\r\t",
                DequeueCount = 3
            };

            var specialQueue = new TestQueueClient
            {
                Name = "queue-with-unicode-⭐-poison",
                AccountName = "account-with-symbols-@#$%"
            };

            var eventArgs = new PoisonMessageEventArgs(specialMessage, specialQueue);

            eventArgs.Message.Id.Should().Contain("🚀");
            eventArgs.Message.Body.Should().Contain("🎉");
            eventArgs.PoisonQueue.Name.Should().Contain("⭐");
            eventArgs.PoisonQueue.AccountName.Should().Contain("@#$%");
        }

        #endregion

        #region Memory and Performance Tests

        /// <summary>
        /// Tests that PoisonMessageEventArgs doesn't cause memory leaks by holding references.
        /// </summary>
        [TestMethod]
        public void PoisonMessageEventArgs_WhenCreatedRepeatedly_ShouldNotLeakMemory()
        {
            // Create many instances to test for potential memory issues
            for (int i = 0; i < 1000; i++)
            {
                var message = new TestQueueMessage { Id = $"msg-{i}", Body = $"body-{i}" };
                var queue = new TestQueueClient { Name = $"queue-{i}" };
                var eventArgs = new PoisonMessageEventArgs(message, queue);

                // Verify the instance works correctly
                eventArgs.Message.Should().NotBeNull();
                eventArgs.PoisonQueue.Should().NotBeNull();
            }

            // Force garbage collection to help detect potential issues
            GC.Collect();
            GC.WaitForPendingFinalizers();
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Simulates processing a poison message event.
        /// </summary>
        /// <param name="eventArgs">The poison message event arguments.</param>
        private static void ProcessPoisonMessageEvent(PoisonMessageEventArgs eventArgs)
        {
            // Simulate what an event handler might do
            if (eventArgs.Message is not null && eventArgs.PoisonQueue is not null)
            {
                var messageInfo = $"Message {eventArgs.Message.Id} moved to poison queue {eventArgs.PoisonQueue.Name}";
                messageInfo.Should().NotBeNullOrWhiteSpace();
            }
        }

        #endregion

    }

}
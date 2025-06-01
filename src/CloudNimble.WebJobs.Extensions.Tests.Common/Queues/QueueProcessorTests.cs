// Copyright (c) CloudNimble, Inc. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using CloudNimble.WebJobs.Extensions.Common.Queues;
using CloudNimble.WebJobs.Extensions.Tests.Common.Models;
using FluentAssertions;
using Microsoft.Azure.WebJobs.Host.Executors;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace CloudNimble.WebJobs.Extensions.Tests.Common.Queues
{

    /// <summary>
    /// Tests for the <see cref="QueueProcessor"/> class.
    /// </summary>
    [TestClass]
    [TestCategory("Unit")]
    public class QueueProcessorTests
    {

        #region Constructor Tests

        /// <summary>
        /// Tests that the constructor correctly initializes with valid parameters.
        /// </summary>
        [TestMethod]
        public void Constructor_WhenCalledWithValidParameters_ShouldInitializeCorrectly()
        {
            var options = CreateValidQueueProcessorOptions();
            var classifier = new TestQueueRequestExceptionClassifier();

            var processor = new QueueProcessor(options, classifier);

            processor.Should().NotBeNull();
            processor.QueuesOptions.Should().NotBeNull();
            processor.QueuesOptions.Should().BeSameAs(options.Options);
        }

        /// <summary>
        /// Tests that the constructor throws when options is null.
        /// </summary>
        [TestMethod]
        public void Constructor_WhenOptionsIsNull_ShouldThrowArgumentNullException()
        {
            var classifier = new TestQueueRequestExceptionClassifier();

            var action = () => new QueueProcessor(null, classifier);

            action.Should().Throw<ArgumentNullException>()
                .WithParameterName("queueProcessorOptions");
        }

        /// <summary>
        /// Tests that the constructor accepts null exception classifier.
        /// </summary>
        [TestMethod]
        public void Constructor_WhenExceptionClassifierIsNull_ShouldAcceptNull()
        {
            var options = CreateValidQueueProcessorOptions();

            var processor = new QueueProcessor(options, null);

            processor.Should().NotBeNull();
        }

        #endregion

        #region BeginProcessingMessageAsync Tests

        /// <summary>
        /// Tests that BeginProcessingMessageAsync returns true for valid messages.
        /// </summary>
        [TestMethod]
        public async Task BeginProcessingMessageAsync_WhenMessageIsValid_ShouldReturnTrue()
        {
            var options = CreateValidQueueProcessorOptions();
            var classifier = new TestQueueRequestExceptionClassifier();
            var processor = new QueueProcessor(options, classifier);
            var message = new TestQueueMessage { Id = "msg-1", DequeueCount = 1 };

            var result = await processor.BeginProcessingMessageAsync(message, CancellationToken.None);

            result.Should().BeTrue();
        }

        /// <summary>
        /// Tests that BeginProcessingMessageAsync returns false when dequeue count exceeds maximum.
        /// </summary>
        [TestMethod]
        public async Task BeginProcessingMessageAsync_WhenDequeueCountExceedsMax_ShouldReturnFalse()
        {
            var options = CreateQueueProcessorOptionsWithMaxDequeueCount(3);
            var classifier = new TestQueueRequestExceptionClassifier();
            var processor = new QueueProcessor(options, classifier);
            var message = new TestQueueMessage { Id = "msg-1", DequeueCount = 4 }; // Exceeds max of 3

            var result = await processor.BeginProcessingMessageAsync(message, CancellationToken.None);

            result.Should().BeFalse();
        }

        /// <summary>
        /// Tests that BeginProcessingMessageAsync handles poison messages correctly.
        /// </summary>
        [TestMethod]
        public async Task BeginProcessingMessageAsync_WhenMessageExceedsMaxDequeueCount_ShouldHandlePoisonMessage()
        {
            var poisonQueue = new TestQueueClient { Name = "test-poison" };
            var options = CreateQueueProcessorOptionsWithPoisonQueue(poisonQueue, 2);
            var classifier = new TestQueueRequestExceptionClassifier();
            var processor = new QueueProcessor(options, classifier);
            var message = new TestQueueMessage { Id = "poison-msg", DequeueCount = 3, Body = "poison content" };

            var result = await processor.BeginProcessingMessageAsync(message, CancellationToken.None);

            result.Should().BeFalse();
            poisonQueue.AddedMessages.Should().HaveCount(1);
            poisonQueue.AddedMessages[0].Should().Be("poison content");
        }

        #endregion

        #region CompleteProcessingMessageAsync Tests

        /// <summary>
        /// Tests that CompleteProcessingMessageAsync deletes message on success.
        /// </summary>
        [TestMethod]
        public async Task CompleteProcessingMessageAsync_WhenResultSucceeded_ShouldDeleteMessage()
        {
            var queue = new TestQueueClient { Name = "test-queue" };
            var options = CreateQueueProcessorOptionsWithQueue(queue);
            var classifier = new TestQueueRequestExceptionClassifier();
            var processor = new QueueProcessor(options, classifier);
            var message = new TestQueueMessage { Id = "msg-1", PopReceipt = "receipt-1" };
            var successResult = new FunctionResult(true);

            await processor.CompleteProcessingMessageAsync(message, successResult, CancellationToken.None);

            queue.DeletedMessages.Should().HaveCount(1);
            queue.DeletedMessages[0].Should().Be(("msg-1", "receipt-1"));
        }

        /// <summary>
        /// Tests that CompleteProcessingMessageAsync handles poison messages on failure.
        /// </summary>
        [TestMethod]
        public async Task CompleteProcessingMessageAsync_WhenResultFailedAndExceedsMaxDequeue_ShouldMoveToPoisonQueue()
        {
            var mainQueue = new TestQueueClient { Name = "main-queue" };
            var poisonQueue = new TestQueueClient { Name = "poison-queue" };
            var options = CreateQueueProcessorOptionsWithQueues(mainQueue, poisonQueue, 2);
            var classifier = new TestQueueRequestExceptionClassifier();
            var processor = new QueueProcessor(options, classifier);
            var message = new TestQueueMessage { Id = "failed-msg", DequeueCount = 2, Body = "failed content" };
            var failedResult = new FunctionResult(false);

            await processor.CompleteProcessingMessageAsync(message, failedResult, CancellationToken.None);

            poisonQueue.AddedMessages.Should().HaveCount(1);
            poisonQueue.AddedMessages[0].Should().Be("failed content");
            mainQueue.DeletedMessages.Should().HaveCount(1);
        }

        /// <summary>
        /// Tests that CompleteProcessingMessageAsync releases message when below max dequeue count.
        /// </summary>
        [TestMethod]
        public async Task CompleteProcessingMessageAsync_WhenResultFailedBelowMaxDequeue_ShouldReleaseMessage()
        {
            var queue = new TestQueueClient { Name = "test-queue" };
            var poisonQueue = new TestQueueClient { Name = "poison-queue" };
            var options = CreateQueueProcessorOptionsWithQueues(queue, poisonQueue, 5);
            var classifier = new TestQueueRequestExceptionClassifier();
            var processor = new QueueProcessor(options, classifier);
            var message = new TestQueueMessage { Id = "retry-msg", DequeueCount = 2, PopReceipt = "receipt-1" };
            var failedResult = new FunctionResult(false);

            await processor.CompleteProcessingMessageAsync(message, failedResult, CancellationToken.None);

            queue.UpdatedMessages.Should().HaveCount(1);
            queue.UpdatedMessages[0].Id.Should().Be("retry-msg");
            poisonQueue.AddedMessages.Should().BeEmpty();
        }

        /// <summary>
        /// Tests that CompleteProcessingMessageAsync handles queues without poison queues.
        /// </summary>
        [TestMethod]
        public async Task CompleteProcessingMessageAsync_WhenNoPoisonQueueAndFailed_ShouldNotReleaseMessage()
        {
            var queue = new TestQueueClient { Name = "test-queue" };
            var options = CreateQueueProcessorOptionsWithQueue(queue); // No poison queue
            var classifier = new TestQueueRequestExceptionClassifier();
            var processor = new QueueProcessor(options, classifier);
            var message = new TestQueueMessage { Id = "failed-msg", DequeueCount = 10 };
            var failedResult = new FunctionResult(false);

            await processor.CompleteProcessingMessageAsync(message, failedResult, CancellationToken.None);

            // Should not release message to prevent infinite loop
            queue.UpdatedMessages.Should().BeEmpty();
            queue.DeletedMessages.Should().BeEmpty();
        }

        #endregion

        #region Exception Handling Tests

        /// <summary>
        /// Tests that ReleaseMessageAsync handles pop receipt mismatch gracefully.
        /// </summary>
        [TestMethod]
        public async Task CompleteProcessingMessageAsync_WhenPopReceiptMismatch_ShouldHandleGracefully()
        {
            var queue = new TestQueueClient 
            { 
                Name = "test-queue",
                ShouldThrowOnUpdate = true,
                ExceptionToThrow = new InvalidOperationException("Pop receipt mismatch")
            };
            var poisonQueue = new TestQueueClient { Name = "poison-queue" };
            var options = CreateQueueProcessorOptionsWithQueues(queue, poisonQueue, 5);
            var classifier = new TestQueueRequestExceptionClassifier { IsPopReceiptMismatchResult = true };
            var processor = new QueueProcessor(options, classifier);
            var message = new TestQueueMessage { Id = "msg-1", DequeueCount = 1 };
            var failedResult = new FunctionResult(false);

            // Should not throw exception
            await processor.CompleteProcessingMessageAsync(message, failedResult, CancellationToken.None);

            // Should not retry update since pop receipt mismatch means someone else took the message
            queue.UpdatedMessages.Should().BeEmpty();
        }

        /// <summary>
        /// Tests that DeleteMessageAsync handles pop receipt mismatch gracefully.
        /// </summary>
        [TestMethod]
        public async Task CompleteProcessingMessageAsync_WhenDeleteFailsWithPopReceiptMismatch_ShouldHandleGracefully()
        {
            var queue = new TestQueueClient 
            { 
                Name = "test-queue",
                ShouldThrowOnDelete = true,
                ExceptionToThrow = new InvalidOperationException("Pop receipt mismatch on delete")
            };
            var options = CreateQueueProcessorOptionsWithQueue(queue);
            var classifier = new TestQueueRequestExceptionClassifier { IsPopReceiptMismatchResult = true };
            var processor = new QueueProcessor(options, classifier);
            var message = new TestQueueMessage { Id = "msg-1", PopReceipt = "receipt-1" };
            var successResult = new FunctionResult(true);

            // Should not throw exception even though delete fails
            await processor.CompleteProcessingMessageAsync(message, successResult, CancellationToken.None);
        }

        /// <summary>
        /// Tests that unclassified exceptions are rethrown during message release.
        /// </summary>
        [TestMethod]
        public async Task CompleteProcessingMessageAsync_WhenUnclassifiedExceptionOnRelease_ShouldRethrow()
        {
            var queue = new TestQueueClient 
            { 
                Name = "test-queue",
                ShouldThrowOnUpdate = true,
                ExceptionToThrow = new InvalidOperationException("Unknown error")
            };
            var poisonQueue = new TestQueueClient { Name = "poison-queue" };
            var options = CreateQueueProcessorOptionsWithQueues(queue, poisonQueue, 5);
            var classifier = new TestQueueRequestExceptionClassifier(); // All return false
            var processor = new QueueProcessor(options, classifier);
            var message = new TestQueueMessage { Id = "msg-1", DequeueCount = 1 };
            var failedResult = new FunctionResult(false);

            var action = async () => await processor.CompleteProcessingMessageAsync(message, failedResult, CancellationToken.None);

            await action.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("Unknown error");
        }

        #endregion

        #region Event Tests

        /// <summary>
        /// Tests that MessageAddedToPoisonQueueAsync event is raised correctly.
        /// </summary>
        [TestMethod]
        public async Task HandlePoisonMessageAsync_WhenCalled_ShouldRaiseEvent()
        {
            var mainQueue = new TestQueueClient { Name = "main-queue" };
            var poisonQueue = new TestQueueClient { Name = "poison-queue" };
            var options = CreateQueueProcessorOptionsWithQueues(mainQueue, poisonQueue, 2);
            var classifier = new TestQueueRequestExceptionClassifier();
            var processor = new QueueProcessor(options, classifier);
            var message = new TestQueueMessage { Id = "poison-msg", Body = "poison content" };

            var eventFired = false;
            PoisonMessageEventArgs capturedEventArgs = null;

            processor.MessageAddedToPoisonQueueAsync += (proc, args) =>
            {
                eventFired = true;
                capturedEventArgs = args;
                return Task.CompletedTask;
            };

            await processor.HandlePoisonMessageAsync(message, CancellationToken.None);

            eventFired.Should().BeTrue();
            capturedEventArgs.Should().NotBeNull();
            capturedEventArgs.Message.Should().BeSameAs(message);
            capturedEventArgs.PoisonQueue.Should().BeSameAs(poisonQueue);
        }

        /// <summary>
        /// Tests that poison message handling works without event handlers.
        /// </summary>
        [TestMethod]
        public async Task HandlePoisonMessageAsync_WhenNoEventHandlers_ShouldCompleteSuccessfully()
        {
            var mainQueue = new TestQueueClient { Name = "main-queue" };
            var poisonQueue = new TestQueueClient { Name = "poison-queue" };
            var options = CreateQueueProcessorOptionsWithQueues(mainQueue, poisonQueue, 2);
            var classifier = new TestQueueRequestExceptionClassifier();
            var processor = new QueueProcessor(options, classifier);
            var message = new TestQueueMessage { Id = "poison-msg", Body = "poison content" };

            // Should not throw even without event handlers
            await processor.HandlePoisonMessageAsync(message, CancellationToken.None);

            poisonQueue.AddedMessages.Should().HaveCount(1);
            mainQueue.DeletedMessages.Should().HaveCount(1);
        }

        /// <summary>
        /// Tests that multiple event handlers can be attached.
        /// </summary>
        [TestMethod]
        public async Task HandlePoisonMessageAsync_WhenMultipleEventHandlers_ShouldCallAll()
        {
            var mainQueue = new TestQueueClient { Name = "main-queue" };
            var poisonQueue = new TestQueueClient { Name = "poison-queue" };
            var options = CreateQueueProcessorOptionsWithQueues(mainQueue, poisonQueue, 2);
            var classifier = new TestQueueRequestExceptionClassifier();
            var processor = new QueueProcessor(options, classifier);
            var message = new TestQueueMessage { Id = "poison-msg", Body = "poison content" };

            var handler1Called = false;
            var handler2Called = false;

            processor.MessageAddedToPoisonQueueAsync += (proc, args) =>
            {
                handler1Called = true;
                return Task.CompletedTask;
            };

            processor.MessageAddedToPoisonQueueAsync += (proc, args) =>
            {
                handler2Called = true;
                return Task.CompletedTask;
            };

            await processor.HandlePoisonMessageAsync(message, CancellationToken.None);

            handler1Called.Should().BeTrue();
            handler2Called.Should().BeTrue();
        }

        #endregion

        #region Integration Tests

        /// <summary>
        /// Tests a complete message processing flow from begin to complete.
        /// </summary>
        [TestMethod]
        public async Task QueueProcessor_WhenProcessingCompleteFlow_ShouldWorkCorrectly()
        {
            var mainQueue = new TestQueueClient { Name = "orders" };
            var poisonQueue = new TestQueueClient { Name = "orders-poison" };
            var options = CreateQueueProcessorOptionsWithQueues(mainQueue, poisonQueue, 3);
            var classifier = new TestQueueRequestExceptionClassifier();
            var processor = new QueueProcessor(options, classifier);
            var message = new TestQueueMessage 
            { 
                Id = "order-12345", 
                PopReceipt = "receipt-abc",
                Body = "{\"orderId\": 12345}",
                DequeueCount = 1
            };

            // Begin processing
            var beginResult = await processor.BeginProcessingMessageAsync(message, CancellationToken.None);
            beginResult.Should().BeTrue();

            // Complete processing successfully
            var successResult = new FunctionResult(true);
            await processor.CompleteProcessingMessageAsync(message, successResult, CancellationToken.None);

            // Message should be deleted from main queue
            mainQueue.DeletedMessages.Should().HaveCount(1);
            mainQueue.DeletedMessages[0].Should().Be(("order-12345", "receipt-abc"));
            poisonQueue.AddedMessages.Should().BeEmpty();
        }

        /// <summary>
        /// Tests poison message flow with event handling.
        /// </summary>
        [TestMethod]
        public async Task QueueProcessor_WhenProcessingPoisonMessageFlow_ShouldWorkCorrectly()
        {
            var mainQueue = new TestQueueClient { Name = "tasks" };
            var poisonQueue = new TestQueueClient { Name = "tasks-poison" };
            var options = CreateQueueProcessorOptionsWithQueues(mainQueue, poisonQueue, 2);
            var classifier = new TestQueueRequestExceptionClassifier();
            var processor = new QueueProcessor(options, classifier);
            var message = new TestQueueMessage 
            { 
                Id = "failed-task-999", 
                PopReceipt = "receipt-xyz",
                Body = "{\"taskId\": 999, \"data\": \"corrupted\"}",
                DequeueCount = 3 // Exceeds max of 2
            };

            var poisonEventFired = false;
            processor.MessageAddedToPoisonQueueAsync += (proc, args) =>
            {
                poisonEventFired = true;
                return Task.CompletedTask;
            };

            // Begin processing should return false and handle as poison
            var beginResult = await processor.BeginProcessingMessageAsync(message, CancellationToken.None);
            beginResult.Should().BeFalse();

            // Should have moved to poison queue and fired event
            poisonQueue.AddedMessages.Should().HaveCount(1);
            poisonQueue.AddedMessages[0].Should().Be("{\"taskId\": 999, \"data\": \"corrupted\"}");
            mainQueue.DeletedMessages.Should().HaveCount(1);
            poisonEventFired.Should().BeTrue();
        }

        #endregion

        #region Thread Safety Tests

        /// <summary>
        /// Tests that queue processor can handle concurrent message processing.
        /// </summary>
        [TestMethod]
        public async Task QueueProcessor_WhenProcessingConcurrentMessages_ShouldBeSafe()
        {
            var mainQueue = new TestQueueClient { Name = "concurrent-queue" };
            var options = CreateQueueProcessorOptionsWithQueue(mainQueue);
            var classifier = new TestQueueRequestExceptionClassifier();
            var processor = new QueueProcessor(options, classifier);

            var tasks = new List<Task>();

            // Process multiple messages concurrently
            for (int i = 0; i < 10; i++)
            {
                int messageId = i;
                tasks.Add(Task.Run(async () =>
                {
                    var message = new TestQueueMessage 
                    { 
                        Id = $"msg-{messageId}", 
                        PopReceipt = $"receipt-{messageId}",
                        DequeueCount = 1
                    };

                    var beginResult = await processor.BeginProcessingMessageAsync(message, CancellationToken.None);
                    if (beginResult)
                    {
                        var successResult = new FunctionResult(true);
                        await processor.CompleteProcessingMessageAsync(message, successResult, CancellationToken.None);
                    }
                }));
            }

            await Task.WhenAll(tasks);

            mainQueue.DeletedMessages.Should().HaveCount(10);
        }

        #endregion

        #region Edge Case Tests

        /// <summary>
        /// Tests queue processor behavior with null poison queue.
        /// </summary>
        [TestMethod]
        public async Task QueueProcessor_WhenNoPoisonQueue_ShouldHandlePoisonMessagesGracefully()
        {
            var mainQueue = new TestQueueClient { Name = "no-poison-queue" };
            var options = CreateQueueProcessorOptionsWithQueue(mainQueue); // No poison queue
            var classifier = new TestQueueRequestExceptionClassifier();
            var processor = new QueueProcessor(options, classifier);
            var message = new TestQueueMessage { Id = "poison-msg", DequeueCount = 100 }; // Way over limit

            // Should not throw and should not move to poison queue
            await processor.HandlePoisonMessageAsync(message, CancellationToken.None);

            mainQueue.AddedMessages.Should().BeEmpty();
            mainQueue.DeletedMessages.Should().BeEmpty();
        }

        /// <summary>
        /// Tests processor with edge case configurations.
        /// </summary>
        [TestMethod]
        public async Task QueueProcessor_WhenUsedWithEdgeCaseConfigurations_ShouldHandleCorrectly()
        {
            var edgeConfigs = new[]
            {
                new { MaxDequeueCount = 1, ExpectedDequeueCount = 2 }, // Immediately poison
                new { MaxDequeueCount = 1000, ExpectedDequeueCount = 1 }, // Very high tolerance
                new { MaxDequeueCount = 1, ExpectedDequeueCount = 1 }  // Exactly at limit
            };

            foreach (var config in edgeConfigs)
            {
                var queue = new TestQueueClient { Name = $"edge-queue-{config.MaxDequeueCount}" };
                var options = CreateQueueProcessorOptionsWithMaxDequeueCount(config.MaxDequeueCount);
                var classifier = new TestQueueRequestExceptionClassifier();
                var processor = new QueueProcessor(options, classifier);
                var message = new TestQueueMessage { Id = "edge-msg", DequeueCount = config.ExpectedDequeueCount };

                var result = await processor.BeginProcessingMessageAsync(message, CancellationToken.None);

                if (config.ExpectedDequeueCount > config.MaxDequeueCount)
                {
                    result.Should().BeFalse("Message should be treated as poison");
                }
                else
                {
                    result.Should().BeTrue("Message should be processed normally");
                }
            }
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Creates a valid QueueProcessorOptions instance for testing.
        /// </summary>
        private static QueueProcessorOptions CreateValidQueueProcessorOptions()
        {
            var queue = new TestQueueClient { Name = "test-queue", AccountName = "test-account" };
            var loggerFactory = new TestLoggerFactory();
            var options = new QueuesOptionsBase { MaxDequeueCount = 5 };

            return new QueueProcessorOptions(queue, loggerFactory, options);
        }

        /// <summary>
        /// Creates QueueProcessorOptions with specific max dequeue count.
        /// </summary>
        private static QueueProcessorOptions CreateQueueProcessorOptionsWithMaxDequeueCount(int maxDequeueCount)
        {
            var queue = new TestQueueClient { Name = "test-queue" };
            var loggerFactory = new TestLoggerFactory();
            var options = new QueuesOptionsBase { MaxDequeueCount = maxDequeueCount };

            return new QueueProcessorOptions(queue, loggerFactory, options);
        }

        /// <summary>
        /// Creates QueueProcessorOptions with specific queue.
        /// </summary>
        private static QueueProcessorOptions CreateQueueProcessorOptionsWithQueue(TestQueueClient queue)
        {
            var loggerFactory = new TestLoggerFactory();
            var options = new QueuesOptionsBase { MaxDequeueCount = 5 };

            return new QueueProcessorOptions(queue, loggerFactory, options);
        }

        /// <summary>
        /// Creates QueueProcessorOptions with poison queue.
        /// </summary>
        private static QueueProcessorOptions CreateQueueProcessorOptionsWithPoisonQueue(TestQueueClient poisonQueue, int maxDequeueCount)
        {
            var mainQueue = new TestQueueClient { Name = "main-queue" };
            var loggerFactory = new TestLoggerFactory();
            var options = new QueuesOptionsBase { MaxDequeueCount = maxDequeueCount };

            return new QueueProcessorOptions(mainQueue, loggerFactory, options, poisonQueue);
        }

        /// <summary>
        /// Creates QueueProcessorOptions with both main and poison queues.
        /// </summary>
        private static QueueProcessorOptions CreateQueueProcessorOptionsWithQueues(TestQueueClient mainQueue, TestQueueClient poisonQueue, int maxDequeueCount)
        {
            var loggerFactory = new TestLoggerFactory();
            var options = new QueuesOptionsBase { MaxDequeueCount = maxDequeueCount };

            return new QueueProcessorOptions(mainQueue, loggerFactory, options, poisonQueue);
        }

        #endregion

    }

}
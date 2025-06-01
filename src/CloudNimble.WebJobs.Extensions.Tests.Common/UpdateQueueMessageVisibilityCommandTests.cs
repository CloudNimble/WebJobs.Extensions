// Copyright (c) CloudNimble, Inc. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using CloudNimble.WebJobs.Extensions.Common;
using CloudNimble.WebJobs.Extensions.Common.Queues;
using CloudNimble.WebJobs.Extensions.Common.Timers;
using CloudNimble.WebJobs.Extensions.Tests.Common.Models;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace CloudNimble.WebJobs.Extensions.Tests.Common.Commands
{

    /// <summary>
    /// Tests for the <see cref="UpdateQueueMessageVisibilityCommand"/> class.
    /// </summary>
    [TestClass]
    [TestCategory("Unit")]
    public class UpdateQueueMessageVisibilityCommandTests
    {

        #region Constructor Tests

        /// <summary>
        /// Tests that the constructor correctly initializes with valid parameters.
        /// </summary>
        [TestMethod]
        public void Constructor_WhenCalledWithValidParameters_ShouldInitializeCorrectly()
        {
            var queue = new TestQueueClient { Name = "test-queue" };
            var message = new TestQueueMessage { Id = "msg-1", PopReceipt = "receipt-1" };
            var visibilityTimeout = TimeSpan.FromMinutes(5);
            var classifier = new TestQueueRequestExceptionClassifier();
            var speedupStrategy = new TestDelayStrategy();
            Action<IQueueMessage, QueueMessageUpdateReceipt> onUpdateReceipt = (msg, receipt) => { };

            var command = new UpdateQueueMessageVisibilityCommand(
                queue, message, visibilityTimeout, classifier, speedupStrategy, onUpdateReceipt);

            command.Should().NotBeNull();
        }

        /// <summary>
        /// Tests that the constructor throws when queue is null.
        /// </summary>
        [TestMethod]
        public void Constructor_WhenQueueIsNull_ShouldThrowArgumentNullException()
        {
            var message = new TestQueueMessage { Id = "msg-1", PopReceipt = "receipt-1" };
            var visibilityTimeout = TimeSpan.FromMinutes(5);
            var classifier = new TestQueueRequestExceptionClassifier();
            var speedupStrategy = new TestDelayStrategy();

            var action = () => new UpdateQueueMessageVisibilityCommand(
                null, message, visibilityTimeout, classifier, speedupStrategy, null);

            action.Should().Throw<ArgumentNullException>()
                .WithParameterName("queue");
        }

        /// <summary>
        /// Tests that the constructor throws when message is null.
        /// </summary>
        [TestMethod]
        public void Constructor_WhenMessageIsNull_ShouldThrowArgumentNullException()
        {
            var queue = new TestQueueClient { Name = "test-queue" };
            var visibilityTimeout = TimeSpan.FromMinutes(5);
            var classifier = new TestQueueRequestExceptionClassifier();
            var speedupStrategy = new TestDelayStrategy();

            var action = () => new UpdateQueueMessageVisibilityCommand(
                queue, null, visibilityTimeout, classifier, speedupStrategy, null);

            action.Should().Throw<ArgumentNullException>()
                .WithParameterName("message");
        }

        /// <summary>
        /// Tests that the constructor throws when speedup strategy is null.
        /// </summary>
        [TestMethod]
        public void Constructor_WhenSpeedupStrategyIsNull_ShouldThrowArgumentNullException()
        {
            var queue = new TestQueueClient { Name = "test-queue" };
            var message = new TestQueueMessage { Id = "msg-1", PopReceipt = "receipt-1" };
            var visibilityTimeout = TimeSpan.FromMinutes(5);
            var classifier = new TestQueueRequestExceptionClassifier();

            var action = () => new UpdateQueueMessageVisibilityCommand(
                queue, message, visibilityTimeout, classifier, null, null);

            action.Should().Throw<ArgumentNullException>()
                .WithParameterName("speedupStrategy");
        }

        /// <summary>
        /// Tests that the constructor accepts null classifier and onUpdateReceipt.
        /// </summary>
        [TestMethod]
        public void Constructor_WhenOptionalParametersAreNull_ShouldAcceptNull()
        {
            var queue = new TestQueueClient { Name = "test-queue" };
            var message = new TestQueueMessage { Id = "msg-1", PopReceipt = "receipt-1" };
            var visibilityTimeout = TimeSpan.FromMinutes(5);
            var speedupStrategy = new TestDelayStrategy();

            var command = new UpdateQueueMessageVisibilityCommand(
                queue, message, visibilityTimeout, null, speedupStrategy, null);

            command.Should().NotBeNull();
        }

        #endregion

        #region ExecuteAsync Success Tests

        /// <summary>
        /// Tests that ExecuteAsync succeeds when queue update succeeds.
        /// </summary>
        [TestMethod]
        public async Task ExecuteAsync_WhenQueueUpdateSucceeds_ShouldReturnSuccessResult()
        {
            var queue = new TestQueueClient { Name = "test-queue" };
            var message = new TestQueueMessage { Id = "msg-1", PopReceipt = "receipt-1" };
            var visibilityTimeout = TimeSpan.FromMinutes(5);
            var classifier = new TestQueueRequestExceptionClassifier();
            var speedupStrategy = new TestDelayStrategy { NextDelay = TimeSpan.FromSeconds(30) };
            var updateReceiptCalled = false;
            var capturedMessage = (IQueueMessage)null;
            var capturedReceipt = (QueueMessageUpdateReceipt)null;

            Action<IQueueMessage, QueueMessageUpdateReceipt> onUpdateReceipt = (msg, receipt) =>
            {
                updateReceiptCalled = true;
                capturedMessage = msg;
                capturedReceipt = receipt;
            };

            var command = new UpdateQueueMessageVisibilityCommand(
                queue, message, visibilityTimeout, classifier, speedupStrategy, onUpdateReceipt);

            var result = await command.ExecuteAsync(CancellationToken.None);

            result.Should().NotBeNull();
            result.Wait.Should().NotBeNull();
            updateReceiptCalled.Should().BeTrue();
            capturedMessage.Should().BeSameAs(message);
            capturedReceipt.Should().NotBeNull();
            speedupStrategy.LastExecutionSucceeded.Should().BeTrue();
        }

        /// <summary>
        /// Tests that ExecuteAsync works without onUpdateReceipt callback.
        /// </summary>
        [TestMethod]
        public async Task ExecuteAsync_WhenOnUpdateReceiptIsNull_ShouldSucceedWithoutCallback()
        {
            var queue = new TestQueueClient { Name = "test-queue" };
            var message = new TestQueueMessage { Id = "msg-1", PopReceipt = "receipt-1" };
            var visibilityTimeout = TimeSpan.FromMinutes(5);
            var classifier = new TestQueueRequestExceptionClassifier();
            var speedupStrategy = new TestDelayStrategy { NextDelay = TimeSpan.FromSeconds(30) };

            var command = new UpdateQueueMessageVisibilityCommand(
                queue, message, visibilityTimeout, classifier, speedupStrategy, null);

            var result = await command.ExecuteAsync(CancellationToken.None);

            result.Should().NotBeNull();
            result.Wait.Should().NotBeNull();
            speedupStrategy.LastExecutionSucceeded.Should().BeTrue();
        }

        /// <summary>
        /// Tests that ExecuteAsync uses the correct delay strategy for success.
        /// </summary>
        [TestMethod]
        public async Task ExecuteAsync_WhenSuccessful_ShouldUseNormalDelay()
        {
            var queue = new TestQueueClient { Name = "test-queue" };
            var message = new TestQueueMessage { Id = "msg-1", PopReceipt = "receipt-1" };
            var visibilityTimeout = TimeSpan.FromMinutes(5);
            var classifier = new TestQueueRequestExceptionClassifier();
            var speedupStrategy = new TestDelayStrategy { NextDelay = TimeSpan.FromMinutes(2) };

            var command = new UpdateQueueMessageVisibilityCommand(
                queue, message, visibilityTimeout, classifier, speedupStrategy, null);

            var result = await command.ExecuteAsync(CancellationToken.None);

            speedupStrategy.LastExecutionSucceeded.Should().BeTrue();
            speedupStrategy.GetNextDelayCallCount.Should().Be(1);
        }

        #endregion

        #region ExecuteAsync Exception Handling Tests

        /// <summary>
        /// Tests that ExecuteAsync handles server-side exceptions correctly.
        /// </summary>
        [TestMethod]
        public async Task ExecuteAsync_WhenServerSideExceptionOccurs_ShouldUseSpeedupDelay()
        {
            var queue = new TestQueueClient 
            { 
                Name = "test-queue",
                ShouldThrowOnUpdate = true,
                ExceptionToThrow = new InvalidOperationException("Server error")
            };
            var message = new TestQueueMessage { Id = "msg-1", PopReceipt = "receipt-1" };
            var visibilityTimeout = TimeSpan.FromMinutes(5);
            var classifier = new TestQueueRequestExceptionClassifier { IsServerSideExceptionResult = true };
            var speedupStrategy = new TestDelayStrategy { NextDelay = TimeSpan.FromSeconds(10) };

            var command = new UpdateQueueMessageVisibilityCommand(
                queue, message, visibilityTimeout, classifier, speedupStrategy, null);

            var result = await command.ExecuteAsync(CancellationToken.None);

            result.Should().NotBeNull();
            speedupStrategy.LastExecutionSucceeded.Should().BeFalse();
            speedupStrategy.GetNextDelayCallCount.Should().Be(1);
        }

        /// <summary>
        /// Tests that ExecuteAsync handles pop receipt mismatch correctly.
        /// </summary>
        [TestMethod]
        public async Task ExecuteAsync_WhenPopReceiptMismatchOccurs_ShouldReturnInfiniteDelay()
        {
            var queue = new TestQueueClient 
            { 
                Name = "test-queue",
                ShouldThrowOnUpdate = true,
                ExceptionToThrow = new InvalidOperationException("Pop receipt mismatch")
            };
            var message = new TestQueueMessage { Id = "msg-1", PopReceipt = "receipt-1" };
            var visibilityTimeout = TimeSpan.FromMinutes(5);
            var classifier = new TestQueueRequestExceptionClassifier { IsPopReceiptMismatchResult = true };
            var speedupStrategy = new TestDelayStrategy();

            var command = new UpdateQueueMessageVisibilityCommand(
                queue, message, visibilityTimeout, classifier, speedupStrategy, null);

            var result = await command.ExecuteAsync(CancellationToken.None);

            result.Should().NotBeNull();
            result.Wait.Should().NotBeNull();
            
            // Should use infinite delay for pop receipt mismatch
            var delayTask = result.Wait;
            delayTask.Status.Should().Be(TaskStatus.WaitingForActivation);
        }

        /// <summary>
        /// Tests that ExecuteAsync handles not found exceptions correctly.
        /// </summary>
        [TestMethod]
        public async Task ExecuteAsync_WhenNotFoundExceptionOccurs_ShouldReturnInfiniteDelay()
        {
            var queue = new TestQueueClient 
            { 
                Name = "test-queue",
                ShouldThrowOnUpdate = true,
                ExceptionToThrow = new InvalidOperationException("Message not found")
            };
            var message = new TestQueueMessage { Id = "msg-1", PopReceipt = "receipt-1" };
            var visibilityTimeout = TimeSpan.FromMinutes(5);
            var classifier = new TestQueueRequestExceptionClassifier { IsNotFoundExceptionResult = true };
            var speedupStrategy = new TestDelayStrategy();

            var command = new UpdateQueueMessageVisibilityCommand(
                queue, message, visibilityTimeout, classifier, speedupStrategy, null);

            var result = await command.ExecuteAsync(CancellationToken.None);

            result.Should().NotBeNull();
            result.Wait.Should().NotBeNull();
        }

        /// <summary>
        /// Tests that ExecuteAsync handles conflict exceptions correctly.
        /// </summary>
        [TestMethod]
        public async Task ExecuteAsync_WhenConflictExceptionOccurs_ShouldReturnInfiniteDelay()
        {
            var queue = new TestQueueClient 
            { 
                Name = "test-queue",
                ShouldThrowOnUpdate = true,
                ExceptionToThrow = new InvalidOperationException("Conflict")
            };
            var message = new TestQueueMessage { Id = "msg-1", PopReceipt = "receipt-1" };
            var visibilityTimeout = TimeSpan.FromMinutes(5);
            var classifier = new TestQueueRequestExceptionClassifier { IsConflictExceptionResult = true };
            var speedupStrategy = new TestDelayStrategy();

            var command = new UpdateQueueMessageVisibilityCommand(
                queue, message, visibilityTimeout, classifier, speedupStrategy, null);

            var result = await command.ExecuteAsync(CancellationToken.None);

            result.Should().NotBeNull();
            result.Wait.Should().NotBeNull();
        }

        /// <summary>
        /// Tests that ExecuteAsync rethrows unclassified exceptions.
        /// </summary>
        [TestMethod]
        public async Task ExecuteAsync_WhenUnclassifiedExceptionOccurs_ShouldRethrowException()
        {
            var expectedException = new InvalidOperationException("Unexpected error");
            var queue = new TestQueueClient 
            { 
                Name = "test-queue",
                ShouldThrowOnUpdate = true,
                ExceptionToThrow = expectedException
            };
            var message = new TestQueueMessage { Id = "msg-1", PopReceipt = "receipt-1" };
            var visibilityTimeout = TimeSpan.FromMinutes(5);
            var classifier = new TestQueueRequestExceptionClassifier(); // All methods return false
            var speedupStrategy = new TestDelayStrategy();

            var command = new UpdateQueueMessageVisibilityCommand(
                queue, message, visibilityTimeout, classifier, speedupStrategy, null);

            var action = async () => await command.ExecuteAsync(CancellationToken.None);

            await action.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("Unexpected error");
        }

        #endregion

        #region Cancellation Tests

        /// <summary>
        /// Tests that ExecuteAsync respects cancellation tokens.
        /// </summary>
        [TestMethod]
        public async Task ExecuteAsync_WhenCancellationRequested_ShouldThrowOperationCanceledException()
        {
            var queue = new TestQueueClient { Name = "test-queue" };
            var message = new TestQueueMessage { Id = "msg-1", PopReceipt = "receipt-1" };
            var visibilityTimeout = TimeSpan.FromMinutes(5);
            var classifier = new TestQueueRequestExceptionClassifier();
            var speedupStrategy = new TestDelayStrategy();

            var command = new UpdateQueueMessageVisibilityCommand(
                queue, message, visibilityTimeout, classifier, speedupStrategy, null);

            using var cts = new CancellationTokenSource();
            cts.Cancel();

            var action = async () => await command.ExecuteAsync(cts.Token);

            await action.Should().ThrowAsync<OperationCanceledException>();
        }

        #endregion

        #region Interface Implementation Tests

        /// <summary>
        /// Tests that UpdateQueueMessageVisibilityCommand implements ITaskSeriesCommand.
        /// </summary>
        [TestMethod]
        public void UpdateQueueMessageVisibilityCommand_WhenExamined_ShouldImplementITaskSeriesCommand()
        {
            typeof(UpdateQueueMessageVisibilityCommand).Should().BeAssignableTo<ITaskSeriesCommand>();
        }

        /// <summary>
        /// Tests that command can be used polymorphically.
        /// </summary>
        [TestMethod]
        public async Task UpdateQueueMessageVisibilityCommand_WhenUsedPolymorphically_ShouldWorkCorrectly()
        {
            var queue = new TestQueueClient { Name = "test-queue" };
            var message = new TestQueueMessage { Id = "msg-1", PopReceipt = "receipt-1" };
            var visibilityTimeout = TimeSpan.FromMinutes(5);
            var classifier = new TestQueueRequestExceptionClassifier();
            var speedupStrategy = new TestDelayStrategy();

            ITaskSeriesCommand command = new UpdateQueueMessageVisibilityCommand(
                queue, message, visibilityTimeout, classifier, speedupStrategy, null);

            var result = await command.ExecuteAsync(CancellationToken.None);

            result.Should().NotBeNull();
        }

        #endregion

        #region Integration Tests

        /// <summary>
        /// Tests the command in a realistic visibility timeout update scenario.
        /// </summary>
        [TestMethod]
        public async Task UpdateQueueMessageVisibilityCommand_WhenUsedInRealisticScenario_ShouldWorkCorrectly()
        {
            var queue = new TestQueueClient { Name = "long-running-tasks", AccountName = "production" };
            var message = new TestQueueMessage 
            { 
                Id = "task-12345", 
                PopReceipt = "receipt-abcdef",
                Body = "Long running task payload",
                DequeueCount = 1
            };
            var visibilityTimeout = TimeSpan.FromMinutes(10); // Extend visibility for long-running task
            var classifier = new TestQueueRequestExceptionClassifier();
            var speedupStrategy = new LinearSpeedupStrategy(TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(1));
            
            var receiptUpdates = new System.Collections.Generic.List<QueueMessageUpdateReceipt>();
            Action<IQueueMessage, QueueMessageUpdateReceipt> onUpdateReceipt = (msg, receipt) =>
            {
                receiptUpdates.Add(receipt);
            };

            var command = new UpdateQueueMessageVisibilityCommand(
                queue, message, visibilityTimeout, classifier, speedupStrategy, onUpdateReceipt);

            var result = await command.ExecuteAsync(CancellationToken.None);

            result.Should().NotBeNull();
            receiptUpdates.Should().HaveCount(1);
            receiptUpdates[0].Should().NotBeNull();
        }

        #endregion

        #region Thread Safety Tests

        /// <summary>
        /// Tests that multiple commands can execute concurrently.
        /// </summary>
        [TestMethod]
        public async Task UpdateQueueMessageVisibilityCommand_WhenExecutedConcurrently_ShouldBeSafe()
        {
            var tasks = new System.Collections.Generic.List<Task<TaskSeriesCommandResult>>();

            for (int i = 0; i < 10; i++)
            {
                var queue = new TestQueueClient { Name = $"queue-{i}" };
                var message = new TestQueueMessage { Id = $"msg-{i}", PopReceipt = $"receipt-{i}" };
                var visibilityTimeout = TimeSpan.FromMinutes(5);
                var classifier = new TestQueueRequestExceptionClassifier();
                var speedupStrategy = new TestDelayStrategy();

                var command = new UpdateQueueMessageVisibilityCommand(
                    queue, message, visibilityTimeout, classifier, speedupStrategy, null);

                tasks.Add(command.ExecuteAsync(CancellationToken.None));
            }

            var results = await Task.WhenAll(tasks);

            results.Should().HaveCount(10);
            results.Should().NotContainNulls();
        }

        #endregion

        #region Edge Case Tests

        /// <summary>
        /// Tests command with zero visibility timeout.
        /// </summary>
        [TestMethod]
        public async Task ExecuteAsync_WhenVisibilityTimeoutIsZero_ShouldWorkCorrectly()
        {
            var queue = new TestQueueClient { Name = "test-queue" };
            var message = new TestQueueMessage { Id = "msg-1", PopReceipt = "receipt-1" };
            var visibilityTimeout = TimeSpan.Zero;
            var classifier = new TestQueueRequestExceptionClassifier();
            var speedupStrategy = new TestDelayStrategy();

            var command = new UpdateQueueMessageVisibilityCommand(
                queue, message, visibilityTimeout, classifier, speedupStrategy, null);

            var result = await command.ExecuteAsync(CancellationToken.None);

            result.Should().NotBeNull();
        }

        /// <summary>
        /// Tests command with very large visibility timeout.
        /// </summary>
        [TestMethod]
        public async Task ExecuteAsync_WhenVisibilityTimeoutIsVeryLarge_ShouldWorkCorrectly()
        {
            var queue = new TestQueueClient { Name = "test-queue" };
            var message = new TestQueueMessage { Id = "msg-1", PopReceipt = "receipt-1" };
            var visibilityTimeout = TimeSpan.FromDays(7); // Maximum for some queue systems
            var classifier = new TestQueueRequestExceptionClassifier();
            var speedupStrategy = new TestDelayStrategy();

            var command = new UpdateQueueMessageVisibilityCommand(
                queue, message, visibilityTimeout, classifier, speedupStrategy, null);

            var result = await command.ExecuteAsync(CancellationToken.None);

            result.Should().NotBeNull();
        }

        #endregion

    }


}
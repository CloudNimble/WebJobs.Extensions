// Copyright (c) CloudNimble, Inc. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using CloudNimble.WebJobs.Extensions.Common.Timers;
using CloudNimble.WebJobs.Extensions.Tests.Common.Models;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;

namespace CloudNimble.WebJobs.Extensions.Tests.Common.Timers
{

    /// <summary>
    /// Unit tests for the <see cref="TaskSeriesTimer"/> class.
    /// </summary>
    [TestClass]
    public class TaskSeriesTimerTests
    {

        #region Constructor Tests

        [TestMethod]
        [TestCategory("Unit")]
        public void Constructor_WithValidParameters_ShouldInitializeCorrectly()
        {
            // Arrange
            var command = new TestTaskSeriesCommand();
            var exceptionHandler = new TestExceptionHandler();
            var initialWait = Task.CompletedTask;

            // Act
            using var timer = new TaskSeriesTimer(command, exceptionHandler, initialWait);

            // Assert
            timer.Should().NotBeNull();
        }

        [TestMethod]
        [TestCategory("Unit")]
        public void Constructor_WithNullCommand_ShouldThrowArgumentNullException()
        {
            // Arrange
            ITaskSeriesCommand command = null;
            var exceptionHandler = new TestExceptionHandler();
            var initialWait = Task.CompletedTask;

            // Act
            var act = () => new TaskSeriesTimer(command, exceptionHandler, initialWait);

            // Assert
            act.Should().Throw<ArgumentNullException>()
                .WithParameterName("command");
        }

        [TestMethod]
        [TestCategory("Unit")]
        public void Constructor_WithNullExceptionHandler_ShouldThrowArgumentNullException()
        {
            // Arrange
            var command = new TestTaskSeriesCommand();
            TestExceptionHandler exceptionHandler = null;
            var initialWait = Task.CompletedTask;

            // Act
            var act = () => new TaskSeriesTimer(command, exceptionHandler, initialWait);

            // Assert
            act.Should().Throw<ArgumentNullException>()
                .WithParameterName("exceptionHandler");
        }

        [TestMethod]
        [TestCategory("Unit")]
        public void Constructor_WithNullInitialWait_ShouldThrowArgumentNullException()
        {
            // Arrange
            var command = new TestTaskSeriesCommand();
            var exceptionHandler = new TestExceptionHandler();
            Task initialWait = null;

            // Act
            var act = () => new TaskSeriesTimer(command, exceptionHandler, initialWait);

            // Assert
            act.Should().Throw<ArgumentNullException>()
                .WithParameterName("initialWait");
        }

        #endregion

        #region Start Tests

        [TestMethod]
        [TestCategory("Unit")]
        public void Start_WhenNotStarted_ShouldStartSuccessfully()
        {
            // Arrange
            var command = new TestTaskSeriesCommand();
            var exceptionHandler = new TestExceptionHandler();
            var initialWait = Task.CompletedTask;
            using var timer = new TaskSeriesTimer(command, exceptionHandler, initialWait);

            // Act
            timer.Start();

            // Assert - no exception thrown
        }

        [TestMethod]
        [TestCategory("Unit")]
        public void Start_WhenAlreadyStarted_ShouldThrowInvalidOperationException()
        {
            // Arrange
            var command = new TestTaskSeriesCommand();
            var exceptionHandler = new TestExceptionHandler();
            var initialWait = Task.CompletedTask;
            using var timer = new TaskSeriesTimer(command, exceptionHandler, initialWait);
            timer.Start();

            // Act
            var act = () => timer.Start();

            // Assert
            act.Should().Throw<InvalidOperationException>()
                .WithMessage("*already been started*");
        }

        [TestMethod]
        [TestCategory("Unit")]
        public void Start_WhenDisposed_ShouldThrowObjectDisposedException()
        {
            // Arrange
            var command = new TestTaskSeriesCommand();
            var exceptionHandler = new TestExceptionHandler();
            var initialWait = Task.CompletedTask;
            var timer = new TaskSeriesTimer(command, exceptionHandler, initialWait);
            timer.Dispose();

            // Act
            var act = () => timer.Start();

            // Assert
            act.Should().Throw<ObjectDisposedException>();
        }

        #endregion

        #region StopAsync Tests

        [TestMethod]
        [TestCategory("Unit")]
        public async Task StopAsync_WhenStarted_ShouldStopSuccessfully()
        {
            // Arrange
            var command = new TestTaskSeriesCommand();
            var exceptionHandler = new TestExceptionHandler();
            var initialWait = Task.CompletedTask;
            using var timer = new TaskSeriesTimer(command, exceptionHandler, initialWait);
            timer.Start();

            // Give the timer a moment to start executing
            await Task.Delay(100);

            // Act
            await timer.StopAsync(CancellationToken.None);

            // Assert - no exception thrown
        }

        [TestMethod]
        [TestCategory("Unit")]
        public async Task StopAsync_WhenNotStarted_ShouldThrowInvalidOperationException()
        {
            // Arrange
            var command = new TestTaskSeriesCommand();
            var exceptionHandler = new TestExceptionHandler();
            var initialWait = Task.CompletedTask;
            using var timer = new TaskSeriesTimer(command, exceptionHandler, initialWait);

            // Act
            var act = async () => await timer.StopAsync(CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("*not yet been started*");
        }

        [TestMethod]
        [TestCategory("Unit")]
        public async Task StopAsync_WhenAlreadyStopped_ShouldThrowInvalidOperationException()
        {
            // Arrange
            var command = new TestTaskSeriesCommand();
            var exceptionHandler = new TestExceptionHandler();
            var initialWait = Task.CompletedTask;
            using var timer = new TaskSeriesTimer(command, exceptionHandler, initialWait);
            timer.Start();
            await timer.StopAsync(CancellationToken.None);

            // Act
            var act = async () => await timer.StopAsync(CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("*already been stopped*");
        }

        [TestMethod]
        [TestCategory("Unit")]
        public async Task StopAsync_WhenDisposed_ShouldThrowObjectDisposedException()
        {
            // Arrange
            var command = new TestTaskSeriesCommand();
            var exceptionHandler = new TestExceptionHandler();
            var initialWait = Task.CompletedTask;
            var timer = new TaskSeriesTimer(command, exceptionHandler, initialWait);
            timer.Dispose();

            // Act
            var act = async () => await timer.StopAsync(CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<ObjectDisposedException>();
        }

        [TestMethod]
        [TestCategory("Unit")]
        public async Task StopAsync_WithCancelledToken_ShouldStopQuickly()
        {
            // Arrange
            var command = new TestTaskSeriesCommand
            {
                NextResult = new TaskSeriesCommandResult(Task.Delay(10000)) // Long delay
            };
            var exceptionHandler = new TestExceptionHandler();
            var initialWait = Task.CompletedTask;
            using var timer = new TaskSeriesTimer(command, exceptionHandler, initialWait);
            timer.Start();

            var cts = new CancellationTokenSource();
            cts.CancelAfter(100);

            // Act & Assert
            await timer.StopAsync(cts.Token); // Should complete quickly despite long delay
        }

        #endregion

        #region Cancel Tests

        [TestMethod]
        [TestCategory("Unit")]
        public void Cancel_WhenRunning_ShouldCancelExecution()
        {
            // Arrange
            var command = new TestTaskSeriesCommand();
            var exceptionHandler = new TestExceptionHandler();
            var initialWait = Task.CompletedTask;
            using var timer = new TaskSeriesTimer(command, exceptionHandler, initialWait);
            timer.Start();

            // Act
            timer.Cancel();

            // Assert - no exception thrown
        }

        [TestMethod]
        [TestCategory("Unit")]
        public void Cancel_WhenDisposed_ShouldThrowObjectDisposedException()
        {
            // Arrange
            var command = new TestTaskSeriesCommand();
            var exceptionHandler = new TestExceptionHandler();
            var initialWait = Task.CompletedTask;
            var timer = new TaskSeriesTimer(command, exceptionHandler, initialWait);
            timer.Dispose();

            // Act
            var act = () => timer.Cancel();

            // Assert
            act.Should().Throw<ObjectDisposedException>();
        }

        #endregion

        #region Execution Tests

        [TestMethod]
        [TestCategory("Unit")]
        public async Task Timer_ShouldExecuteCommandRepeatedly()
        {
            // Arrange
            var command = new TestTaskSeriesCommand
            {
                NextResult = new TaskSeriesCommandResult(Task.Delay(50))
            };
            var exceptionHandler = new TestExceptionHandler();
            var initialWait = Task.CompletedTask;
            using var timer = new TaskSeriesTimer(command, exceptionHandler, initialWait);

            // Act
            timer.Start();
            await Task.Delay(200); // Allow time for multiple executions
            await timer.StopAsync(CancellationToken.None);

            // Assert
            command.ExecuteCallCount.Should().BeGreaterThan(1);
        }

        [TestMethod]
        [TestCategory("Unit")]
        public async Task Timer_WithInitialWait_ShouldWaitBeforeFirstExecution()
        {
            // Arrange
            var initialWaitTime = 100;
            var command = new TestTaskSeriesCommand();
            var exceptionHandler = new TestExceptionHandler();
            var initialWait = Task.Delay(initialWaitTime);
            using var timer = new TaskSeriesTimer(command, exceptionHandler, initialWait);

            // Act
            timer.Start();
            await Task.Delay(50); // Less than initial wait
            var countAfterShortWait = command.ExecuteCallCount;
            await Task.Delay(100); // More than initial wait
            await timer.StopAsync(CancellationToken.None);

            // Assert
            countAfterShortWait.Should().Be(0);
            command.ExecuteCallCount.Should().BeGreaterThan(0);
        }

        [TestMethod]
        [TestCategory("Unit")]
        public async Task Timer_WhenCommandThrows_ShouldReportToExceptionHandler()
        {
            // Arrange
            var expectedException = new InvalidOperationException("Test exception");
            var command = new TestTaskSeriesCommand
            {
                ThrowOnExecute = true,
                ExceptionToThrow = expectedException
            };
            var exceptionHandler = new TestExceptionHandler();
            var initialWait = Task.CompletedTask;
            using var timer = new TaskSeriesTimer(command, exceptionHandler, initialWait);

            // Act
            timer.Start();
            await Task.Delay(100);
            await timer.StopAsync(CancellationToken.None);

            // Assert
            exceptionHandler.UnhandledExceptionCount.Should().Be(1);
            exceptionHandler.LastException.SourceException.Should().Be(expectedException);
        }

        [TestMethod]
        [TestCategory("Unit")]
        public async Task Timer_WhenCommandThrowsOperationCanceled_ShouldContinueRunning()
        {
            // Arrange
            var command = new TestTaskSeriesCommand
            {
                ThrowOnExecute = true,
                ExceptionToThrow = new OperationCanceledException(),
                NextResult = new TaskSeriesCommandResult(Task.Delay(50))
            };
            
            var exceptionHandler = new TestExceptionHandler();
            var initialWait = Task.CompletedTask;
            using var timer = new TaskSeriesTimer(command, exceptionHandler, initialWait);

            // Act
            timer.Start();
            await Task.Delay(200);
            
            // Stop throwing after a few iterations
            command.ThrowOnExecute = false;
            await Task.Delay(100);
            await timer.StopAsync(CancellationToken.None);

            // Assert
            command.ExecuteCallCount.Should().BeGreaterThan(1);
            exceptionHandler.UnhandledExceptionCount.Should().Be(0); // OperationCanceled is handled
        }

        #endregion

        #region Dispose Tests

        [TestMethod]
        [TestCategory("Unit")]
        public void Dispose_ShouldNotThrow()
        {
            // Arrange
            var command = new TestTaskSeriesCommand();
            var exceptionHandler = new TestExceptionHandler();
            var initialWait = Task.CompletedTask;
            var timer = new TaskSeriesTimer(command, exceptionHandler, initialWait);

            // Act
            var act = () => timer.Dispose();

            // Assert
            act.Should().NotThrow();
        }

        [TestMethod]
        [TestCategory("Unit")]
        public void Dispose_WhenCalledMultipleTimes_ShouldNotThrow()
        {
            // Arrange
            var command = new TestTaskSeriesCommand();
            var exceptionHandler = new TestExceptionHandler();
            var initialWait = Task.CompletedTask;
            var timer = new TaskSeriesTimer(command, exceptionHandler, initialWait);

            // Act
            var act = () =>
            {
                timer.Dispose();
                timer.Dispose();
                timer.Dispose();
            };

            // Assert
            act.Should().NotThrow();
        }

        [TestMethod]
        [TestCategory("Unit")]
        public async Task Dispose_WhenRunning_ShouldCancelExecution()
        {
            // Arrange
            var command = new TestTaskSeriesCommand
            {
                NextResult = new TaskSeriesCommandResult(Task.Delay(200)) // Longer delay between executions
            };
            var exceptionHandler = new TestExceptionHandler();
            var initialWait = Task.CompletedTask;
            var timer = new TaskSeriesTimer(command, exceptionHandler, initialWait);
            timer.Start();

            // Wait for first execution
            await Task.Delay(100);
            var countBeforeDispose = command.ExecuteCallCount;

            // Act
            timer.Dispose();
            await Task.Delay(300); // Wait longer than the delay period

            // Assert
            countBeforeDispose.Should().BeGreaterThan(0); // Should have executed at least once
            command.ExecuteCallCount.Should().Be(countBeforeDispose); // Should not continue executing after dispose
        }

        #endregion

    }

}
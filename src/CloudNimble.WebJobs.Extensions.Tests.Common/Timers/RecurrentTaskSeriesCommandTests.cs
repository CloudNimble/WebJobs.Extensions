// Copyright (c) CloudNimble, Inc. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using CloudNimble.WebJobs.Extensions.Common.Timers;
using CloudNimble.WebJobs.Extensions.Tests.Common.Commands;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace CloudNimble.WebJobs.Extensions.Tests.Common.Timers
{

    /// <summary>
    /// Unit tests for the <see cref="RecurrentTaskSeriesCommand"/> class.
    /// </summary>
    [TestClass]
    public class RecurrentTaskSeriesCommandTests
    {

        #region Constructor Tests

        [TestMethod]
        [TestCategory("Unit")]
        public void Constructor_WithValidParameters_ShouldInitializeCorrectly()
        {
            // Arrange
            var innerCommand = new TestRecurrentCommand();
            var delayStrategy = new TestDelayStrategy();

            // Act
            var command = new RecurrentTaskSeriesCommand(innerCommand, delayStrategy);

            // Assert
            command.Should().NotBeNull();
        }

        [TestMethod]
        [TestCategory("Unit")]
        public void Constructor_WithNullInnerCommand_ShouldNotThrow()
        {
            // Arrange
            IRecurrentCommand innerCommand = null;
            var delayStrategy = new TestDelayStrategy();

            // Act
            var act = () => new RecurrentTaskSeriesCommand(innerCommand, delayStrategy);

            // Assert
            act.Should().NotThrow(); // Constructor doesn't validate parameters
        }

        [TestMethod]
        [TestCategory("Unit")]
        public void Constructor_WithNullDelayStrategy_ShouldNotThrow()
        {
            // Arrange
            var innerCommand = new TestRecurrentCommand();
            IDelayStrategy delayStrategy = null;

            // Act
            var act = () => new RecurrentTaskSeriesCommand(innerCommand, delayStrategy);

            // Assert
            act.Should().NotThrow(); // Constructor doesn't validate parameters
        }

        #endregion

        #region ExecuteAsync Tests

        [TestMethod]
        [TestCategory("Unit")]
        public async Task ExecuteAsync_WhenCommandSucceeds_ShouldUseSuccessDelay()
        {
            // Arrange
            var innerCommand = new TestRecurrentCommand { ReturnValue = true };
            var delayStrategy = new TestDelayStrategy { NextDelay = TimeSpan.FromMilliseconds(100) };
            var command = new RecurrentTaskSeriesCommand(innerCommand, delayStrategy);

            // Act
            var result = await command.ExecuteAsync(CancellationToken.None);

            // Assert
            innerCommand.ExecuteCallCount.Should().Be(1);
            delayStrategy.GetNextDelayCallCount.Should().Be(1);
            delayStrategy.LastExecutionSucceeded.Should().BeTrue();
            result.Wait.Should().NotBeNull();
        }

        [TestMethod]
        [TestCategory("Unit")]
        public async Task ExecuteAsync_WhenCommandFails_ShouldUseFailureDelay()
        {
            // Arrange
            var innerCommand = new TestRecurrentCommand { ReturnValue = false };
            var delayStrategy = new TestDelayStrategy { NextDelay = TimeSpan.FromMilliseconds(200) };
            var command = new RecurrentTaskSeriesCommand(innerCommand, delayStrategy);

            // Act
            var result = await command.ExecuteAsync(CancellationToken.None);

            // Assert
            innerCommand.ExecuteCallCount.Should().Be(1);
            delayStrategy.GetNextDelayCallCount.Should().Be(1);
            delayStrategy.LastExecutionSucceeded.Should().BeFalse();
            result.Wait.Should().NotBeNull();
        }

        [TestMethod]
        [TestCategory("Unit")]
        public async Task ExecuteAsync_WithCancellationToken_ShouldPassTokenToInnerCommand()
        {
            // Arrange
            var cts = new CancellationTokenSource();
            var innerCommand = new TestRecurrentCommand();
            var delayStrategy = new TestDelayStrategy();
            var command = new RecurrentTaskSeriesCommand(innerCommand, delayStrategy);

            // Act
            await command.ExecuteAsync(cts.Token);

            // Assert
            innerCommand.ExecuteCallCount.Should().Be(1);
        }

        [TestMethod]
        [TestCategory("Unit")]
        public async Task ExecuteAsync_WhenCommandThrows_ShouldPropagateException()
        {
            // Arrange
            var expectedException = new InvalidOperationException("Test exception");
            var innerCommand = new TestRecurrentCommand
            {
                ThrowOnExecute = true,
                ExceptionToThrow = expectedException
            };
            var delayStrategy = new TestDelayStrategy();
            var command = new RecurrentTaskSeriesCommand(innerCommand, delayStrategy);

            // Act
            var act = async () => await command.ExecuteAsync(CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("Test exception");
            delayStrategy.GetNextDelayCallCount.Should().Be(0); // Should not call delay strategy if command throws
        }

        [TestMethod]
        [TestCategory("Unit")]
        public async Task ExecuteAsync_DelayTask_ShouldBeConfiguredWithCancellationToken()
        {
            // Arrange
            var innerCommand = new TestRecurrentCommand();
            var delayStrategy = new TestDelayStrategy { NextDelay = TimeSpan.FromSeconds(10) }; // Long delay
            var command = new RecurrentTaskSeriesCommand(innerCommand, delayStrategy);
            var cts = new CancellationTokenSource();

            // Act
            var result = await command.ExecuteAsync(cts.Token);
            cts.Cancel();

            // Assert
            var act = async () => await result.Wait;
            await act.Should().ThrowAsync<TaskCanceledException>();
        }

        [TestMethod]
        [TestCategory("Unit")]
        public async Task ExecuteAsync_WithZeroDelay_ShouldReturnCompletedTask()
        {
            // Arrange
            var innerCommand = new TestRecurrentCommand();
            var delayStrategy = new TestDelayStrategy { NextDelay = TimeSpan.Zero };
            var command = new RecurrentTaskSeriesCommand(innerCommand, delayStrategy);

            // Act
            var result = await command.ExecuteAsync(CancellationToken.None);

            // Assert
            result.Wait.Status.Should().Be(TaskStatus.RanToCompletion);
        }

        [TestMethod]
        [TestCategory("Unit")]
        public async Task ExecuteAsync_MultipleExecutions_ShouldUseDelayStrategyCorrectly()
        {
            // Arrange
            var innerCommand = new TestRecurrentCommand();
            var delayStrategy = new TestDelayStrategy();
            var command = new RecurrentTaskSeriesCommand(innerCommand, delayStrategy);

            // Act - Execute multiple times with different results
            innerCommand.ReturnValue = true;
            await command.ExecuteAsync(CancellationToken.None);
            
            innerCommand.ReturnValue = false;
            await command.ExecuteAsync(CancellationToken.None);
            
            innerCommand.ReturnValue = true;
            await command.ExecuteAsync(CancellationToken.None);

            // Assert
            innerCommand.ExecuteCallCount.Should().Be(3);
            delayStrategy.GetNextDelayCallCount.Should().Be(3);
        }

        #endregion

    }

}
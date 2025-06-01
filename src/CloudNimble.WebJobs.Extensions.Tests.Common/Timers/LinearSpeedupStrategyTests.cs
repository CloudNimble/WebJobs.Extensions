// Copyright (c) CloudNimble, Inc. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using CloudNimble.WebJobs.Extensions.Common.Timers;
using CloudNimble.WebJobs.Extensions.Tests.Common.Models;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace CloudNimble.WebJobs.Extensions.Tests.Common.Timers
{

    /// <summary>
    /// Unit tests for the <see cref="LinearSpeedupStrategy"/> class.
    /// </summary>
    [TestClass]
    public class LinearSpeedupStrategyTests
    {

        #region Constructor Tests

        [TestMethod]
        [TestCategory("Unit")]
        public void Constructor_WithValidIntervals_ShouldInitializeCorrectly()
        {
            // Arrange
            var normalInterval = TimeSpan.FromSeconds(10);
            var minimumInterval = TimeSpan.FromSeconds(1);

            // Act
            var strategy = new LinearSpeedupStrategy(normalInterval, minimumInterval);

            // Assert
            strategy.Should().NotBeNull();
            strategy.GetNextDelay(true).Should().Be(normalInterval);
        }

        [TestMethod]
        [TestCategory("Unit")]
        public void Constructor_WithCustomDivisor_ShouldInitializeCorrectly()
        {
            // Arrange
            var normalInterval = TimeSpan.FromSeconds(10);
            var minimumInterval = TimeSpan.FromSeconds(1);
            var divisor = 3;

            // Act
            var strategy = new LinearSpeedupStrategy(normalInterval, minimumInterval, divisor);

            // Assert
            strategy.Should().NotBeNull();
            strategy.GetNextDelay(true).Should().Be(normalInterval);
        }

        [TestMethod]
        [TestCategory("Unit")]
        public void Constructor_WithNegativeNormalInterval_ShouldThrowArgumentOutOfRangeException()
        {
            // Arrange
            var normalInterval = TimeSpan.FromSeconds(-1);
            var minimumInterval = TimeSpan.FromSeconds(1);

            // Act
            var act = () => new LinearSpeedupStrategy(normalInterval, minimumInterval);

            // Assert
            act.Should().Throw<ArgumentOutOfRangeException>()
                .WithParameterName("normalInterval")
                .WithMessage("*must not be negative*");
        }

        [TestMethod]
        [TestCategory("Unit")]
        public void Constructor_WithNegativeMinimumInterval_ShouldThrowArgumentOutOfRangeException()
        {
            // Arrange
            var normalInterval = TimeSpan.FromSeconds(10);
            var minimumInterval = TimeSpan.FromSeconds(-1);

            // Act
            var act = () => new LinearSpeedupStrategy(normalInterval, minimumInterval);

            // Assert
            act.Should().Throw<ArgumentOutOfRangeException>()
                .WithParameterName("minimumInterval")
                .WithMessage("*must not be negative*");
        }

        [TestMethod]
        [TestCategory("Unit")]
        public void Constructor_WithMinimumGreaterThanNormal_ShouldThrowArgumentException()
        {
            // Arrange
            var normalInterval = TimeSpan.FromSeconds(5);
            var minimumInterval = TimeSpan.FromSeconds(10);

            // Act
            var act = () => new LinearSpeedupStrategy(normalInterval, minimumInterval);

            // Assert
            act.Should().Throw<ArgumentException>()
                .WithParameterName("minimumInterval")
                .WithMessage("*must not be greater than*");
        }

        [TestMethod]
        [TestCategory("Unit")]
        public void Constructor_WithInvalidDivisor_ShouldThrowArgumentOutOfRangeException()
        {
            // Arrange
            var normalInterval = TimeSpan.FromSeconds(10);
            var minimumInterval = TimeSpan.FromSeconds(1);
            var divisor = 0;

            // Act
            var act = () => new LinearSpeedupStrategy(normalInterval, minimumInterval, divisor);

            // Assert
            act.Should().Throw<ArgumentOutOfRangeException>()
                .WithParameterName("failureSpeedupDivisor")
                .WithMessage("*must not be less than 1*");
        }

        #endregion

        #region GetNextDelay Tests

        [TestMethod]
        [TestCategory("Unit")]
        public void GetNextDelay_AfterSuccess_ShouldReturnNormalInterval()
        {
            // Arrange
            var normalInterval = TimeSpan.FromSeconds(10);
            var minimumInterval = TimeSpan.FromSeconds(1);
            var strategy = new LinearSpeedupStrategy(normalInterval, minimumInterval);
            
            // First simulate some failures to change the current interval
            strategy.GetNextDelay(false);
            strategy.GetNextDelay(false);

            // Act
            var delay = strategy.GetNextDelay(true);

            // Assert
            delay.Should().Be(normalInterval);
        }

        [TestMethod]
        [TestCategory("Unit")]
        public void GetNextDelay_AfterFailure_ShouldReduceInterval()
        {
            // Arrange
            var normalInterval = TimeSpan.FromSeconds(10);
            var minimumInterval = TimeSpan.FromSeconds(1);
            var strategy = new LinearSpeedupStrategy(normalInterval, minimumInterval);

            // Act
            var firstDelay = strategy.GetNextDelay(false);
            var secondDelay = strategy.GetNextDelay(false);

            // Assert
            firstDelay.Should().Be(TimeSpan.FromSeconds(5)); // 10 / 2
            secondDelay.Should().Be(TimeSpan.FromSeconds(2.5)); // 5 / 2
        }

        [TestMethod]
        [TestCategory("Unit")]
        public void GetNextDelay_WithCustomDivisor_ShouldUseCustomDivisor()
        {
            // Arrange
            var normalInterval = TimeSpan.FromSeconds(9);
            var minimumInterval = TimeSpan.FromSeconds(1);
            var divisor = 3;
            var strategy = new LinearSpeedupStrategy(normalInterval, minimumInterval, divisor);

            // Act
            var firstDelay = strategy.GetNextDelay(false);
            var secondDelay = strategy.GetNextDelay(false);

            // Assert
            firstDelay.Should().Be(TimeSpan.FromSeconds(3)); // 9 / 3
            secondDelay.Should().Be(TimeSpan.FromSeconds(1)); // 3 / 3
        }

        [TestMethod]
        [TestCategory("Unit")]
        public void GetNextDelay_ShouldNotGoBelowMinimumInterval()
        {
            // Arrange
            var normalInterval = TimeSpan.FromSeconds(10);
            var minimumInterval = TimeSpan.FromSeconds(3);
            var strategy = new LinearSpeedupStrategy(normalInterval, minimumInterval);

            // Act - Keep failing until we hit minimum
            strategy.GetNextDelay(false); // 5
            strategy.GetNextDelay(false); // 2.5 -> should be 3 (minimum)
            var thirdDelay = strategy.GetNextDelay(false); // Should still be 3

            // Assert
            thirdDelay.Should().Be(minimumInterval);
        }

        [TestMethod]
        [TestCategory("Unit")]
        public void GetNextDelay_WithAlternatingSuccessFailure_ShouldResetAndReduceCorrectly()
        {
            // Arrange
            var normalInterval = TimeSpan.FromSeconds(8);
            var minimumInterval = TimeSpan.FromSeconds(1);
            var strategy = new LinearSpeedupStrategy(normalInterval, minimumInterval);

            // Act & Assert
            strategy.GetNextDelay(false).Should().Be(TimeSpan.FromSeconds(4)); // First failure
            strategy.GetNextDelay(true).Should().Be(normalInterval); // Success resets
            strategy.GetNextDelay(false).Should().Be(TimeSpan.FromSeconds(4)); // Failure again
            strategy.GetNextDelay(false).Should().Be(TimeSpan.FromSeconds(2)); // Another failure
        }

        [TestMethod]
        [TestCategory("Unit")]
        public void GetNextDelay_WithZeroMinimumInterval_ShouldAllowZeroDelay()
        {
            // Arrange
            var normalInterval = TimeSpan.FromSeconds(4);
            var minimumInterval = TimeSpan.Zero;
            var strategy = new LinearSpeedupStrategy(normalInterval, minimumInterval);

            // Act - Keep failing until we hit zero
            strategy.GetNextDelay(false); // 2
            strategy.GetNextDelay(false); // 1
            strategy.GetNextDelay(false); // 0.5
            var finalDelay = strategy.GetNextDelay(false); // 0.25

            // Assert
            finalDelay.Should().BeGreaterThanOrEqualTo(minimumInterval);
            finalDelay.Should().Be(TimeSpan.FromSeconds(0.25));
        }

        #endregion

        #region CreateTimer Tests

        [TestMethod]
        [TestCategory("Unit")]
        public void CreateTimer_WithValidParameters_ShouldReturnTimer()
        {
            // Arrange
            var command = new TestRecurrentCommand();
            var normalInterval = TimeSpan.FromSeconds(10);
            var minimumInterval = TimeSpan.FromSeconds(1);
            var exceptionHandler = new TestExceptionHandler();

            // Act
            var timer = LinearSpeedupStrategy.CreateTimer(command, normalInterval, minimumInterval, exceptionHandler);

            // Assert
            timer.Should().NotBeNull();
            timer.Should().BeOfType<TaskSeriesTimer>();
        }

        #endregion

    }

}
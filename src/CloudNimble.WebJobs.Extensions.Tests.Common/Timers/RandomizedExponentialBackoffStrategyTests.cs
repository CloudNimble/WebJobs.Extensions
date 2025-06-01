// Copyright (c) CloudNimble, Inc. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using CloudNimble.WebJobs.Extensions.Common.Timers;
using FluentAssertions;
using FluentAssertions.Execution;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;

namespace CloudNimble.WebJobs.Extensions.Tests.Common.Timers
{

    /// <summary>
    /// Unit tests for the <see cref="RandomizedExponentialBackoffStrategy"/> class.
    /// </summary>
    [TestClass]
    public class RandomizedExponentialBackoffStrategyTests
    {

        #region Constructor Tests

        [TestMethod]
        [TestCategory("Unit")]
        public void Constructor_WithTwoParameters_ShouldInitializeCorrectly()
        {
            // Arrange
            var minimumInterval = TimeSpan.FromSeconds(1);
            var maximumInterval = TimeSpan.FromSeconds(60);

            // Act
            var strategy = new RandomizedExponentialBackoffStrategy(minimumInterval, maximumInterval);

            // Assert
            strategy.Should().NotBeNull();
            strategy.GetNextDelay(true).Should().Be(minimumInterval);
        }

        [TestMethod]
        [TestCategory("Unit")]
        public void Constructor_WithThreeParameters_ShouldInitializeCorrectly()
        {
            // Arrange
            var minimumInterval = TimeSpan.FromSeconds(1);
            var maximumInterval = TimeSpan.FromSeconds(60);
            var deltaBackoff = TimeSpan.FromSeconds(2);

            // Act
            var strategy = new RandomizedExponentialBackoffStrategy(minimumInterval, maximumInterval, deltaBackoff);

            // Assert
            strategy.Should().NotBeNull();
            strategy.GetNextDelay(true).Should().Be(minimumInterval);
        }

        [TestMethod]
        [TestCategory("Unit")]
        public void Constructor_WithNegativeMinimumInterval_ShouldThrowArgumentOutOfRangeException()
        {
            // Arrange
            var minimumInterval = TimeSpan.FromSeconds(-1);
            var maximumInterval = TimeSpan.FromSeconds(60);

            // Act
            var act = () => new RandomizedExponentialBackoffStrategy(minimumInterval, maximumInterval);

            // Assert
            act.Should().Throw<ArgumentOutOfRangeException>()
                .WithParameterName("minimumInterval")
                .WithMessage("*must not be negative*");
        }

        [TestMethod]
        [TestCategory("Unit")]
        public void Constructor_WithNegativeMaximumInterval_ShouldThrowArgumentOutOfRangeException()
        {
            // Arrange
            var minimumInterval = TimeSpan.FromSeconds(1);
            var maximumInterval = TimeSpan.FromSeconds(-60);

            // Act
            var act = () => new RandomizedExponentialBackoffStrategy(minimumInterval, maximumInterval);

            // Assert
            act.Should().Throw<ArgumentOutOfRangeException>()
                .WithParameterName("maximumInterval")
                .WithMessage("*must not be negative*");
        }

        [TestMethod]
        [TestCategory("Unit")]
        public void Constructor_WithMinimumGreaterThanMaximum_ShouldThrowArgumentException()
        {
            // Arrange
            var minimumInterval = TimeSpan.FromSeconds(60);
            var maximumInterval = TimeSpan.FromSeconds(1);

            // Act
            var act = () => new RandomizedExponentialBackoffStrategy(minimumInterval, maximumInterval);

            // Assert
            act.Should().Throw<ArgumentException>()
                .WithParameterName("minimumInterval")
                .WithMessage("*must not be greater than*");
        }

        #endregion

        #region GetNextDelay Tests

        [TestMethod]
        [TestCategory("Unit")]
        public void GetNextDelay_AfterSuccess_ShouldResetToMinimumInterval()
        {
            // Arrange
            var minimumInterval = TimeSpan.FromSeconds(1);
            var maximumInterval = TimeSpan.FromSeconds(60);
            var strategy = new RandomizedExponentialBackoffStrategy(minimumInterval, maximumInterval);
            
            // First simulate some failures to increase the interval
            strategy.GetNextDelay(false);
            strategy.GetNextDelay(false);
            strategy.GetNextDelay(false);

            // Act
            var delay = strategy.GetNextDelay(true);

            // Assert
            delay.Should().Be(minimumInterval);
        }

        [TestMethod]
        [TestCategory("Unit")]
        public void GetNextDelay_AfterFirstFailure_ShouldReturnMinimumInterval()
        {
            // Arrange
            var minimumInterval = TimeSpan.FromSeconds(1);
            var maximumInterval = TimeSpan.FromSeconds(60);
            var strategy = new RandomizedExponentialBackoffStrategy(minimumInterval, maximumInterval);

            // Act
            var delay = strategy.GetNextDelay(false);

            // Assert
            delay.Should().Be(minimumInterval);
        }

        [TestMethod]
        [TestCategory("Unit")]
        public void GetNextDelay_AfterMultipleFailures_ShouldIncreaseExponentially()
        {
            // Arrange
            var minimumInterval = TimeSpan.FromSeconds(1);
            var maximumInterval = TimeSpan.FromSeconds(60);
            var deltaBackoff = TimeSpan.FromSeconds(1);
            var strategy = new RandomizedExponentialBackoffStrategy(minimumInterval, maximumInterval, deltaBackoff);

            // Act
            var firstDelay = strategy.GetNextDelay(false); // Should be minimum
            var secondDelay = strategy.GetNextDelay(false); // Should increase
            var thirdDelay = strategy.GetNextDelay(false); // Should increase more

            // Assert
            using (new AssertionScope())
            {
                firstDelay.Should().Be(minimumInterval);
                
                // Second delay should be minimum + randomized increment based on 2^0 * deltaBackoff
                // With randomization factor of 0.2, it should be between 0.8 and 1.2 seconds
                var expectedSecondMin = minimumInterval + TimeSpan.FromMilliseconds(0.8 * 1000);
                var expectedSecondMax = minimumInterval + TimeSpan.FromMilliseconds(1.2 * 1000);
                secondDelay.Should().BeGreaterThanOrEqualTo(expectedSecondMin);
                secondDelay.Should().BeLessThanOrEqualTo(expectedSecondMax);
                
                // Third delay should increase exponentially
                thirdDelay.Should().BeGreaterThan(secondDelay);
            }
        }

        [TestMethod]
        [TestCategory("Unit")]
        public void GetNextDelay_ShouldNotExceedMaximumInterval()
        {
            // Arrange
            var minimumInterval = TimeSpan.FromSeconds(1);
            var maximumInterval = TimeSpan.FromSeconds(5);
            var deltaBackoff = TimeSpan.FromSeconds(2);
            var strategy = new RandomizedExponentialBackoffStrategy(minimumInterval, maximumInterval, deltaBackoff);

            // Act - Keep failing until we should hit maximum
            TimeSpan lastDelay = TimeSpan.Zero;
            for (int i = 0; i < 10; i++)
            {
                lastDelay = strategy.GetNextDelay(false);
            }

            // Assert
            lastDelay.Should().Be(maximumInterval);
        }

        [TestMethod]
        [TestCategory("Unit")]
        public void GetNextDelay_AtMaximumInterval_ShouldStayAtMaximum()
        {
            // Arrange
            var minimumInterval = TimeSpan.FromSeconds(1);
            var maximumInterval = TimeSpan.FromSeconds(5);
            var strategy = new RandomizedExponentialBackoffStrategy(minimumInterval, maximumInterval);

            // Act - Keep failing until we hit maximum
            TimeSpan delay;
            do
            {
                delay = strategy.GetNextDelay(false);
            } while (delay < maximumInterval);

            // Now we're at maximum, next failure should keep us there
            var nextDelay = strategy.GetNextDelay(false);

            // Assert
            nextDelay.Should().Be(maximumInterval);
        }

        [TestMethod]
        [TestCategory("Unit")]
        public void GetNextDelay_WithRandomization_ShouldProduceVariedResults()
        {
            // Arrange
            var minimumInterval = TimeSpan.FromSeconds(1);
            var maximumInterval = TimeSpan.FromSeconds(60);
            var deltaBackoff = TimeSpan.FromSeconds(1);
            
            // Create multiple strategies to test randomization
            var strategies = new RandomizedExponentialBackoffStrategy[5];
            for (int i = 0; i < strategies.Length; i++)
            {
                strategies[i] = new RandomizedExponentialBackoffStrategy(minimumInterval, maximumInterval, deltaBackoff);
                strategies[i].GetNextDelay(false); // First delay to initialize
            }

            // Act - Get second delay from each strategy
            var delays = new TimeSpan[strategies.Length];
            for (int i = 0; i < strategies.Length; i++)
            {
                delays[i] = strategies[i].GetNextDelay(false);
            }

            // Assert - At least some delays should be different due to randomization
            // Check that not all delays are exactly the same
            delays.Distinct().Count().Should().BeGreaterThan(1, "randomization should produce varied results");
        }

        [TestMethod]
        [TestCategory("Unit")]
        public void GetNextDelay_WithAlternatingSuccessFailure_ShouldResetAndIncreaseCorrectly()
        {
            // Arrange
            var minimumInterval = TimeSpan.FromSeconds(1);
            var maximumInterval = TimeSpan.FromSeconds(60);
            var deltaBackoff = TimeSpan.FromSeconds(1);
            var strategy = new RandomizedExponentialBackoffStrategy(minimumInterval, maximumInterval, deltaBackoff);

            // Act & Assert
            // First failure returns minimum (backoffExponent starts at 0)
            strategy.GetNextDelay(false).Should().Be(minimumInterval);
            
            // Second failure should increase with randomization
            var secondFailure = strategy.GetNextDelay(false);
            secondFailure.Should().BeGreaterThan(minimumInterval);
            
            // Success resets to minimum
            strategy.GetNextDelay(true).Should().Be(minimumInterval);
            
            // After success, backoffExponent is set to 1, so next failure includes randomization
            var afterResetFailure = strategy.GetNextDelay(false);
            // With randomization factor of 0.2 and deltaBackoff of 1s, expected range is [1.8s, 2.2s]
            var expectedMin = minimumInterval + TimeSpan.FromMilliseconds(0.8 * 1000);
            var expectedMax = minimumInterval + TimeSpan.FromMilliseconds(1.2 * 1000);
            afterResetFailure.Should().BeGreaterThanOrEqualTo(expectedMin);
            afterResetFailure.Should().BeLessThanOrEqualTo(expectedMax);
        }

        [TestMethod]
        [TestCategory("Unit")]
        public void GetNextDelay_WithZeroMinimumInterval_ShouldAllowZeroDelay()
        {
            // Arrange
            var minimumInterval = TimeSpan.Zero;
            var maximumInterval = TimeSpan.FromSeconds(30);
            var strategy = new RandomizedExponentialBackoffStrategy(minimumInterval, maximumInterval);

            // Act
            var delay = strategy.GetNextDelay(true);

            // Assert
            delay.Should().Be(TimeSpan.Zero);
        }

        [TestMethod]
        [TestCategory("Unit")]
        public void GetNextDelay_WithSameMinimumAndMaximum_ShouldAlwaysReturnSameValue()
        {
            // Arrange
            var interval = TimeSpan.FromSeconds(5);
            var strategy = new RandomizedExponentialBackoffStrategy(interval, interval);

            // Act
            var successDelay = strategy.GetNextDelay(true);
            var firstFailureDelay = strategy.GetNextDelay(false);
            var secondFailureDelay = strategy.GetNextDelay(false);

            // Assert
            using (new AssertionScope())
            {
                successDelay.Should().Be(interval);
                firstFailureDelay.Should().Be(interval);
                secondFailureDelay.Should().Be(interval);
            }
        }

        #endregion

    }

}
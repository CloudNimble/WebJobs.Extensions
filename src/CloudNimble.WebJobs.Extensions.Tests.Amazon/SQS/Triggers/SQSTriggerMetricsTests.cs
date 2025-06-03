// Copyright (c) CloudNimble, Inc. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using CloudNimble.WebJobs.Extensions.Amazon.SQS.Triggers;
using FluentAssertions;
using Microsoft.Azure.WebJobs.Host.Scale;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace CloudNimble.WebJobs.Extensions.Tests.Amazon.SQS.Triggers
{

    /// <summary>
    /// Unit tests for <see cref="SQSTriggerMetrics"/>.
    /// </summary>
    [TestClass]
    public class SQSTriggerMetricsTests
    {

        [TestMethod]
        public void DefaultConstructor_InitializesWithDefaultValues()
        {
            // Arrange & Act
            var metrics = new SQSTriggerMetrics();

            // Assert
            metrics.MessageCount.Should().Be(0);
            metrics.QueueTime.Should().Be(TimeSpan.Zero);
        }

        [TestMethod]
        public void MessageCount_CanBeSetAndRetrieved()
        {
            // Arrange
            var metrics = new SQSTriggerMetrics();
            const int expectedCount = 42;

            // Act
            metrics.MessageCount = expectedCount;

            // Assert
            metrics.MessageCount.Should().Be(expectedCount);
        }

        [TestMethod]
        public void QueueTime_CanBeSetAndRetrieved()
        {
            // Arrange
            var metrics = new SQSTriggerMetrics();
            var expectedTime = TimeSpan.FromMinutes(5.5);

            // Act
            metrics.QueueTime = expectedTime;

            // Assert
            metrics.QueueTime.Should().Be(expectedTime);
        }

        [TestMethod]
        public void SQSTriggerMetrics_InheritsFromScaleMetrics()
        {
            // Arrange & Act
            var metrics = new SQSTriggerMetrics();

            // Assert
            metrics.Should().BeAssignableTo<ScaleMetrics>();
        }

        [TestMethod]
        public void SQSTriggerMetrics_IsInternalClass()
        {
            // Arrange
            var type = typeof(SQSTriggerMetrics);

            // Assert
            type.IsNotPublic.Should().BeTrue();
        }

        [TestMethod]
        public void AllProperties_CanBeSetSimultaneously()
        {
            // Arrange
            var metrics = new SQSTriggerMetrics();

            // Act
            metrics.MessageCount = 100;
            metrics.QueueTime = TimeSpan.FromHours(2);

            // Assert
            metrics.MessageCount.Should().Be(100);
            metrics.QueueTime.Should().Be(TimeSpan.FromHours(2));
        }

        [TestMethod]
        public void QueueTime_HandlesNegativeTimeSpan()
        {
            // Arrange
            var metrics = new SQSTriggerMetrics();
            var negativeTime = TimeSpan.FromSeconds(-30);

            // Act
            metrics.QueueTime = negativeTime;

            // Assert
            metrics.QueueTime.Should().Be(negativeTime);
        }

    }

}
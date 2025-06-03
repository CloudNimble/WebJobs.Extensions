// Copyright (c) CloudNimble, Inc. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using CloudNimble.WebJobs.Extensions.Amazon.SQS.Listeners;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Reflection;

namespace CloudNimble.WebJobs.Extensions.Tests.Amazon.SQS.Listeners
{

    /// <summary>
    /// Unit tests for <see cref="SQSPollingIntervals"/>.
    /// </summary>
    [TestClass]
    public class SQSPollingIntervalsTests
    {

        [TestMethod]
        public void Minimum_HasCorrectValue()
        {
            // Arrange
            var type = typeof(SQSPollingIntervals);
            var field = type.GetField("Minimum", BindingFlags.Static | BindingFlags.Public);

            // Act
            var value = (TimeSpan)field.GetValue(null);

            // Assert
            value.Should().Be(TimeSpan.FromMilliseconds(100));
        }

        [TestMethod]
        public void DefaultMaximum_HasCorrectValue()
        {
            // Arrange
            var type = typeof(SQSPollingIntervals);
            var field = type.GetField("DefaultMaximum", BindingFlags.Static | BindingFlags.Public);

            // Act
            var value = (TimeSpan)field.GetValue(null);

            // Assert
            value.Should().Be(TimeSpan.FromMinutes(1));
        }

        [TestMethod]
        public void SQSPollingIntervals_IsInternalStaticClass()
        {
            // Arrange
            var type = typeof(SQSPollingIntervals);

            // Act & Assert
            type.IsNotPublic.Should().BeTrue(); // internal
            type.IsAbstract.Should().BeTrue();
            type.IsSealed.Should().BeTrue();
            type.IsClass.Should().BeTrue();
        }

        [TestMethod]
        public void SQSPollingIntervals_HasNoInstanceConstructor()
        {
            // Arrange
            var type = typeof(SQSPollingIntervals);

            // Act
            var constructors = type.GetConstructors(
                BindingFlags.Public | 
                BindingFlags.NonPublic | 
                BindingFlags.Instance);

            // Assert
            constructors.Should().BeEmpty();
        }

        [TestMethod]
        public void Intervals_AreLogicallyConsistent()
        {
            // Arrange
            var type = typeof(SQSPollingIntervals);
            var minimumField = type.GetField("Minimum", BindingFlags.Static | BindingFlags.Public);
            var maximumField = type.GetField("DefaultMaximum", BindingFlags.Static | BindingFlags.Public);

            // Act
            var minimum = (TimeSpan)minimumField.GetValue(null);
            var maximum = (TimeSpan)maximumField.GetValue(null);

            // Assert
            minimum.Should().BeLessThan(maximum);
            minimum.Should().BeGreaterThan(TimeSpan.Zero);
            maximum.Should().BeGreaterThan(TimeSpan.Zero);
        }

    }

}
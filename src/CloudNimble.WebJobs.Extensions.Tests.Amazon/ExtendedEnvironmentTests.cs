// Copyright (c) CloudNimble, Inc. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using CloudNimble.WebJobs.Extensions.Amazon;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace CloudNimble.WebJobs.Extensions.Tests.Amazon
{

    /// <summary>
    /// Unit tests for <see cref="ExtendedEnvironment"/>.
    /// </summary>
    [TestClass]
    public class ExtendedEnvironmentTests
    {

        [TestMethod]
        public void GetProcessorCount_ReturnsEnvironmentProcessorCount()
        {
            // Arrange
            var expectedCount = Environment.ProcessorCount;

            // Act
            var actualCount = ExtendedEnvironment.GetProcessorCount();

            // Assert
            actualCount.Should().Be(expectedCount);
            actualCount.Should().BeGreaterThan(0);
        }

        [TestMethod]
        public void ExtendedEnvironment_IsStaticClass()
        {
            // Arrange
            var type = typeof(ExtendedEnvironment);

            // Act & Assert
            type.IsAbstract.Should().BeTrue();
            type.IsSealed.Should().BeTrue();
            type.IsClass.Should().BeTrue();
        }

        [TestMethod]
        public void ExtendedEnvironment_HasNoInstanceConstructor()
        {
            // Arrange
            var type = typeof(ExtendedEnvironment);

            // Act
            var constructors = type.GetConstructors(
                System.Reflection.BindingFlags.Public | 
                System.Reflection.BindingFlags.NonPublic | 
                System.Reflection.BindingFlags.Instance);

            // Assert
            constructors.Should().BeEmpty();
        }

    }

}
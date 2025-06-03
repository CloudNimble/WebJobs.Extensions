// Copyright (c) CloudNimble, Inc. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using CloudNimble.WebJobs.Extensions.Amazon;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Reflection;

namespace CloudNimble.WebJobs.Extensions.Tests.Amazon
{

    /// <summary>
    /// Unit tests for <see cref="AmazonConstants"/>.
    /// </summary>
    [TestClass]
    public class AmazonConstantsTests
    {

        [TestMethod]
        public void SQSConfigSectionName_HasCorrectValue()
        {
            // Arrange & Act
            const string expectedValue = "Amazon:SQS:Queues";

            // Assert
            AmazonConstants.SQSConfigSectionName.Should().Be(expectedValue);
        }

        [TestMethod]
        public void SQSExtensionName_InternalConstant_HasCorrectValue()
        {
            // Arrange
            var field = typeof(AmazonConstants).GetField("SQSExtensionName", 
                BindingFlags.NonPublic | BindingFlags.Static);

            // Act
            var value = field?.GetValue(null) as string;

            // Assert
            field.Should().NotBeNull();
            value.Should().Be("AmazonSQS");
        }

        [TestMethod]
        public void AmazonConstants_IsStaticClass()
        {
            // Arrange
            var type = typeof(AmazonConstants);

            // Act & Assert
            type.IsAbstract.Should().BeTrue();
            type.IsSealed.Should().BeTrue();
            type.IsClass.Should().BeTrue();
        }

        [TestMethod]
        public void AmazonConstants_HasNoInstanceConstructor()
        {
            // Arrange
            var type = typeof(AmazonConstants);

            // Act
            var constructors = type.GetConstructors(
                BindingFlags.Public | 
                BindingFlags.NonPublic | 
                BindingFlags.Instance);

            // Assert
            constructors.Should().BeEmpty();
        }

    }

}
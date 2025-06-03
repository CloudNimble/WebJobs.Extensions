// Copyright (c) CloudNimble, Inc. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using CloudNimble.WebJobs.Extensions.Amazon.SQS;
using FluentAssertions;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Description;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Diagnostics;
using System.Linq;

namespace CloudNimble.WebJobs.Extensions.Tests.Amazon.SQS
{

    /// <summary>
    /// Unit tests for <see cref="SQSAttribute"/>.
    /// </summary>
    [TestClass]
    public class SQSAttributeTests
    {

        [TestMethod]
        public void Constructor_WithQueueName_SetsQueueNameProperty()
        {
            // Arrange
            const string queueName = "test-queue";

            // Act
            var attribute = new SQSAttribute(queueName);

            // Assert
            attribute.QueueName.Should().Be(queueName);
            attribute.Connection.Should().BeNull();
        }

        [TestMethod]
        public void Connection_CanBeSetAndRetrieved()
        {
            // Arrange
            const string queueName = "test-queue";
            const string connectionString = "test-connection";
            var attribute = new SQSAttribute(queueName);

            // Act
            attribute.Connection = connectionString;

            // Assert
            attribute.Connection.Should().Be(connectionString);
        }

        [TestMethod]
        public void Attribute_ShouldHaveCorrectAttributeUsage()
        {
            // Arrange & Act
            var attributeUsage = typeof(SQSAttribute)
                .GetCustomAttributes(typeof(AttributeUsageAttribute), false)
                .Cast<AttributeUsageAttribute>()
                .FirstOrDefault();

            // Assert
            attributeUsage.Should().NotBeNull();
            attributeUsage.ValidOn.Should().Be(AttributeTargets.Parameter | AttributeTargets.ReturnValue);
            attributeUsage.AllowMultiple.Should().BeFalse();
            attributeUsage.Inherited.Should().BeTrue();
        }

        [TestMethod]
        public void Attribute_ShouldImplementIConnectionProvider()
        {
            // Arrange & Act
            var attribute = new SQSAttribute("test-queue");

            // Assert
            attribute.Should().BeAssignableTo<IConnectionProvider>();
        }

        [TestMethod]
        public void Attribute_ShouldHaveBindingAttribute()
        {
            // Arrange & Act
            var bindingAttribute = typeof(SQSAttribute)
                .GetCustomAttributes(typeof(BindingAttribute), false)
                .Any();

            // Assert
            bindingAttribute.Should().BeTrue();
        }

        [TestMethod]
        public void QueueName_ShouldHaveAutoResolveAttribute()
        {
            // Arrange & Act
            var property = typeof(SQSAttribute).GetProperty(nameof(SQSAttribute.QueueName));
            var hasAutoResolve = property.GetCustomAttributes(typeof(AutoResolveAttribute), false).Any();

            // Assert
            hasAutoResolve.Should().BeTrue();
        }

        [TestMethod]
        public void Attribute_ShouldHaveDebuggerDisplayAttribute()
        {
            // Arrange & Act
            var debuggerDisplay = typeof(SQSAttribute)
                .GetCustomAttributes(typeof(DebuggerDisplayAttribute), false)
                .Cast<DebuggerDisplayAttribute>()
                .FirstOrDefault();

            // Assert
            debuggerDisplay.Should().NotBeNull();
            debuggerDisplay.Value.Should().Contain("GetDebuggerDisplay");
        }

        [TestMethod]
        [Ignore("SuppressMessage attribute removed in newer version")]
        public void Attribute_ShouldHaveCodeAnalysisSuppressionAttribute()
        {
            // Arrange & Act
            var suppressionAttribute = typeof(SQSAttribute)
                .GetCustomAttributes(typeof(System.Diagnostics.CodeAnalysis.SuppressMessageAttribute), false)
                .Cast<System.Diagnostics.CodeAnalysis.SuppressMessageAttribute>()
                .FirstOrDefault();

            // Assert
            suppressionAttribute.Should().NotBeNull();
            suppressionAttribute.Category.Should().Be("Microsoft.Performance");
            suppressionAttribute.CheckId.Should().Be("CA1813:AvoidUnsealedAttributes");
        }

    }

}
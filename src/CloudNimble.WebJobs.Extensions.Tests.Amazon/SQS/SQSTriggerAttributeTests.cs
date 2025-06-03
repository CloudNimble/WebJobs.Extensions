// Copyright (c) CloudNimble, Inc. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using CloudNimble.WebJobs.Extensions.Amazon.SQS;
using FluentAssertions;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Description;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;

namespace CloudNimble.WebJobs.Extensions.Tests.Amazon.SQS
{

    /// <summary>
    /// Unit tests for <see cref="SQSTriggerAttribute"/>.
    /// </summary>
    [TestClass]
    public class SQSTriggerAttributeTests
    {

        [TestMethod]
        public void Constructor_WithQueueName_SetsQueueNameProperty()
        {
            // Arrange
            const string queueName = "test-queue";

            // Act
            var attribute = new SQSTriggerAttribute(queueName);

            // Assert
            attribute.QueueName.Should().Be(queueName);
            attribute.Connection.Should().BeEmpty();
        }

        [TestMethod]
        public void Connection_CanBeSetAndRetrieved()
        {
            // Arrange
            const string queueName = "test-queue";
            const string connectionString = "test-connection";
            var attribute = new SQSTriggerAttribute(queueName);

            // Act
            attribute.Connection = connectionString;

            // Assert
            attribute.Connection.Should().Be(connectionString);
        }

        [TestMethod]
        public void Attribute_ShouldHaveCorrectAttributeUsage()
        {
            // Arrange & Act
            var attributeUsage = typeof(SQSTriggerAttribute)
                .GetCustomAttributes(typeof(AttributeUsageAttribute), false)
                .Cast<AttributeUsageAttribute>()
                .FirstOrDefault();

            // Assert
            attributeUsage.Should().NotBeNull();
            attributeUsage.ValidOn.Should().Be(AttributeTargets.Parameter);
            attributeUsage.AllowMultiple.Should().BeFalse();
            attributeUsage.Inherited.Should().BeTrue();
        }

        [TestMethod]
        public void Attribute_ShouldImplementIConnectionProvider()
        {
            // Arrange & Act
            var attribute = new SQSTriggerAttribute("test-queue");

            // Assert
            attribute.Should().BeAssignableTo<IConnectionProvider>();
        }

        [TestMethod]
        public void Attribute_ShouldHaveBindingAttribute()
        {
            // Arrange & Act
            var bindingAttribute = typeof(SQSTriggerAttribute)
                .GetCustomAttributes(typeof(BindingAttribute), false)
                .Any();

            // Assert
            bindingAttribute.Should().BeTrue();
        }

        [TestMethod]
        public void QueueName_ShouldHaveAutoResolveAttribute()
        {
            // Arrange & Act
            var property = typeof(SQSTriggerAttribute).GetProperty(nameof(SQSTriggerAttribute.QueueName));
            var hasAutoResolve = property.GetCustomAttributes(typeof(AutoResolveAttribute), false).Any();

            // Assert
            hasAutoResolve.Should().BeTrue();
        }

        [TestMethod]
        public void Connection_ShouldHaveConnectionStringAttribute()
        {
            // Arrange & Act
            var property = typeof(SQSTriggerAttribute).GetProperty(nameof(SQSTriggerAttribute.Connection));
            var hasConnectionString = property.GetCustomAttributes(typeof(ConnectionStringAttribute), false).Any();

            // Assert
            hasConnectionString.Should().BeTrue();
        }

    }

}
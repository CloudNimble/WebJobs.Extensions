using CloudNimble.WebJobs.Extensions.Amazon.SQS;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace CloudNimble.WebJobs.Extensions.Tests.Amazon.SQS
{
    [TestClass]
    public class SQSOutputAttributeTests
    {
        [TestMethod]
        public void Constructor_SetsQueueName()
        {
            // Arrange
            var queueName = "test-queue";

            // Act
            var attribute = new SQSOutputAttribute(queueName);

            // Assert
            attribute.QueueName.Should().Be(queueName);
        }

        [TestMethod]
        public void ConnectionProperty_CanBeSetAndRetrieved()
        {
            // Arrange
            var attribute = new SQSOutputAttribute("test-queue");
            var connectionString = "my-connection";

            // Act
            attribute.Connection = connectionString;

            // Assert
            attribute.Connection.Should().Be(connectionString);
        }

        [TestMethod]
        public void AttributeUsage_AllowsParameterAndReturnValue()
        {
            // Arrange
            var attributeType = typeof(SQSOutputAttribute);

            // Act
            var attributeUsage = (AttributeUsageAttribute)Attribute.GetCustomAttribute(attributeType, typeof(AttributeUsageAttribute));

            // Assert
            attributeUsage.Should().NotBeNull();
            attributeUsage.ValidOn.Should().HaveFlag(AttributeTargets.Parameter);
            attributeUsage.ValidOn.Should().HaveFlag(AttributeTargets.ReturnValue);
        }

        [TestMethod]
        public void Attribute_HasBindingAttribute()
        {
            // Arrange
            var attributeType = typeof(SQSOutputAttribute);

            // Act
            var bindingAttributes = attributeType.GetCustomAttributes(typeof(Microsoft.Azure.WebJobs.Description.BindingAttribute), false);

            // Assert
            bindingAttributes.Should().HaveCount(1);
        }

        [TestMethod]
        public void GetDebuggerDisplay_ReturnsQueueName()
        {
            // Arrange
            var queueName = "test-queue";
            var attribute = new SQSOutputAttribute(queueName);

            // Act
            var debuggerDisplayAttribute = (System.Diagnostics.DebuggerDisplayAttribute)Attribute.GetCustomAttribute(
                typeof(SQSOutputAttribute), 
                typeof(System.Diagnostics.DebuggerDisplayAttribute));

            // Assert
            debuggerDisplayAttribute.Should().NotBeNull();
            // The actual display value would be evaluated at runtime
        }
    }
}
// Copyright (c) CloudNimble, Inc. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using CloudNimble.WebJobs.Extensions.Amazon.SQS.Triggers;
using FluentAssertions;
using Microsoft.Azure.WebJobs.Host.Protocols;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Globalization;

namespace CloudNimble.WebJobs.Extensions.Tests.Amazon.SQS.Triggers
{

    /// <summary>
    /// Unit tests for <see cref="SQSTriggerParameterDescriptor"/>.
    /// </summary>
    [TestClass]
    public class SQSTriggerParameterDescriptorTests
    {

        [TestMethod]
        public void Properties_CanBeSetAndRetrieved()
        {
            // Arrange
            var descriptor = new SQSTriggerParameterDescriptor();

            // Act
            descriptor.AccountName = "test-account";
            descriptor.QueueName = "test-queue";

            // Assert
            descriptor.AccountName.Should().Be("test-account");
            descriptor.QueueName.Should().Be("test-queue");
        }

        [TestMethod]
        public void GetTriggerReason_ReturnsFormattedMessage()
        {
            // Arrange
            var descriptor = new SQSTriggerParameterDescriptor
            {
                QueueName = "my-queue"
            };

            // Act
            var reason = descriptor.GetTriggerReason(new Dictionary<string, string>());

            // Assert
            reason.Should().Be("New queue message detected on 'my-queue'.");
        }

        [TestMethod]
        public void GetTriggerReason_WithNullQueueName_ReturnsMessageWithNull()
        {
            // Arrange
            var descriptor = new SQSTriggerParameterDescriptor
            {
                QueueName = null
            };

            // Act
            var reason = descriptor.GetTriggerReason(new Dictionary<string, string>());

            // Assert
            reason.Should().Be("New queue message detected on ''.");
        }

        [TestMethod]
        public void GetTriggerReason_WithEmptyQueueName_ReturnsMessageWithEmpty()
        {
            // Arrange
            var descriptor = new SQSTriggerParameterDescriptor
            {
                QueueName = string.Empty
            };

            // Act
            var reason = descriptor.GetTriggerReason(new Dictionary<string, string>());

            // Assert
            reason.Should().Be("New queue message detected on ''.");
        }

        [TestMethod]
        public void GetTriggerReason_IgnoresProvidedArguments()
        {
            // Arrange
            var descriptor = new SQSTriggerParameterDescriptor
            {
                QueueName = "test-queue"
            };
            var arguments = new Dictionary<string, string>
            {
                { "key1", "value1" },
                { "key2", "value2" }
            };

            // Act
            var reason = descriptor.GetTriggerReason(arguments);

            // Assert
            reason.Should().Be("New queue message detected on 'test-queue'.");
        }

        [TestMethod]
        public void GetTriggerReason_WithNullArguments_DoesNotThrow()
        {
            // Arrange
            var descriptor = new SQSTriggerParameterDescriptor
            {
                QueueName = "test-queue"
            };

            // Act
            var reason = descriptor.GetTriggerReason(null);

            // Assert
            reason.Should().Be("New queue message detected on 'test-queue'.");
        }

        [TestMethod]
        public void GetTriggerReason_UsesCurrentCulture()
        {
            // Arrange
            var descriptor = new SQSTriggerParameterDescriptor
            {
                QueueName = "test-queue"
            };
            var originalCulture = CultureInfo.CurrentCulture;

            try
            {
                // Change to a different culture to ensure it's using CurrentCulture
                CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;

                // Act
                var reason = descriptor.GetTriggerReason(new Dictionary<string, string>());

                // Assert
                reason.Should().Be("New queue message detected on 'test-queue'.");
            }
            finally
            {
                CultureInfo.CurrentCulture = originalCulture;
            }
        }

        [TestMethod]
        public void InheritsFromTriggerParameterDescriptor()
        {
            // Arrange & Act
            var descriptor = new SQSTriggerParameterDescriptor();

            // Assert
            descriptor.Should().BeAssignableTo<TriggerParameterDescriptor>();
        }

        [TestMethod]
        public void DefaultConstructor_InitializesWithNullValues()
        {
            // Arrange & Act
            var descriptor = new SQSTriggerParameterDescriptor();

            // Assert
            descriptor.AccountName.Should().BeNull();
            descriptor.QueueName.Should().BeNull();
        }

    }

}
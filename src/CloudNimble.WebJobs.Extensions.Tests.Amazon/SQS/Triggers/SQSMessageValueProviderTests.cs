// Copyright (c) CloudNimble, Inc. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using CloudNimble.WebJobs.Extensions.Amazon.SQS.Triggers;
using CloudNimble.WebJobs.Extensions.Tests.Amazon.SQS.TestHelpers;
using FluentAssertions;
using Microsoft.Azure.WebJobs.Host.Bindings;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Threading.Tasks;

namespace CloudNimble.WebJobs.Extensions.Tests.Amazon.SQS.Triggers
{

    /// <summary>
    /// Unit tests for <see cref="SQSMessageValueProvider"/>.
    /// </summary>
    [TestClass]
    public class SQSMessageValueProviderTests
    {

        [TestMethod]
        public void Constructor_WithValidParameters_Succeeds()
        {
            // Arrange
            var message = SQSTestHelpers.CreateTestMessage("test body");
            var value = "test value";
            var valueType = typeof(string);

            // Act
            var provider = new SQSMessageValueProvider(message, value, valueType);

            // Assert
            provider.Should().NotBeNull();
            provider.Type.Should().Be(valueType);
        }

        [TestMethod]
        public void Constructor_WithNullValue_Succeeds()
        {
            // Arrange
            var message = SQSTestHelpers.CreateTestMessage();
            var valueType = typeof(string);

            // Act
            var provider = new SQSMessageValueProvider(message, null, valueType);

            // Assert
            provider.Should().NotBeNull();
            provider.Type.Should().Be(valueType);
        }

        [TestMethod]
        public void Constructor_WithMismatchedValueType_ThrowsInvalidOperationException()
        {
            // Arrange
            var message = SQSTestHelpers.CreateTestMessage();
            var value = 123; // int
            var valueType = typeof(string); // Expecting string

            // Act & Assert
            Action act = () => new SQSMessageValueProvider(message, value, valueType);
            act.Should().Throw<InvalidOperationException>()
                .WithMessage("value is not of the correct type.");
        }

        [TestMethod]
        public void Constructor_WithDerivedType_Succeeds()
        {
            // Arrange
            var message = SQSTestHelpers.CreateTestMessage();
            var value = "test"; // string
            var valueType = typeof(object); // Base type

            // Act
            var provider = new SQSMessageValueProvider(message, value, valueType);

            // Assert
            provider.Should().NotBeNull();
        }

        [TestMethod]
        public async Task GetValueAsync_ReturnsStoredValue()
        {
            // Arrange
            var message = SQSTestHelpers.CreateTestMessage();
            var expectedValue = new { Name = "Test", Value = 42 };
            var valueType = expectedValue.GetType();
            var provider = new SQSMessageValueProvider(message, expectedValue, valueType);

            // Act
            var result = await provider.GetValueAsync();

            // Assert
            result.Should().BeSameAs(expectedValue);
        }

        [TestMethod]
        public async Task GetValueAsync_WithNullValue_ReturnsNull()
        {
            // Arrange
            var message = SQSTestHelpers.CreateTestMessage();
            var provider = new SQSMessageValueProvider(message, null, typeof(string));

            // Act
            var result = await provider.GetValueAsync();

            // Assert
            result.Should().BeNull();
        }

        [TestMethod]
        public void ToInvokeString_ReturnsMessageBody()
        {
            // Arrange
            const string messageBody = "This is the message body";
            var message = SQSTestHelpers.CreateTestMessage(body: messageBody);
            var provider = new SQSMessageValueProvider(message, "any value", typeof(string));

            // Act
            var result = provider.ToInvokeString();

            // Assert
            result.Should().Be(messageBody);
        }

        [TestMethod]
        public void ToInvokeString_WithEmptyBody_ReturnsEmptyString()
        {
            // Arrange
            var message = SQSTestHelpers.CreateTestMessage(body: string.Empty);
            var provider = new SQSMessageValueProvider(message, null, typeof(string));

            // Act
            var result = provider.ToInvokeString();

            // Assert
            result.Should().BeEmpty();
        }

        [TestMethod]
        public void Type_ReturnsValueType()
        {
            // Arrange
            var message = SQSTestHelpers.CreateTestMessage();
            var valueType = typeof(DateTime);
            var provider = new SQSMessageValueProvider(message, DateTime.Now, valueType);

            // Act
            var result = provider.Type;

            // Assert
            result.Should().Be(valueType);
        }

        [TestMethod]
        public void ImplementsIValueProvider()
        {
            // Arrange
            var message = SQSTestHelpers.CreateTestMessage();
            var provider = new SQSMessageValueProvider(message, "value", typeof(string));

            // Assert
            provider.Should().BeAssignableTo<IValueProvider>();
        }

        [TestMethod]
        public void SQSMessageValueProvider_IsInternalClass()
        {
            // Arrange
            var type = typeof(SQSMessageValueProvider);

            // Assert
            type.IsNotPublic.Should().BeTrue();
        }

    }

}
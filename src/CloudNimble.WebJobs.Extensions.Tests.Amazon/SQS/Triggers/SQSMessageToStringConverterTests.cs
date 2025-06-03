// Copyright (c) CloudNimble, Inc. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using CloudNimble.WebJobs.Extensions.Amazon.SQS;
using CloudNimble.WebJobs.Extensions.Amazon.SQS.Triggers;
using CloudNimble.WebJobs.Extensions.Tests.Amazon.SQS.TestHelpers;
using FluentAssertions;
using Microsoft.Azure.WebJobs;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace CloudNimble.WebJobs.Extensions.Tests.Amazon.SQS.Triggers
{

    /// <summary>
    /// Unit tests for <see cref="SQSMessageToStringConverter"/>.
    /// </summary>
    [TestClass]
    public class SQSMessageToStringConverterTests
    {

        private SQSMessageToStringConverter _converter;

        [TestInitialize]
        public void Initialize()
        {
            _converter = new SQSMessageToStringConverter();
        }

        [TestMethod]
        public void Convert_WithValidMessage_ReturnsBody()
        {
            // Arrange
            const string expectedBody = "Hello, World!";
            var message = SQSTestHelpers.CreateTestMessage(body: expectedBody);

            // Act
            var result = _converter.Convert(message);

            // Assert
            result.Should().Be(expectedBody);
        }

        [TestMethod]
        public void Convert_WithEmptyBody_ReturnsEmptyString()
        {
            // Arrange
            var message = SQSTestHelpers.CreateTestMessage(body: string.Empty);

            // Act
            var result = _converter.Convert(message);

            // Assert
            result.Should().BeEmpty();
        }

        [TestMethod]
        public void Convert_WithNullInput_ThrowsArgumentNullException()
        {
            // Act & Assert
            _converter.Invoking(c => c.Convert(null))
                .Should().Throw<ArgumentNullException>()
                .WithParameterName("input");
        }

        [TestMethod]
        public void Convert_WithJsonBody_ReturnsJsonString()
        {
            // Arrange
            const string jsonBody = @"{""name"": ""John"", ""age"": 30}";
            var message = SQSTestHelpers.CreateTestMessage(body: jsonBody);

            // Act
            var result = _converter.Convert(message);

            // Assert
            result.Should().Be(jsonBody);
        }

        [TestMethod]
        public void Convert_WithSyntheticMessage_ReturnsBody()
        {
            // Arrange
            const string expectedBody = "synthetic body";
            var message = SQSTestHelpers.CreateSyntheticMessage(body: expectedBody);

            // Act
            var result = _converter.Convert(message);

            // Assert
            result.Should().Be(expectedBody);
        }

        [TestMethod]
        public void Converter_ImplementsIConverter()
        {
            // Assert
            _converter.Should().BeAssignableTo<IConverter<SQSMessage, string>>();
        }

        [TestMethod]
        public void Converter_IsInternal()
        {
            // Arrange
            var type = typeof(SQSMessageToStringConverter);

            // Assert
            type.IsNotPublic.Should().BeTrue();
        }

    }

}
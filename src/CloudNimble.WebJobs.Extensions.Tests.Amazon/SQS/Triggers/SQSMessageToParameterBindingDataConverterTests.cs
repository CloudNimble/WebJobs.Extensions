// Copyright (c) CloudNimble, Inc. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using CloudNimble.WebJobs.Extensions.Amazon;
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
    /// Unit tests for <see cref="SQSMessageToParameterBindingDataConverter"/>.
    /// </summary>
    [TestClass]
    public class SQSMessageToParameterBindingDataConverterTests
    {

        private SQSMessageToParameterBindingDataConverter _converter;

        [TestInitialize]
        public void Initialize()
        {
            _converter = new SQSMessageToParameterBindingDataConverter();
        }

        [TestMethod]
        public void Convert_ReturnsValidParameterBindingData()
        {
            // Arrange
            var message = SQSTestHelpers.CreateTestMessage(body: "test message");

            // Act
            var result = _converter.Convert(message);

            // Assert
            result.Should().NotBeNull();
            result.Version.Should().Be("1.0");
            result.Source.Should().Be("AmazonSQS");
            result.ContentType.Should().Be("application/json");
            result.Content.Should().NotBeNull();
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
        public void Convert_UsesCorrectExtensionName()
        {
            // Arrange
            var message = SQSTestHelpers.CreateTestMessage();

            // Act
            var result = _converter.Convert(message);

            // Assert
            result.Source.Should().Be(AmazonConstants.SQSExtensionName);
        }

        [TestMethod]
        public void Convert_CreatesSerializableBinaryData()
        {
            // Arrange
            var message = SQSTestHelpers.CreateTestMessage(body: @"{""key"": ""value""}");

            // Act
            var result = _converter.Convert(message);

            // Assert
            result.Content.Should().NotBeNull();
            // The BinaryData should contain the serialized SQSMessage
            result.Content.ToString().Should().NotBeNullOrEmpty();
        }

        [TestMethod]
        public void Converter_ImplementsIConverter()
        {
            // Assert
            _converter.Should().BeAssignableTo<IConverter<SQSMessage, ParameterBindingData>>();
        }

        [TestMethod]
        public void Converter_IsInternal()
        {
            // Arrange
            var type = typeof(SQSMessageToParameterBindingDataConverter);

            // Assert
            type.IsNotPublic.Should().BeTrue();
        }

    }

}
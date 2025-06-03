// Copyright (c) CloudNimble, Inc. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using CloudNimble.WebJobs.Extensions.Amazon.SQS;
using CloudNimble.WebJobs.Extensions.Amazon.SQS.Triggers;
using CloudNimble.WebJobs.Extensions.Common.Converters;
using CloudNimble.WebJobs.Extensions.Tests.Amazon.SQS.TestHelpers;
using FluentAssertions;
using Microsoft.Azure.WebJobs;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CloudNimble.WebJobs.Extensions.Tests.Amazon.SQS.Triggers
{

    /// <summary>
    /// Unit tests for <see cref="OutputConverter{TInput}"/>.
    /// </summary>
    [TestClass]
    public class OutputConverterTests
    {

        [TestMethod]
        public void TryConvert_WithMatchingType_CallsInnerConverterAndReturnsTrue()
        {
            // Arrange
            var expectedMessage = SQSTestHelpers.CreateTestMessage();
            var innerConverter = new TestStringToSQSMessageConverter(expectedMessage);
            var converter = new OutputConverter<string>(innerConverter);

            // Act
            var result = converter.TryConvert("test input", out var output);

            // Assert
            result.Should().BeTrue();
            output.Should().BeSameAs(expectedMessage);
            innerConverter.WasCalled.Should().BeTrue();
            innerConverter.LastInput.Should().Be("test input");
        }

        [TestMethod]
        public void TryConvert_WithNonMatchingType_ReturnsFalse()
        {
            // Arrange
            var innerConverter = new TestStringToSQSMessageConverter(null);
            var converter = new OutputConverter<string>(innerConverter);

            // Act
            var result = converter.TryConvert(123, out var output);

            // Assert
            result.Should().BeFalse();
            output.Should().BeNull();
            innerConverter.WasCalled.Should().BeFalse();
        }

        [TestMethod]
        public void TryConvert_WithNull_ReturnsFalse()
        {
            // Arrange
            var innerConverter = new TestStringToSQSMessageConverter(null);
            var converter = new OutputConverter<string>(innerConverter);

            // Act
            var result = converter.TryConvert(null, out var output);

            // Assert
            result.Should().BeFalse();
            output.Should().BeNull();
            innerConverter.WasCalled.Should().BeFalse();
        }

        [TestMethod]
        public void TryConvert_WithDerivedType_CallsInnerConverter()
        {
            // Arrange
            var expectedMessage = SQSTestHelpers.CreateTestMessage();
            var innerConverter = new TestObjectToSQSMessageConverter(expectedMessage);
            var converter = new OutputConverter<TestBaseClass>(innerConverter);
            var derivedInput = new TestDerivedClass { Value = "derived" };

            // Act
            var result = converter.TryConvert(derivedInput, out var output);

            // Assert
            result.Should().BeTrue();
            output.Should().BeSameAs(expectedMessage);
            innerConverter.WasCalled.Should().BeTrue();
            innerConverter.LastInput.Should().BeSameAs(derivedInput);
        }

        [TestMethod]
        public void Converter_ImplementsIObjectToTypeConverter()
        {
            // Arrange
            var innerConverter = new TestStringToSQSMessageConverter(null);
            var converter = new OutputConverter<string>(innerConverter);

            // Assert
            converter.Should().BeAssignableTo<IObjectToTypeConverter<SQSMessage>>();
        }

        [TestMethod]
        public void Converter_IsInternal()
        {
            // Arrange
            var type = typeof(OutputConverter<>);

            // Assert
            type.IsNotPublic.Should().BeTrue();
        }

        #region Test Helpers

        private class TestStringToSQSMessageConverter : IConverter<string, SQSMessage>
        {
            private readonly SQSMessage _returnValue;

            public TestStringToSQSMessageConverter(SQSMessage returnValue)
            {
                _returnValue = returnValue;
            }

            public bool WasCalled { get; private set; }
            public string LastInput { get; private set; }

            public SQSMessage Convert(string input)
            {
                WasCalled = true;
                LastInput = input;
                return _returnValue;
            }
        }

        private class TestObjectToSQSMessageConverter : IConverter<TestBaseClass, SQSMessage>
        {
            private readonly SQSMessage _returnValue;

            public TestObjectToSQSMessageConverter(SQSMessage returnValue)
            {
                _returnValue = returnValue;
            }

            public bool WasCalled { get; private set; }
            public TestBaseClass LastInput { get; private set; }

            public SQSMessage Convert(TestBaseClass input)
            {
                WasCalled = true;
                LastInput = input;
                return _returnValue;
            }
        }

        private class TestBaseClass
        {
            public string Value { get; set; }
        }

        private class TestDerivedClass : TestBaseClass
        {
        }

        #endregion

    }

}
// Copyright (c) CloudNimble, Inc. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using CloudNimble.WebJobs.Extensions.Common;
using CloudNimble.WebJobs.Extensions.Common.Queues;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace CloudNimble.WebJobs.Extensions.Tests.Common.Queues
{

    /// <summary>
    /// Tests for the <see cref="QueueMessageEncoding"/> enum.
    /// </summary>
    [TestClass]
    [TestCategory("Unit")]
    public class QueueMessageEncodingTests
    {

        #region Enum Value Tests

        /// <summary>
        /// Tests that the enum contains the expected values.
        /// </summary>
        [TestMethod]
        public void QueueMessageEncoding_WhenCheckingValues_ShouldContainExpectedValues()
        {
            var enumValues = Enum.GetValues<QueueMessageEncoding>();

            enumValues.Should().Contain(QueueMessageEncoding.None);
            enumValues.Should().Contain(QueueMessageEncoding.Base64);
            enumValues.Should().HaveCount(2);
        }

        /// <summary>
        /// Tests that None has the expected underlying value.
        /// </summary>
        [TestMethod]
        public void QueueMessageEncoding_None_ShouldHaveValueZero()
        {
            ((int)QueueMessageEncoding.None).Should().Be(0);
        }

        /// <summary>
        /// Tests that Base64 has the expected underlying value.
        /// </summary>
        [TestMethod]
        public void QueueMessageEncoding_Base64_ShouldHaveValueOne()
        {
            ((int)QueueMessageEncoding.Base64).Should().Be(1);
        }

        #endregion

        #region String Conversion Tests

        /// <summary>
        /// Tests that enum values can be converted to string correctly.
        /// </summary>
        [TestMethod]
        public void QueueMessageEncoding_WhenConvertedToString_ShouldReturnCorrectNames()
        {
            QueueMessageEncoding.None.ToString().Should().Be("None");
            QueueMessageEncoding.Base64.ToString().Should().Be("Base64");
        }

        /// <summary>
        /// Tests that enum values can be parsed from strings correctly.
        /// </summary>
        [TestMethod]
        public void QueueMessageEncoding_WhenParsedFromString_ShouldReturnCorrectValues()
        {
            Enum.Parse<QueueMessageEncoding>("None").Should().Be(QueueMessageEncoding.None);
            Enum.Parse<QueueMessageEncoding>("Base64").Should().Be(QueueMessageEncoding.Base64);
        }

        /// <summary>
        /// Tests that enum values can be parsed from strings with case insensitivity.
        /// </summary>
        [TestMethod]
        public void QueueMessageEncoding_WhenParsedFromStringIgnoreCase_ShouldReturnCorrectValues()
        {
            Enum.Parse<QueueMessageEncoding>("none", true).Should().Be(QueueMessageEncoding.None);
            Enum.Parse<QueueMessageEncoding>("base64", true).Should().Be(QueueMessageEncoding.Base64);
            Enum.Parse<QueueMessageEncoding>("NONE", true).Should().Be(QueueMessageEncoding.None);
            Enum.Parse<QueueMessageEncoding>("BASE64", true).Should().Be(QueueMessageEncoding.Base64);
        }

        /// <summary>
        /// Tests that parsing invalid strings throws the expected exception.
        /// </summary>
        [TestMethod]
        public void QueueMessageEncoding_WhenParsingInvalidString_ShouldThrowArgumentException()
        {
            var action1 = () => Enum.Parse<QueueMessageEncoding>("Invalid");
            var action2 = () => Enum.Parse<QueueMessageEncoding>("NotAnEncoding");
            var action3 = () => Enum.Parse<QueueMessageEncoding>("");

            action1.Should().Throw<ArgumentException>();
            action2.Should().Throw<ArgumentException>();
            action3.Should().Throw<ArgumentException>();
        }

        #endregion

        #region TryParse Tests

        /// <summary>
        /// Tests that TryParse works correctly for valid enum names.
        /// </summary>
        [TestMethod]
        public void QueueMessageEncoding_WhenTryParsingValidNames_ShouldSucceed()
        {
            Enum.TryParse<QueueMessageEncoding>("None", out var result1).Should().BeTrue();
            result1.Should().Be(QueueMessageEncoding.None);

            Enum.TryParse<QueueMessageEncoding>("Base64", out var result2).Should().BeTrue();
            result2.Should().Be(QueueMessageEncoding.Base64);
        }

        /// <summary>
        /// Tests that TryParse works correctly for valid enum names with case insensitivity.
        /// </summary>
        [TestMethod]
        public void QueueMessageEncoding_WhenTryParsingValidNamesIgnoreCase_ShouldSucceed()
        {
            Enum.TryParse<QueueMessageEncoding>("none", true, out var result1).Should().BeTrue();
            result1.Should().Be(QueueMessageEncoding.None);

            Enum.TryParse<QueueMessageEncoding>("BASE64", true, out var result2).Should().BeTrue();
            result2.Should().Be(QueueMessageEncoding.Base64);
        }

        /// <summary>
        /// Tests that TryParse returns false for invalid enum names.
        /// </summary>
        [TestMethod]
        public void QueueMessageEncoding_WhenTryParsingInvalidNames_ShouldReturnFalse()
        {
            Enum.TryParse<QueueMessageEncoding>("Invalid", out var result1).Should().BeFalse();
            result1.Should().Be(default);

            Enum.TryParse<QueueMessageEncoding>("", out var result2).Should().BeFalse();
            result2.Should().Be(default);

            Enum.TryParse<QueueMessageEncoding>("NotAnEncoding", out var result3).Should().BeFalse();
            result3.Should().Be(default);
        }

        #endregion

        #region IsDefined Tests

        /// <summary>
        /// Tests that IsDefined correctly identifies valid enum values.
        /// </summary>
        [TestMethod]
        public void QueueMessageEncoding_WhenCheckingDefinedValues_ShouldReturnTrue()
        {
            Enum.IsDefined(typeof(QueueMessageEncoding), QueueMessageEncoding.None).Should().BeTrue();
            Enum.IsDefined(typeof(QueueMessageEncoding), QueueMessageEncoding.Base64).Should().BeTrue();
        }

        /// <summary>
        /// Tests that IsDefined correctly identifies invalid enum values.
        /// </summary>
        [TestMethod]
        public void QueueMessageEncoding_WhenCheckingUndefinedValues_ShouldReturnFalse()
        {
            Enum.IsDefined(typeof(QueueMessageEncoding), (QueueMessageEncoding)999).Should().BeFalse();
            Enum.IsDefined(typeof(QueueMessageEncoding), (QueueMessageEncoding)(-1)).Should().BeFalse();
            Enum.IsDefined(typeof(QueueMessageEncoding), (QueueMessageEncoding)2).Should().BeFalse();
        }

        #endregion

        #region Comparison Tests

        /// <summary>
        /// Tests that enum values can be compared correctly.
        /// </summary>
        [TestMethod]
        public void QueueMessageEncoding_WhenComparing_ShouldWorkCorrectly()
        {
#pragma warning disable CS1718 // Comparison made to same variable
            (QueueMessageEncoding.None == QueueMessageEncoding.None).Should().BeTrue();
            (QueueMessageEncoding.Base64 == QueueMessageEncoding.Base64).Should().BeTrue();
#pragma warning restore CS1718 // Comparison made to same variable
            (QueueMessageEncoding.None == QueueMessageEncoding.Base64).Should().BeFalse();
            (QueueMessageEncoding.None != QueueMessageEncoding.Base64).Should().BeTrue();
        }

        /// <summary>
        /// Tests that enum values have the expected hash codes.
        /// </summary>
        [TestMethod]
        public void QueueMessageEncoding_WhenGettingHashCode_ShouldBeConsistent()
        {
            QueueMessageEncoding.None.GetHashCode().Should().Be(QueueMessageEncoding.None.GetHashCode());
            QueueMessageEncoding.Base64.GetHashCode().Should().Be(QueueMessageEncoding.Base64.GetHashCode());
            QueueMessageEncoding.None.GetHashCode().Should().NotBe(QueueMessageEncoding.Base64.GetHashCode());
        }

        #endregion

        #region Default Value Tests

        /// <summary>
        /// Tests that the default enum value is None.
        /// </summary>
        [TestMethod]
        public void QueueMessageEncoding_WhenUsingDefault_ShouldBeNone()
        {
            default(QueueMessageEncoding).Should().Be(QueueMessageEncoding.None);
        }

        /// <summary>
        /// Tests that creating a new instance without initialization defaults to None.
        /// </summary>
        [TestMethod]
        public void QueueMessageEncoding_WhenCreatingNewInstance_ShouldDefaultToNone()
        {
            var encoding = new QueueMessageEncoding();

            encoding.Should().Be(QueueMessageEncoding.None);
        }

        #endregion

        #region Integration Tests with JsonSerialization

        /// <summary>
        /// Tests that enum values serialize correctly with JsonSerialization options.
        /// </summary>
        [TestMethod]
        public void QueueMessageEncoding_WhenSerialized_ShouldUseCamelCase()
        {
            var testObject = new { Encoding = QueueMessageEncoding.Base64 };

            var json = System.Text.Json.JsonSerializer.Serialize(testObject, JsonSerialization.Options);

            json.Should().Contain("\"encoding\": \"base64\"");
        }

        /// <summary>
        /// Tests that enum values can be deserialized correctly from camelCase JSON.
        /// </summary>
        [TestMethod]
        public void QueueMessageEncoding_WhenDeserializedFromCamelCase_ShouldWorkCorrectly()
        {
            var json = "{\"encoding\": \"base64\"}";

            var result = System.Text.Json.JsonSerializer.Deserialize<TestEncodingObject>(json, JsonSerialization.Options);

            result.Should().NotBeNull();
            result.Encoding.Should().Be(QueueMessageEncoding.Base64);
        }

        /// <summary>
        /// Tests that both enum values serialize and deserialize correctly.
        /// </summary>
        [TestMethod]
        public void QueueMessageEncoding_WhenRoundTripSerialization_ShouldMaintainValues()
        {
            var testObjects = new[]
            {
                new TestEncodingObject { Encoding = QueueMessageEncoding.None },
                new TestEncodingObject { Encoding = QueueMessageEncoding.Base64 }
            };

            foreach (var original in testObjects)
            {
                var json = System.Text.Json.JsonSerializer.Serialize(original, JsonSerialization.Options);
                var deserialized = System.Text.Json.JsonSerializer.Deserialize<TestEncodingObject>(json, JsonSerialization.Options);

                deserialized.Should().NotBeNull();
                deserialized.Encoding.Should().Be(original.Encoding);
            }
        }

        #endregion

        #region Edge Case Tests

        /// <summary>
        /// Tests enum behavior with explicit casting.
        /// </summary>
        [TestMethod]
        public void QueueMessageEncoding_WhenExplicitlyCast_ShouldWorkCorrectly()
        {
            ((QueueMessageEncoding)0).Should().Be(QueueMessageEncoding.None);
            ((QueueMessageEncoding)1).Should().Be(QueueMessageEncoding.Base64);
        }

        /// <summary>
        /// Tests enum behavior when used in switch statements.
        /// </summary>
        [TestMethod]
        public void QueueMessageEncoding_WhenUsedInSwitch_ShouldWorkCorrectly()
        {
            var noneResult = GetEncodingDescription(QueueMessageEncoding.None);
            var base64Result = GetEncodingDescription(QueueMessageEncoding.Base64);

            noneResult.Should().Be("No encoding");
            base64Result.Should().Be("Base64 encoding");
        }

        /// <summary>
        /// Tests that the enum can be used in collections correctly.
        /// </summary>
        [TestMethod]
        public void QueueMessageEncoding_WhenUsedInCollections_ShouldWorkCorrectly()
        {
            var encodings = new[] { QueueMessageEncoding.None, QueueMessageEncoding.Base64 };
            var encodingSet = new System.Collections.Generic.HashSet<QueueMessageEncoding> 
            { 
                QueueMessageEncoding.None, 
                QueueMessageEncoding.Base64 
            };

            encodings.Should().HaveCount(2);
            encodings.Should().Contain(QueueMessageEncoding.None);
            encodings.Should().Contain(QueueMessageEncoding.Base64);

            encodingSet.Should().HaveCount(2);
            encodingSet.Should().Contain(QueueMessageEncoding.None);
            encodingSet.Should().Contain(QueueMessageEncoding.Base64);
        }

        #endregion

        #region Reflection Tests

        /// <summary>
        /// Tests that enum metadata can be accessed via reflection.
        /// </summary>
        [TestMethod]
        public void QueueMessageEncoding_WhenAccessedViaReflection_ShouldHaveCorrectMetadata()
        {
            var enumType = typeof(QueueMessageEncoding);

            enumType.IsEnum.Should().BeTrue();
            enumType.GetEnumUnderlyingType().Should().Be(typeof(int));
            
            var enumNames = enumType.GetEnumNames();
            enumNames.Should().Contain("None");
            enumNames.Should().Contain("Base64");
            enumNames.Should().HaveCount(2);
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Helper method for switch statement testing.
        /// </summary>
        /// <param name="encoding">The encoding to describe.</param>
        /// <returns>A description of the encoding.</returns>
        private static string GetEncodingDescription(QueueMessageEncoding encoding)
        {
            return encoding switch
            {
                QueueMessageEncoding.None => "No encoding",
                QueueMessageEncoding.Base64 => "Base64 encoding",
                _ => "Unknown encoding"
            };
        }

        #endregion

    }

    #region Test Helper Classes

    /// <summary>
    /// Test class for JSON serialization of QueueMessageEncoding.
    /// </summary>
    internal class TestEncodingObject
    {
        public QueueMessageEncoding Encoding { get; set; }
    }

    #endregion

}
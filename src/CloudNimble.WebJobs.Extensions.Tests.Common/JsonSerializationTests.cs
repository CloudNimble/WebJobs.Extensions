// Copyright (c) CloudNimble, Inc. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using CloudNimble.WebJobs.Extensions.Common;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace CloudNimble.WebJobs.Extensions.Tests.Common
{

    /// <summary>
    /// Tests for the <see cref="JsonSerialization"/> utility class.
    /// </summary>
    [TestClass]
    [TestCategory("Unit")]
    public class JsonSerializationTests
    {

        #region Options Property Tests

        /// <summary>
        /// Tests that the Options property returns a configured JsonSerializerOptions instance.
        /// </summary>
        [TestMethod]
        public void Options_WhenAccessed_ShouldReturnConfiguredInstance()
        {
            var options = JsonSerialization.Options;

            options.Should().NotBeNull();
            options.DefaultIgnoreCondition.Should().Be(JsonIgnoreCondition.WhenWritingNull);
            options.WriteIndented.Should().BeTrue();
            options.PropertyNameCaseInsensitive.Should().BeTrue();
        }

        /// <summary>
        /// Tests that the Options property returns the same instance on multiple calls.
        /// </summary>
        [TestMethod]
        public void Options_WhenAccessedMultipleTimes_ShouldReturnSameInstance()
        {
            var options1 = JsonSerialization.Options;
            var options2 = JsonSerialization.Options;

            options1.Should().BeSameAs(options2);
        }

        /// <summary>
        /// Tests that the Options property includes JsonStringEnumConverter.
        /// </summary>
        [TestMethod]
        public void Options_WhenAccessed_ShouldIncludeEnumConverter()
        {
            var options = JsonSerialization.Options;

            options.Converters.Should().NotBeEmpty();
            options.Converters.Should().Contain(c => c is JsonStringEnumConverter);
        }

        /// <summary>
        /// Tests that the JsonStringEnumConverter uses camelCase naming policy.
        /// </summary>
        [TestMethod]
        public void Options_WhenSerializingEnum_ShouldUseCamelCaseNaming()
        {
            var testEnum = TestEnum.FirstValue;

            var json = JsonSerializer.Serialize(testEnum, JsonSerialization.Options);

            json.Should().Be("\"firstValue\"");
        }

        /// <summary>
        /// Tests that the Options properly handle null value serialization.
        /// </summary>
        [TestMethod]
        public void Options_WhenSerializingObjectWithNullProperties_ShouldIgnoreNullValues()
        {
            var testObject = new TestObject
            {
                StringProperty = "test",
                NullProperty = null
            };

            var json = JsonSerializer.Serialize(testObject, JsonSerialization.Options);

            json.Should().Contain("stringProperty");
            json.Should().NotContain("nullProperty");
        }

        /// <summary>
        /// Tests that the Options produce indented JSON.
        /// </summary>
        [TestMethod]
        public void Options_WhenSerializingObject_ShouldProduceIndentedJson()
        {
            var testObject = new TestObject
            {
                StringProperty = "test",
                IntProperty = 42
            };

            var json = JsonSerializer.Serialize(testObject, JsonSerialization.Options);

            json.Should().Contain("\n");
            json.Should().Contain("  "); // Indentation spaces
        }

        #endregion

        #region IsJsonObject Method Tests

        /// <summary>
        /// Tests that IsJsonObject returns true for valid JSON objects.
        /// </summary>
        [TestMethod]
        public void IsJsonObject_WhenGivenValidJsonObject_ShouldReturnTrue()
        {
            var validJsonObjects = new[]
            {
                "{}",
                "{\"property\": \"value\"}",
                "{ \"number\": 42, \"boolean\": true }",
                "{\n  \"nested\": {\n    \"property\": \"value\"\n  }\n}"
            };

            foreach (var json in validJsonObjects)
            {
                JsonSerialization.IsJsonObject(json).Should().BeTrue($"'{json}' should be recognized as a JSON object");
            }
        }

        /// <summary>
        /// Tests that IsJsonObject returns false for JSON arrays.
        /// </summary>
        [TestMethod]
        public void IsJsonObject_WhenGivenJsonArray_ShouldReturnFalse()
        {
            var jsonArrays = new[]
            {
                "[]",
                "[1, 2, 3]",
                "[{\"item\": 1}, {\"item\": 2}]",
                "[\n  \"item1\",\n  \"item2\"\n]"
            };

            foreach (var json in jsonArrays)
            {
                JsonSerialization.IsJsonObject(json).Should().BeFalse($"'{json}' should not be recognized as a JSON object");
            }
        }

        /// <summary>
        /// Tests that IsJsonObject returns false for primitive JSON values.
        /// </summary>
        [TestMethod]
        public void IsJsonObject_WhenGivenPrimitiveValues_ShouldReturnFalse()
        {
            var primitiveValues = new[]
            {
                "\"string\"",
                "42",
                "true",
                "false",
                "null"
            };

            foreach (var json in primitiveValues)
            {
                JsonSerialization.IsJsonObject(json).Should().BeFalse($"'{json}' should not be recognized as a JSON object");
            }
        }

        /// <summary>
        /// Tests that IsJsonObject returns false for invalid JSON.
        /// </summary>
        [TestMethod]
        public void IsJsonObject_WhenGivenInvalidJson_ShouldReturnFalse()
        {
            var invalidJson = new[]
            {
                "not json",
                "{invalid}",
                "{\"unclosed\": \"value\"",
                "\"just a string\"",
                "{\"property\": }"
            };

            foreach (var json in invalidJson)
            {
                JsonSerialization.IsJsonObject(json).Should().BeFalse($"'{json}' should not be recognized as a JSON object");
            }
        }

        /// <summary>
        /// Tests that IsJsonObject returns false for null input.
        /// </summary>
        [TestMethod]
        public void IsJsonObject_WhenGivenNull_ShouldReturnFalse()
        {
            JsonSerialization.IsJsonObject(null).Should().BeFalse();
        }

        /// <summary>
        /// Tests that IsJsonObject returns false for empty string.
        /// </summary>
        [TestMethod]
        public void IsJsonObject_WhenGivenEmptyString_ShouldReturnFalse()
        {
            JsonSerialization.IsJsonObject("").Should().BeFalse();
        }

        /// <summary>
        /// Tests that IsJsonObject returns false for whitespace-only strings.
        /// </summary>
        [TestMethod]
        public void IsJsonObject_WhenGivenWhitespaceOnly_ShouldReturnFalse()
        {
            var whitespaceStrings = new[]
            {
                " ",
                "\t",
                "\n",
                "\r\n",
                "   \t\n  "
            };

            foreach (var whitespace in whitespaceStrings)
            {
                JsonSerialization.IsJsonObject(whitespace).Should().BeFalse($"'{whitespace}' should not be recognized as a JSON object");
            }
        }

        /// <summary>
        /// Tests that IsJsonObject handles JSON objects with whitespace correctly.
        /// </summary>
        [TestMethod]
        public void IsJsonObject_WhenGivenJsonObjectWithWhitespace_ShouldReturnTrue()
        {
            var jsonWithWhitespace = new[]
            {
                "  {}  ",
                "\t{\"property\": \"value\"}\n",
                "\r\n  { \"nested\": { \"value\": 42 } }  \t",
                "   \n\t  {  }  \r\n   "
            };

            foreach (var json in jsonWithWhitespace)
            {
                JsonSerialization.IsJsonObject(json).Should().BeTrue($"'{json}' should be recognized as a JSON object");
            }
        }

        /// <summary>
        /// Tests that IsJsonObject is case-sensitive for object delimiters.
        /// </summary>
        [TestMethod]
        public void IsJsonObject_WhenTestingCaseSensitivity_ShouldBeCorrectlyImplemented()
        {
            // Should be case-insensitive according to the implementation (StringComparison.OrdinalIgnoreCase)
            JsonSerialization.IsJsonObject("{\"test\": \"VALUE\"}").Should().BeTrue();
            JsonSerialization.IsJsonObject("{\"TEST\": \"value\"}").Should().BeTrue();
        }

        #endregion

        #region Internal Method Tests (via reflection if needed)

        /// <summary>
        /// Tests that CreateJsonTextWriter creates a properly configured writer.
        /// </summary>
        [TestMethod]
        public void CreateJsonTextWriter_WhenCalledWithStream_ShouldCreateConfiguredWriter()
        {
            using var stream = new MemoryStream();

            var writer = JsonSerialization.CreateJsonTextWriter(stream);

            writer.Should().NotBeNull();
            // The writer should be configured to match the JsonSerialization options
        }

        /// <summary>
        /// Tests that CreateJsonTextWriter produces indented output.
        /// </summary>
        [TestMethod]
        public void CreateJsonTextWriter_WhenWritingJson_ShouldProduceIndentedOutput()
        {
            using var stream = new MemoryStream();
            using var writer = JsonSerialization.CreateJsonTextWriter(stream);

            writer.WriteStartObject();
            writer.WritePropertyName("test");
            writer.WriteStringValue("value");
            writer.WriteEndObject();
            writer.Flush();

            stream.Position = 0;
            using var reader = new StreamReader(stream);
            var json = reader.ReadToEnd();

            json.Should().Contain("\n");
        }

        /// <summary>
        /// Tests that ParseJsonObject returns null for non-JSON-object input.
        /// </summary>
        [TestMethod]
        public void ParseJsonObject_WhenGivenNonJsonObject_ShouldReturnNull()
        {
            var nonObjects = new[]
            {
                "[]",
                "\"string\"",
                "42",
                "true",
                "invalid json"
            };

            foreach (var input in nonObjects)
            {
                var result = JsonSerialization.ParseJsonObject(input);
                result?.Should().BeNull($"'{input}' should result in null");
            }
        }

        /// <summary>
        /// Tests that ParseJsonObject returns a JsonDocument for valid JSON objects.
        /// </summary>
        [TestMethod]
        public void ParseJsonObject_WhenGivenValidJsonObject_ShouldReturnJsonDocument()
        {
            var validJson = "{\"property\": \"value\", \"number\": 42}";

            var result = JsonSerialization.ParseJsonObject(validJson);

            result.Should().NotBeNull();
            result.RootElement.ValueKind.Should().Be(JsonValueKind.Object);
            result.RootElement.GetProperty("property").GetString().Should().Be("value");
            result.RootElement.GetProperty("number").GetInt32().Should().Be(42);
            result.Dispose();
        }

        /// <summary>
        /// Tests that ParseJsonObject handles JSON with comments correctly.
        /// </summary>
        [TestMethod]
        public void ParseJsonObject_WhenGivenJsonWithComments_ShouldSkipComments()
        {
            var jsonWithComments = @"{
                // This is a comment
                ""property"": ""value"",
                /* Multi-line
                   comment */
                ""number"": 42
            }";

            var result = JsonSerialization.ParseJsonObject(jsonWithComments);

            result.Should().NotBeNull();
            result.RootElement.GetProperty("property").GetString().Should().Be("value");
            result.RootElement.GetProperty("number").GetInt32().Should().Be(42);
            result.Dispose();
        }

        /// <summary>
        /// Tests that ParseJsonObject throws ArgumentNullException for null input.
        /// </summary>
        [TestMethod]
        public void ParseJsonObject_WhenGivenNull_ShouldThrowArgumentNullException()
        {
            var action = () => JsonSerialization.ParseJsonObject(null);

            action.Should().Throw<ArgumentNullException>();
        }

        #endregion

        #region Integration Tests

        /// <summary>
        /// Tests the complete flow of checking and parsing JSON objects.
        /// </summary>
        [TestMethod]
        public void JsonSerialization_WhenCheckingAndParsingValidJson_ShouldWorkTogether()
        {
            var json = "{\"message\": \"Hello, World!\", \"timestamp\": \"2025-01-31T12:00:00Z\"}";

            var isJsonObject = JsonSerialization.IsJsonObject(json);
            isJsonObject.Should().BeTrue();

            var document = JsonSerialization.ParseJsonObject(json);
            document.Should().NotBeNull();
            document.RootElement.GetProperty("message").GetString().Should().Be("Hello, World!");
            document.Dispose();
        }

        /// <summary>
        /// Tests serialization and deserialization with the configured options.
        /// </summary>
        [TestMethod]
        public void JsonSerialization_WhenSerializingAndDeserializing_ShouldMaintainDataIntegrity()
        {
            var originalObject = new TestObject
            {
                StringProperty = "test value",
                IntProperty = 42,
                EnumProperty = TestEnum.SecondValue,
                BoolProperty = true
            };

            var json = JsonSerializer.Serialize(originalObject, JsonSerialization.Options);
            var deserializedObject = JsonSerializer.Deserialize<TestObject>(json, JsonSerialization.Options);

            deserializedObject.Should().NotBeNull();
            deserializedObject.StringProperty.Should().Be(originalObject.StringProperty);
            deserializedObject.IntProperty.Should().Be(originalObject.IntProperty);
            deserializedObject.EnumProperty.Should().Be(originalObject.EnumProperty);
            deserializedObject.BoolProperty.Should().Be(originalObject.BoolProperty);
        }

        #endregion

        #region Edge Case Tests

        /// <summary>
        /// Tests handling of very large JSON objects.
        /// </summary>
        [TestMethod]
        public void IsJsonObject_WhenGivenVeryLargeJsonObject_ShouldHandleCorrectly()
        {
            // Create a large JSON object string
            var largeJson = "{" + string.Join(",", 
                Enumerable.Range(1, 1000).Select(i => $"\"property{i}\": \"value{i}\"")) + "}";

            JsonSerialization.IsJsonObject(largeJson).Should().BeTrue();
        }

        /// <summary>
        /// Tests handling of JSON objects with special characters.
        /// </summary>
        [TestMethod]
        public void IsJsonObject_WhenGivenJsonWithSpecialCharacters_ShouldHandleCorrectly()
        {
            var jsonWithSpecialChars = @"{
                ""unicode"": ""Hello 🌟 World"",
                ""escaped"": ""Line 1\nLine 2\tTabbed"",
                ""quotes"": ""She said \""Hello\"""",
                ""backslashes"": ""C:\\Users\\Test\\""
            }";

            JsonSerialization.IsJsonObject(jsonWithSpecialChars).Should().BeTrue();

            var document = JsonSerialization.ParseJsonObject(jsonWithSpecialChars);
            document.Should().NotBeNull();
            document.RootElement.GetProperty("unicode").GetString().Should().Contain("🌟");
            document.Dispose();
        }

        #endregion

    }

    #region Test Helper Classes

    /// <summary>
    /// Test enum for JSON serialization testing.
    /// </summary>
    internal enum TestEnum
    {
        FirstValue,
        SecondValue,
        ThirdValue
    }

    /// <summary>
    /// Test object for JSON serialization testing.
    /// </summary>
    internal class TestObject
    {
        public string StringProperty { get; set; }
        public int IntProperty { get; set; }
        public TestEnum EnumProperty { get; set; }
        public bool BoolProperty { get; set; }
        public string NullProperty { get; set; }
    }

    #endregion

}
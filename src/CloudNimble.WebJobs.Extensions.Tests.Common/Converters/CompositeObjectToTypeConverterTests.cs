// Copyright (c) CloudNimble, Inc. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using CloudNimble.WebJobs.Extensions.Common.Converters;
using CloudNimble.WebJobs.Extensions.Tests.Common.Models;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;

namespace CloudNimble.WebJobs.Extensions.Tests.Common.Converters
{

    /// <summary>
    /// Tests for the <see cref="CompositeObjectToTypeConverter{T}"/> class.
    /// </summary>
    [TestClass]
    [TestCategory("Unit")]
    public class CompositeObjectToTypeConverterTests
    {

        #region Constructor Tests

        /// <summary>
        /// Tests that the constructor correctly initializes with a collection of converters.
        /// </summary>
        [TestMethod]
        public void Constructor_WhenCalledWithConverterCollection_ShouldInitializeCorrectly()
        {
            var converters = new List<IObjectToTypeConverter<string>>
            {
                new TestObjectToTypeConverter<string>(),
                new TestObjectToTypeConverter<string>()
            };

            var compositeConverter = new CompositeObjectToTypeConverter<string>(converters);

            compositeConverter.Should().NotBeNull();
        }

        /// <summary>
        /// Tests that the constructor correctly initializes with a params array of converters.
        /// </summary>
        [TestMethod]
        public void Constructor_WhenCalledWithConverterArray_ShouldInitializeCorrectly()
        {
            var converter1 = new TestObjectToTypeConverter<string>();
            var converter2 = new TestObjectToTypeConverter<string>();

            var compositeConverter = new CompositeObjectToTypeConverter<string>(converter1, converter2);

            compositeConverter.Should().NotBeNull();
        }

        /// <summary>
        /// Tests that the constructor throws when converters collection is null.
        /// </summary>
        [TestMethod]
        public void Constructor_WhenConvertersCollectionIsNull_ShouldThrowArgumentNullException()
        {
            IEnumerable<IObjectToTypeConverter<string>> nullConverters = null;

            var action = () => new CompositeObjectToTypeConverter<string>(nullConverters);

            action.Should().Throw<ArgumentNullException>()
                .WithParameterName("converters");
        }

        /// <summary>
        /// Tests that the constructor accepts an empty collection of converters.
        /// </summary>
        [TestMethod]
        public void Constructor_WhenCalledWithEmptyCollection_ShouldAcceptEmptyCollection()
        {
            var emptyConverters = new List<IObjectToTypeConverter<string>>();

            var compositeConverter = new CompositeObjectToTypeConverter<string>(emptyConverters);

            compositeConverter.Should().NotBeNull();
        }

        #endregion

        #region TryConvert Success Tests

        /// <summary>
        /// Tests that TryConvert succeeds when first converter succeeds.
        /// </summary>
        [TestMethod]
        public void TryConvert_WhenFirstConverterSucceeds_ShouldReturnTrueWithResult()
        {
            var converter1 = new TestObjectToTypeConverter<string> { ShouldSucceed = true, Result = "first" };
            var converter2 = new TestObjectToTypeConverter<string> { ShouldSucceed = true, Result = "second" };
            var compositeConverter = new CompositeObjectToTypeConverter<string>(converter1, converter2);

            var success = compositeConverter.TryConvert("input", out var result);

            success.Should().BeTrue();
            result.Should().Be("first");
            converter1.TryConvertCallCount.Should().Be(1);
            converter2.TryConvertCallCount.Should().Be(0); // Should not call second converter
        }

        /// <summary>
        /// Tests that TryConvert succeeds when second converter succeeds after first fails.
        /// </summary>
        [TestMethod]
        public void TryConvert_WhenSecondConverterSucceeds_ShouldReturnTrueWithResult()
        {
            var converter1 = new TestObjectToTypeConverter<string> { ShouldSucceed = false };
            var converter2 = new TestObjectToTypeConverter<string> { ShouldSucceed = true, Result = "second" };
            var converter3 = new TestObjectToTypeConverter<string> { ShouldSucceed = true, Result = "third" };
            var compositeConverter = new CompositeObjectToTypeConverter<string>(converter1, converter2, converter3);

            var success = compositeConverter.TryConvert("input", out var result);

            success.Should().BeTrue();
            result.Should().Be("second");
            converter1.TryConvertCallCount.Should().Be(1);
            converter2.TryConvertCallCount.Should().Be(1);
            converter3.TryConvertCallCount.Should().Be(0); // Should not call third converter
        }

        /// <summary>
        /// Tests that TryConvert succeeds when last converter succeeds.
        /// </summary>
        [TestMethod]
        public void TryConvert_WhenLastConverterSucceeds_ShouldReturnTrueWithResult()
        {
            var converter1 = new TestObjectToTypeConverter<string> { ShouldSucceed = false };
            var converter2 = new TestObjectToTypeConverter<string> { ShouldSucceed = false };
            var converter3 = new TestObjectToTypeConverter<string> { ShouldSucceed = true, Result = "third" };
            var compositeConverter = new CompositeObjectToTypeConverter<string>(converter1, converter2, converter3);

            var success = compositeConverter.TryConvert("input", out var result);

            success.Should().BeTrue();
            result.Should().Be("third");
            converter1.TryConvertCallCount.Should().Be(1);
            converter2.TryConvertCallCount.Should().Be(1);
            converter3.TryConvertCallCount.Should().Be(1);
        }

        #endregion

        #region TryConvert Failure Tests

        /// <summary>
        /// Tests that TryConvert returns false when all converters fail.
        /// </summary>
        [TestMethod]
        public void TryConvert_WhenAllConvertersFail_ShouldReturnFalseWithDefault()
        {
            var converter1 = new TestObjectToTypeConverter<string> { ShouldSucceed = false };
            var converter2 = new TestObjectToTypeConverter<string> { ShouldSucceed = false };
            var converter3 = new TestObjectToTypeConverter<string> { ShouldSucceed = false };
            var compositeConverter = new CompositeObjectToTypeConverter<string>(converter1, converter2, converter3);

            var success = compositeConverter.TryConvert("input", out var result);

            success.Should().BeFalse();
            result.Should().Be(default);
            converter1.TryConvertCallCount.Should().Be(1);
            converter2.TryConvertCallCount.Should().Be(1);
            converter3.TryConvertCallCount.Should().Be(1);
        }

        /// <summary>
        /// Tests that TryConvert returns false when no converters are provided.
        /// </summary>
        [TestMethod]
        public void TryConvert_WhenNoConvertersProvided_ShouldReturnFalseWithDefault()
        {
            var compositeConverter = new CompositeObjectToTypeConverter<string>();

            var success = compositeConverter.TryConvert("input", out var result);

            success.Should().BeFalse();
            result.Should().Be(default);
        }

        #endregion

        #region Interface Implementation Tests

        /// <summary>
        /// Tests that CompositeObjectToTypeConverter implements IObjectToTypeConverter.
        /// </summary>
        [TestMethod]
        public void CompositeObjectToTypeConverter_WhenExamined_ShouldImplementIObjectToTypeConverter()
        {
            typeof(CompositeObjectToTypeConverter<string>).Should().BeAssignableTo<IObjectToTypeConverter<string>>();
        }

        /// <summary>
        /// Tests that converter can be used polymorphically.
        /// </summary>
        [TestMethod]
        public void CompositeObjectToTypeConverter_WhenUsedPolymorphically_ShouldWorkCorrectly()
        {
            var innerConverter = new TestObjectToTypeConverter<string> { ShouldSucceed = true, Result = "polymorphic" };
            IObjectToTypeConverter<string> compositeConverter = new CompositeObjectToTypeConverter<string>(innerConverter);

            var success = compositeConverter.TryConvert("input", out var result);

            success.Should().BeTrue();
            result.Should().Be("polymorphic");
        }

        #endregion

        #region Different Type Tests

        /// <summary>
        /// Tests that composite converter works with integer types.
        /// </summary>
        [TestMethod]
        public void TryConvert_WhenUsedWithIntegerTypes_ShouldWorkCorrectly()
        {
            var stringToIntConverter = new StringToIntConverter();
            var objectToIntConverter = new ObjectToIntConverter();
            var compositeConverter = new CompositeObjectToTypeConverter<int>(stringToIntConverter, objectToIntConverter);

            var success1 = compositeConverter.TryConvert("42", out var result1);
            var success2 = compositeConverter.TryConvert(84, out var result2);

            success1.Should().BeTrue();
            result1.Should().Be(42);
            success2.Should().BeTrue();
            result2.Should().Be(84);
        }

        /// <summary>
        /// Tests that composite converter works with custom object types.
        /// </summary>
        [TestMethod]
        public void TryConvert_WhenUsedWithCustomTypes_ShouldWorkCorrectly()
        {
            var stringToPersonConverter = new StringToPersonConverter();
            var intToPersonConverter = new IntToPersonConverter();
            var compositeConverter = new CompositeObjectToTypeConverter<Person>(stringToPersonConverter, intToPersonConverter);

            var success1 = compositeConverter.TryConvert("John Doe", out var result1);
            var success2 = compositeConverter.TryConvert(25, out var result2);

            success1.Should().BeTrue();
            result1.Should().NotBeNull();
            result1.Name.Should().Be("John Doe");

            success2.Should().BeTrue();
            result2.Should().NotBeNull();
            result2.Age.Should().Be(25);
        }

        #endregion

        #region Input Validation Tests

        /// <summary>
        /// Tests that TryConvert handles null inputs correctly.
        /// </summary>
        [TestMethod]
        public void TryConvert_WhenInputIsNull_ShouldHandleCorrectly()
        {
            var converter1 = new TestObjectToTypeConverter<string> { ShouldSucceed = false };
            var converter2 = new TestObjectToTypeConverter<string> { ShouldSucceed = true, Result = "null-handled" };
            var compositeConverter = new CompositeObjectToTypeConverter<string>(converter1, converter2);

            var success = compositeConverter.TryConvert(null, out var result);

            success.Should().BeTrue();
            result.Should().Be("null-handled");
            converter1.LastInput.Should().BeNull();
            converter2.LastInput.Should().BeNull();
        }

        /// <summary>
        /// Tests that TryConvert passes inputs correctly to all converters.
        /// </summary>
        [TestMethod]
        public void TryConvert_WhenCalled_ShouldPassInputToConverters()
        {
            var converter1 = new TestObjectToTypeConverter<string> { ShouldSucceed = false };
            var converter2 = new TestObjectToTypeConverter<string> { ShouldSucceed = false };
            var converter3 = new TestObjectToTypeConverter<string> { ShouldSucceed = true, Result = "success" };
            var compositeConverter = new CompositeObjectToTypeConverter<string>(converter1, converter2, converter3);
            var input = "test input";

            var success = compositeConverter.TryConvert(input, out var result);

            success.Should().BeTrue();
            converter1.LastInput.Should().Be(input);
            converter2.LastInput.Should().Be(input);
            converter3.LastInput.Should().Be(input);
        }

        #endregion

        #region Exception Handling Tests

        /// <summary>
        /// Tests that TryConvert continues to next converter when one throws an exception.
        /// </summary>
        [TestMethod]
        public void TryConvert_WhenConverterThrows_ShouldContinueToNextConverter()
        {
            var throwingConverter = new ThrowingObjectToTypeConverter<string>();
            var successfulConverter = new TestObjectToTypeConverter<string> { ShouldSucceed = true, Result = "recovered" };
            var compositeConverter = new CompositeObjectToTypeConverter<string>(throwingConverter, successfulConverter);

            var success = compositeConverter.TryConvert("input", out var result);

            success.Should().BeTrue();
            result.Should().Be("recovered");
            successfulConverter.TryConvertCallCount.Should().Be(1);
        }

        /// <summary>
        /// Tests that TryConvert returns false when all converters throw exceptions.
        /// </summary>
        [TestMethod]
        public void TryConvert_WhenAllConvertersThrow_ShouldReturnFalse()
        {
            var throwingConverter1 = new ThrowingObjectToTypeConverter<string>();
            var throwingConverter2 = new ThrowingObjectToTypeConverter<string>();
            var compositeConverter = new CompositeObjectToTypeConverter<string>(throwingConverter1, throwingConverter2);

            var success = compositeConverter.TryConvert("input", out var result);

            success.Should().BeFalse();
            result.Should().Be(default);
        }

        #endregion

        #region Performance Tests

        /// <summary>
        /// Tests that composite converter performs well with many converters.
        /// </summary>
        [TestMethod]
        public void TryConvert_WhenUsedWithManyConverters_ShouldPerformWell()
        {
            var converters = new List<IObjectToTypeConverter<string>>();
            
            // Add many failing converters
            for (int i = 0; i < 100; i++)
            {
                converters.Add(new TestObjectToTypeConverter<string> { ShouldSucceed = false });
            }
            
            // Add one successful converter at the end
            converters.Add(new TestObjectToTypeConverter<string> { ShouldSucceed = true, Result = "found" });

            var compositeConverter = new CompositeObjectToTypeConverter<string>(converters);

            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            var success = compositeConverter.TryConvert("input", out var result);
            stopwatch.Stop();

            success.Should().BeTrue();
            result.Should().Be("found");
            stopwatch.ElapsedMilliseconds.Should().BeLessThan(100);
        }

        /// <summary>
        /// Tests performance when first converter always succeeds.
        /// </summary>
        [TestMethod]
        public void TryConvert_WhenFirstConverterSucceeds_ShouldBeVeryFast()
        {
            var fastConverter = new TestObjectToTypeConverter<string> { ShouldSucceed = true, Result = "fast" };
            var slowConverter = new SlowObjectToTypeConverter<string>();
            var compositeConverter = new CompositeObjectToTypeConverter<string>(fastConverter, slowConverter);

            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            var success = compositeConverter.TryConvert("input", out var result);
            stopwatch.Stop();

            success.Should().BeTrue();
            result.Should().Be("fast");
            // Should be fast because it doesn't reach the slow converter
            stopwatch.ElapsedMilliseconds.Should().BeLessThan(10);
            slowConverter.TryConvertCallCount.Should().Be(0);
        }

        #endregion

        #region Realistic Scenario Tests

        /// <summary>
        /// Tests composite converter in a WebJobs message conversion scenario.
        /// </summary>
        [TestMethod]
        public void TryConvert_WhenUsedInWebJobsScenario_ShouldWorkCorrectly()
        {
            // Simulate converting queue message body to a typed object
            var jsonConverter = new JsonStringToOrderConverter();
            var xmlConverter = new XmlStringToOrderConverter();
            var plainTextConverter = new PlainTextToOrderConverter();
            
            var compositeConverter = new CompositeObjectToTypeConverter<Order>(
                jsonConverter, xmlConverter, plainTextConverter);

            // Test JSON input
            var jsonInput = "{\"OrderId\": 123, \"Amount\": 99.99}";
            var jsonSuccess = compositeConverter.TryConvert(jsonInput, out var jsonResult);

            // Test XML input (should fallback to XML converter)
            var xmlInput = "<Order><OrderId>456</OrderId><Amount>199.99</Amount></Order>";
            var xmlSuccess = compositeConverter.TryConvert(xmlInput, out var xmlResult);

            // Test plain text input (should fallback to plain text converter)
            var textInput = "Order 789 for $299.99";
            var textSuccess = compositeConverter.TryConvert(textInput, out var textResult);

            jsonSuccess.Should().BeTrue();
            jsonResult.OrderId.Should().Be(123);
            jsonResult.Amount.Should().Be(99.99);

            xmlSuccess.Should().BeTrue();
            xmlResult.OrderId.Should().Be(456);
            xmlResult.Amount.Should().Be(199.99);

            textSuccess.Should().BeTrue();
            textResult.OrderId.Should().Be(789);
            textResult.Amount.Should().Be(299.99);
        }

        #endregion

        #region Thread Safety Tests

        /// <summary>
        /// Tests that composite converter is safe for concurrent use.
        /// </summary>
        [TestMethod]
        public void TryConvert_WhenUsedConcurrently_ShouldBeSafe()
        {
            var converter1 = new TestObjectToTypeConverter<string> { ShouldSucceed = false };
            var converter2 = new TestObjectToTypeConverter<string> { ShouldSucceed = true, Result = "concurrent" };
            var compositeConverter = new CompositeObjectToTypeConverter<string>(converter1, converter2);

            var tasks = new List<System.Threading.Tasks.Task>();
            var results = new System.Collections.Concurrent.ConcurrentBag<string>();

            for (int i = 0; i < 10; i++)
            {
                int taskId = i;
                tasks.Add(System.Threading.Tasks.Task.Run(() =>
                {
                    var success = compositeConverter.TryConvert($"input-{taskId}", out var result);
                    if (success)
                    {
                        results.Add(result);
                    }
                }));
            }

            System.Threading.Tasks.Task.WaitAll(tasks.ToArray());

            results.Should().HaveCount(10);
            results.Should().OnlyContain(r => r == "concurrent");
        }

        #endregion

    }

    #region Test Helper Classes

    /// <summary>
    /// Test implementation of IObjectToTypeConverter for testing purposes.
    /// </summary>
    internal class TestObjectToTypeConverter<T> : IObjectToTypeConverter<T>
    {
        public bool ShouldSucceed { get; set; } = false;
        public T Result { get; set; } = default;
        public int TryConvertCallCount { get; private set; }
        public object LastInput { get; private set; }

        public bool TryConvert(object input, out T output)
        {
            TryConvertCallCount++;
            LastInput = input;

            if (ShouldSucceed)
            {
                output = Result;
                return true;
            }

            output = default;
            return false;
        }
    }

    /// <summary>
    /// Converter that throws exceptions for testing error handling.
    /// </summary>
    internal class ThrowingObjectToTypeConverter<T> : IObjectToTypeConverter<T>
    {
        public bool TryConvert(object input, out T output)
        {
            throw new InvalidOperationException("Test exception from converter");
        }
    }

    /// <summary>
    /// Slow converter for performance testing.
    /// </summary>
    internal class SlowObjectToTypeConverter<T> : IObjectToTypeConverter<T>
    {
        public int TryConvertCallCount { get; private set; }

        public bool TryConvert(object input, out T output)
        {
            TryConvertCallCount++;
            
            // Simulate slow operation
            System.Threading.Thread.Sleep(100);
            
            output = default;
            return false;
        }
    }

    /// <summary>
    /// String to integer converter for testing.
    /// </summary>
    internal class StringToIntConverter : IObjectToTypeConverter<int>
    {
        public bool TryConvert(object input, out int output)
        {
            if (input is string stringInput && int.TryParse(stringInput, out var result))
            {
                output = result;
                return true;
            }

            output = default;
            return false;
        }
    }

    /// <summary>
    /// Object to integer converter for testing.
    /// </summary>
    internal class ObjectToIntConverter : IObjectToTypeConverter<int>
    {
        public bool TryConvert(object input, out int output)
        {
            try
            {
                output = Convert.ToInt32(input);
                return true;
            }
            catch
            {
                output = default;
                return false;
            }
        }
    }

    /// <summary>
    /// String to Person converter for testing.
    /// </summary>
    internal class StringToPersonConverter : IObjectToTypeConverter<Person>
    {
        public bool TryConvert(object input, out Person output)
        {
            if (input is string name && !string.IsNullOrWhiteSpace(name))
            {
                output = new Person { Name = name };
                return true;
            }

            output = default;
            return false;
        }
    }

    /// <summary>
    /// Integer to Person converter for testing.
    /// </summary>
    internal class IntToPersonConverter : IObjectToTypeConverter<Person>
    {
        public bool TryConvert(object input, out Person output)
        {
            if (input is int age && age > 0)
            {
                output = new Person { Age = age };
                return true;
            }

            output = default;
            return false;
        }
    }

    /// <summary>
    /// JSON string to Order converter for testing.
    /// </summary>
    internal class JsonStringToOrderConverter : IObjectToTypeConverter<Order>
    {
        public bool TryConvert(object input, out Order output)
        {
            if (input is string jsonString && jsonString.TrimStart().StartsWith("{"))
            {
                try
                {
                    output = System.Text.Json.JsonSerializer.Deserialize<Order>(jsonString);
                    return output is not null;
                }
                catch
                {
                    // Fall through to failure
                }
            }

            output = default;
            return false;
        }
    }

    /// <summary>
    /// XML string to Order converter for testing.
    /// </summary>
    internal class XmlStringToOrderConverter : IObjectToTypeConverter<Order>
    {
        public bool TryConvert(object input, out Order output)
        {
            if (input is string xmlString && xmlString.TrimStart().StartsWith("<"))
            {
                try
                {
                    // Simple XML parsing for testing
                    var orderIdStart = xmlString.IndexOf("<OrderId>") + 9;
                    var orderIdEnd = xmlString.IndexOf("</OrderId>");
                    var amountStart = xmlString.IndexOf("<Amount>") + 8;
                    var amountEnd = xmlString.IndexOf("</Amount>");

                    if (orderIdStart > 8 && orderIdEnd > orderIdStart && 
                        amountStart > 7 && amountEnd > amountStart)
                    {
                        var orderIdStr = xmlString.Substring(orderIdStart, orderIdEnd - orderIdStart);
                        var amountStr = xmlString.Substring(amountStart, amountEnd - amountStart);

                        if (int.TryParse(orderIdStr, out var orderId) && 
                            double.TryParse(amountStr, out var amount))
                        {
                            output = new Order { OrderId = orderId, Amount = amount };
                            return true;
                        }
                    }
                }
                catch
                {
                    // Fall through to failure
                }
            }

            output = default;
            return false;
        }
    }

    /// <summary>
    /// Plain text to Order converter for testing.
    /// </summary>
    internal class PlainTextToOrderConverter : IObjectToTypeConverter<Order>
    {
        public bool TryConvert(object input, out Order output)
        {
            if (input is string text && text.Contains("Order"))
            {
                try
                {
                    // Parse "Order 789 for $299.99"
                    var parts = text.Split(' ');
                    if (parts.Length >= 4 && parts[0] == "Order")
                    {
                        if (int.TryParse(parts[1], out var orderId) && 
                            parts[2] == "for" && parts[3].StartsWith("$"))
                        {
                            var amountStr = parts[3].Substring(1);
                            if (double.TryParse(amountStr, out var amount))
                            {
                                output = new Order { OrderId = orderId, Amount = amount };
                                return true;
                            }
                        }
                    }
                }
                catch
                {
                    // Fall through to failure
                }
            }

            output = default;
            return false;
        }
    }

    #endregion

}
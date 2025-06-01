// Copyright (c) CloudNimble, Inc. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using CloudNimble.WebJobs.Extensions.Common.Converters;
using CloudNimble.WebJobs.Extensions.Tests.Common.Models;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CloudNimble.WebJobs.Extensions.Tests.Common.Converters
{

    /// <summary>
    /// Tests for the <see cref="CompositeAsyncObjectToTypeConverter{T}"/> class.
    /// </summary>
    [TestClass]
    [TestCategory("Unit")]
    public class CompositeAsyncObjectToTypeConverterTests
    {

        #region Constructor Tests

        /// <summary>
        /// Tests that the constructor correctly initializes with a collection of converters.
        /// </summary>
        [TestMethod]
        public void Constructor_WhenCalledWithConverterCollection_ShouldInitializeCorrectly()
        {
            var converters = new List<IAsyncObjectToTypeConverter<string>>
            {
                new TestAsyncObjectToTypeConverter<string>(),
                new TestAsyncObjectToTypeConverter<string>()
            };

            var compositeConverter = new CompositeAsyncObjectToTypeConverter<string>(converters);

            compositeConverter.Should().NotBeNull();
        }

        /// <summary>
        /// Tests that the constructor correctly initializes with a params array of converters.
        /// </summary>
        [TestMethod]
        public void Constructor_WhenCalledWithConverterArray_ShouldInitializeCorrectly()
        {
            var converter1 = new TestAsyncObjectToTypeConverter<string>();
            var converter2 = new TestAsyncObjectToTypeConverter<string>();

            var compositeConverter = new CompositeAsyncObjectToTypeConverter<string>(converter1, converter2);

            compositeConverter.Should().NotBeNull();
        }

        /// <summary>
        /// Tests that the constructor accepts an empty collection of converters.
        /// </summary>
        [TestMethod]
        public void Constructor_WhenCalledWithEmptyCollection_ShouldAcceptEmptyCollection()
        {
            var emptyConverters = new List<IAsyncObjectToTypeConverter<string>>();

            var compositeConverter = new CompositeAsyncObjectToTypeConverter<string>(emptyConverters);

            compositeConverter.Should().NotBeNull();
        }

        #endregion

        #region TryConvertAsync Success Tests

        /// <summary>
        /// Tests that TryConvertAsync succeeds when first converter succeeds.
        /// </summary>
        [TestMethod]
        public async Task TryConvertAsync_WhenFirstConverterSucceeds_ShouldReturnSuccessResult()
        {
            var converter1 = new TestAsyncObjectToTypeConverter<string> { ShouldSucceed = true, Result = "first" };
            var converter2 = new TestAsyncObjectToTypeConverter<string> { ShouldSucceed = true, Result = "second" };
            var compositeConverter = new CompositeAsyncObjectToTypeConverter<string>(converter1, converter2);

            var result = await compositeConverter.TryConvertAsync("input", CancellationToken.None);

            result.Succeeded.Should().BeTrue();
            result.Result.Should().Be("first");
            converter1.TryConvertCallCount.Should().Be(1);
            converter2.TryConvertCallCount.Should().Be(0); // Should not call second converter
        }

        /// <summary>
        /// Tests that TryConvertAsync succeeds when second converter succeeds after first fails.
        /// </summary>
        [TestMethod]
        public async Task TryConvertAsync_WhenSecondConverterSucceeds_ShouldReturnSuccessResult()
        {
            var converter1 = new TestAsyncObjectToTypeConverter<string> { ShouldSucceed = false };
            var converter2 = new TestAsyncObjectToTypeConverter<string> { ShouldSucceed = true, Result = "second" };
            var converter3 = new TestAsyncObjectToTypeConverter<string> { ShouldSucceed = true, Result = "third" };
            var compositeConverter = new CompositeAsyncObjectToTypeConverter<string>(converter1, converter2, converter3);

            var result = await compositeConverter.TryConvertAsync("input", CancellationToken.None);

            result.Succeeded.Should().BeTrue();
            result.Result.Should().Be("second");
            converter1.TryConvertCallCount.Should().Be(1);
            converter2.TryConvertCallCount.Should().Be(1);
            converter3.TryConvertCallCount.Should().Be(0); // Should not call third converter
        }

        /// <summary>
        /// Tests that TryConvertAsync succeeds when last converter succeeds.
        /// </summary>
        [TestMethod]
        public async Task TryConvertAsync_WhenLastConverterSucceeds_ShouldReturnSuccessResult()
        {
            var converter1 = new TestAsyncObjectToTypeConverter<string> { ShouldSucceed = false };
            var converter2 = new TestAsyncObjectToTypeConverter<string> { ShouldSucceed = false };
            var converter3 = new TestAsyncObjectToTypeConverter<string> { ShouldSucceed = true, Result = "third" };
            var compositeConverter = new CompositeAsyncObjectToTypeConverter<string>(converter1, converter2, converter3);

            var result = await compositeConverter.TryConvertAsync("input", CancellationToken.None);

            result.Succeeded.Should().BeTrue();
            result.Result.Should().Be("third");
            converter1.TryConvertCallCount.Should().Be(1);
            converter2.TryConvertCallCount.Should().Be(1);
            converter3.TryConvertCallCount.Should().Be(1);
        }

        #endregion

        #region TryConvertAsync Failure Tests

        /// <summary>
        /// Tests that TryConvertAsync returns failure when all converters fail.
        /// </summary>
        [TestMethod]
        public async Task TryConvertAsync_WhenAllConvertersFail_ShouldReturnFailureResult()
        {
            var converter1 = new TestAsyncObjectToTypeConverter<string> { ShouldSucceed = false };
            var converter2 = new TestAsyncObjectToTypeConverter<string> { ShouldSucceed = false };
            var converter3 = new TestAsyncObjectToTypeConverter<string> { ShouldSucceed = false };
            var compositeConverter = new CompositeAsyncObjectToTypeConverter<string>(converter1, converter2, converter3);

            var result = await compositeConverter.TryConvertAsync("input", CancellationToken.None);

            result.Succeeded.Should().BeFalse();
            result.Result.Should().Be(default);
            converter1.TryConvertCallCount.Should().Be(1);
            converter2.TryConvertCallCount.Should().Be(1);
            converter3.TryConvertCallCount.Should().Be(1);
        }

        /// <summary>
        /// Tests that TryConvertAsync returns failure when no converters are provided.
        /// </summary>
        [TestMethod]
        public async Task TryConvertAsync_WhenNoConvertersProvided_ShouldReturnFailureResult()
        {
            var compositeConverter = new CompositeAsyncObjectToTypeConverter<string>();

            var result = await compositeConverter.TryConvertAsync("input", CancellationToken.None);

            result.Succeeded.Should().BeFalse();
            result.Result.Should().Be(default);
        }

        #endregion

        #region Cancellation Tests

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
        /// Tests that TryConvertAsync passes cancellation token to converters.
        /// </summary>
        [TestMethod]
        public async Task TryConvertAsync_WhenCalled_ShouldPassCancellationTokenToConverters()
        {
            var converter1 = new TestAsyncObjectToTypeConverter<string> { ShouldSucceed = false };
            var converter2 = new TestAsyncObjectToTypeConverter<string> { ShouldSucceed = true, Result = "success" };
            var compositeConverter = new CompositeAsyncObjectToTypeConverter<string>(converter1, converter2);

            using var cts = new CancellationTokenSource();

            var result = await compositeConverter.TryConvertAsync("input", cts.Token);

            result.Succeeded.Should().BeTrue();
            converter1.LastCancellationToken.Should().Be(cts.Token);
            converter2.LastCancellationToken.Should().Be(cts.Token);
        }

        #endregion

        #region Interface Implementation Tests

        /// <summary>
        /// Tests that CompositeAsyncObjectToTypeConverter implements IAsyncObjectToTypeConverter.
        /// </summary>
        [TestMethod]
        public void CompositeAsyncObjectToTypeConverter_WhenExamined_ShouldImplementIAsyncObjectToTypeConverter()
        {
            typeof(CompositeAsyncObjectToTypeConverter<string>).Should().BeAssignableTo<IAsyncObjectToTypeConverter<string>>();
        }

        /// <summary>
        /// Tests that converter can be used polymorphically.
        /// </summary>
        [TestMethod]
        public async Task CompositeAsyncObjectToTypeConverter_WhenUsedPolymorphically_ShouldWorkCorrectly()
        {
            var innerConverter = new TestAsyncObjectToTypeConverter<string> { ShouldSucceed = true, Result = "polymorphic" };
            IAsyncObjectToTypeConverter<string> compositeConverter = new CompositeAsyncObjectToTypeConverter<string>(innerConverter);

            var result = await compositeConverter.TryConvertAsync("input", CancellationToken.None);

            result.Succeeded.Should().BeTrue();
            result.Result.Should().Be("polymorphic");
        }

        #endregion

        #region Different Type Tests

        /// <summary>
        /// Tests that composite converter works with integer types.
        /// </summary>
        [TestMethod]
        public async Task TryConvertAsync_WhenUsedWithIntegerTypes_ShouldWorkCorrectly()
        {
            var stringToIntConverter = new AsyncStringToIntConverter();
            var objectToIntConverter = new AsyncObjectToIntConverter();
            var compositeConverter = new CompositeAsyncObjectToTypeConverter<int>(stringToIntConverter, objectToIntConverter);

            var result1 = await compositeConverter.TryConvertAsync("42", CancellationToken.None);
            var result2 = await compositeConverter.TryConvertAsync(84, CancellationToken.None);

            result1.Succeeded.Should().BeTrue();
            result1.Result.Should().Be(42);
            result2.Succeeded.Should().BeTrue();
            result2.Result.Should().Be(84);
        }

        /// <summary>
        /// Tests that composite converter works with custom object types.
        /// </summary>
        [TestMethod]
        public async Task TryConvertAsync_WhenUsedWithCustomTypes_ShouldWorkCorrectly()
        {
            var stringToPersonConverter = new AsyncStringToPersonConverter();
            var intToPersonConverter = new AsyncIntToPersonConverter();
            var compositeConverter = new CompositeAsyncObjectToTypeConverter<Person>(stringToPersonConverter, intToPersonConverter);

            var result1 = await compositeConverter.TryConvertAsync("Jane Doe", CancellationToken.None);
            var result2 = await compositeConverter.TryConvertAsync(30, CancellationToken.None);

            result1.Succeeded.Should().BeTrue();
            result1.Result.Should().NotBeNull();
            result1.Result.Name.Should().Be("Jane Doe");

            result2.Succeeded.Should().BeTrue();
            result2.Result.Should().NotBeNull();
            result2.Result.Age.Should().Be(30);
        }

        #endregion

        #region Input Validation Tests

        /// <summary>
        /// Tests that TryConvertAsync handles null inputs correctly.
        /// </summary>
        [TestMethod]
        public async Task TryConvertAsync_WhenInputIsNull_ShouldHandleCorrectly()
        {
            var converter1 = new TestAsyncObjectToTypeConverter<string> { ShouldSucceed = false };
            var converter2 = new TestAsyncObjectToTypeConverter<string> { ShouldSucceed = true, Result = "null-handled" };
            var compositeConverter = new CompositeAsyncObjectToTypeConverter<string>(converter1, converter2);

            var result = await compositeConverter.TryConvertAsync(null, CancellationToken.None);

            result.Succeeded.Should().BeTrue();
            result.Result.Should().Be("null-handled");
            converter1.LastInput.Should().BeNull();
            converter2.LastInput.Should().BeNull();
        }

        /// <summary>
        /// Tests that TryConvertAsync passes inputs correctly to all converters.
        /// </summary>
        [TestMethod]
        public async Task TryConvertAsync_WhenCalled_ShouldPassInputToConverters()
        {
            var converter1 = new TestAsyncObjectToTypeConverter<string> { ShouldSucceed = false };
            var converter2 = new TestAsyncObjectToTypeConverter<string> { ShouldSucceed = false };
            var converter3 = new TestAsyncObjectToTypeConverter<string> { ShouldSucceed = true, Result = "success" };
            var compositeConverter = new CompositeAsyncObjectToTypeConverter<string>(converter1, converter2, converter3);
            var input = "test input";

            var result = await compositeConverter.TryConvertAsync(input, CancellationToken.None);

            result.Succeeded.Should().BeTrue();
            converter1.LastInput.Should().Be(input);
            converter2.LastInput.Should().Be(input);
            converter3.LastInput.Should().Be(input);
        }

        #endregion

        #region Exception Handling Tests

        /// <summary>
        /// Tests that TryConvertAsync continues to next converter when one throws an exception.
        /// </summary>
        [TestMethod]
        public async Task TryConvertAsync_WhenConverterThrows_ShouldContinueToNextConverter()
        {
            var throwingConverter = new ThrowingAsyncObjectToTypeConverter<string>();
            var successfulConverter = new TestAsyncObjectToTypeConverter<string> { ShouldSucceed = true, Result = "recovered" };
            var compositeConverter = new CompositeAsyncObjectToTypeConverter<string>(throwingConverter, successfulConverter);

            var result = await compositeConverter.TryConvertAsync("input", CancellationToken.None);

            result.Succeeded.Should().BeTrue();
            result.Result.Should().Be("recovered");
            successfulConverter.TryConvertCallCount.Should().Be(1);
        }

        /// <summary>
        /// Tests that TryConvertAsync returns failure when all converters throw exceptions.
        /// </summary>
        [TestMethod]
        public async Task TryConvertAsync_WhenAllConvertersThrow_ShouldReturnFailure()
        {
            var throwingConverter1 = new ThrowingAsyncObjectToTypeConverter<string>();
            var throwingConverter2 = new ThrowingAsyncObjectToTypeConverter<string>();
            var compositeConverter = new CompositeAsyncObjectToTypeConverter<string>(throwingConverter1, throwingConverter2);

            var result = await compositeConverter.TryConvertAsync("input", CancellationToken.None);

            result.Succeeded.Should().BeFalse();
            result.Result.Should().Be(default);
        }

        #endregion

        #region Performance Tests

        /// <summary>
        /// Tests that composite converter performs well with many converters.
        /// </summary>
        [TestMethod]
        public async Task TryConvertAsync_WhenUsedWithManyConverters_ShouldPerformWell()
        {
            var converters = new List<IAsyncObjectToTypeConverter<string>>();
            
            // Add many failing converters
            for (int i = 0; i < 50; i++)
            {
                converters.Add(new TestAsyncObjectToTypeConverter<string> { ShouldSucceed = false });
            }
            
            // Add one successful converter at the end
            converters.Add(new TestAsyncObjectToTypeConverter<string> { ShouldSucceed = true, Result = "found" });

            var compositeConverter = new CompositeAsyncObjectToTypeConverter<string>(converters);

            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            var result = await compositeConverter.TryConvertAsync("input", CancellationToken.None);
            stopwatch.Stop();

            result.Succeeded.Should().BeTrue();
            result.Result.Should().Be("found");
            stopwatch.ElapsedMilliseconds.Should().BeLessThan(500);
        }

        /// <summary>
        /// Tests performance when first converter always succeeds.
        /// </summary>
        [TestMethod]
        public async Task TryConvertAsync_WhenFirstConverterSucceeds_ShouldBeVeryFast()
        {
            var fastConverter = new TestAsyncObjectToTypeConverter<string> { ShouldSucceed = true, Result = "fast" };
            var slowConverter = new DelayedAsyncConverter<string> { DelayMs = 1000, ShouldSucceed = true, Result = "slow" };
            var compositeConverter = new CompositeAsyncObjectToTypeConverter<string>(fastConverter, slowConverter);

            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            var result = await compositeConverter.TryConvertAsync("input", CancellationToken.None);
            stopwatch.Stop();

            result.Succeeded.Should().BeTrue();
            result.Result.Should().Be("fast");
            // Should be fast because it doesn't reach the slow converter
            stopwatch.ElapsedMilliseconds.Should().BeLessThan(100);
            slowConverter.TryConvertCallCount.Should().Be(0);
        }

        #endregion

        #region Concurrent Usage Tests

        /// <summary>
        /// Tests that composite converter can be used safely from multiple threads.
        /// </summary>
        [TestMethod]
        public async Task TryConvertAsync_WhenUsedConcurrently_ShouldBeSafe()
        {
            var converter1 = new TestAsyncObjectToTypeConverter<string> { ShouldSucceed = false };
            var converter2 = new TestAsyncObjectToTypeConverter<string> { ShouldSucceed = true, Result = "concurrent" };
            var compositeConverter = new CompositeAsyncObjectToTypeConverter<string>(converter1, converter2);

            var tasks = new List<Task<ConversionResult<string>>>();

            // Create multiple concurrent conversion tasks
            for (int i = 0; i < 10; i++)
            {
                int value = i;
                tasks.Add(compositeConverter.TryConvertAsync($"input-{value}", CancellationToken.None));
            }

            var results = await Task.WhenAll(tasks);

            results.Should().HaveCount(10);
            results.Should().OnlyContain(r => r.Succeeded && r.Result == "concurrent");
        }

        #endregion

        #region Realistic Scenario Tests

        /// <summary>
        /// Tests composite converter in a WebJobs message conversion scenario.
        /// </summary>
        [TestMethod]
        public async Task TryConvertAsync_WhenUsedInWebJobsScenario_ShouldWorkCorrectly()
        {
            // Simulate converting queue message body to a typed object with async operations
            var jsonConverter = new AsyncJsonStringToOrderConverter();
            var xmlConverter = new AsyncXmlStringToOrderConverter();
            var apiConverter = new AsyncApiLookupConverter(); // Simulates external API call
            
            var compositeConverter = new CompositeAsyncObjectToTypeConverter<Order>(
                jsonConverter, xmlConverter, apiConverter);

            // Test JSON input
            var jsonInput = "{\"OrderId\": 123, \"Amount\": 99.99}";
            var jsonResult = await compositeConverter.TryConvertAsync(jsonInput, CancellationToken.None);

            // Test XML input (should fallback to XML converter)
            var xmlInput = "<Order><OrderId>456</OrderId><Amount>199.99</Amount></Order>";
            var xmlResult = await compositeConverter.TryConvertAsync(xmlInput, CancellationToken.None);

            // Test API lookup input (should fallback to API converter)
            var apiInput = "ORDER_REF_789";
            var apiResult = await compositeConverter.TryConvertAsync(apiInput, CancellationToken.None);

            jsonResult.Succeeded.Should().BeTrue();
            jsonResult.Result.OrderId.Should().Be(123);
            jsonResult.Result.Amount.Should().Be(99.99);

            xmlResult.Succeeded.Should().BeTrue();
            xmlResult.Result.OrderId.Should().Be(456);
            xmlResult.Result.Amount.Should().Be(199.99);

            apiResult.Succeeded.Should().BeTrue();
            apiResult.Result.OrderId.Should().Be(789);
            apiResult.Result.Amount.Should().Be(999.99); // From API lookup
        }

        /// <summary>
        /// Tests that async operations are properly awaited in sequence.
        /// </summary>
        [TestMethod]
        public async Task TryConvertAsync_WhenConvertersAreAsync_ShouldAwaitInSequence()
        {
            var converter1 = new DelayedAsyncConverter<string> { DelayMs = 50, ShouldSucceed = false };
            var converter2 = new DelayedAsyncConverter<string> { DelayMs = 50, ShouldSucceed = false };
            var converter3 = new DelayedAsyncConverter<string> { DelayMs = 50, ShouldSucceed = true, Result = "sequential" };
            var compositeConverter = new CompositeAsyncObjectToTypeConverter<string>(converter1, converter2, converter3);

            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            var result = await compositeConverter.TryConvertAsync("input", CancellationToken.None);
            stopwatch.Stop();

            result.Succeeded.Should().BeTrue();
            result.Result.Should().Be("sequential");
            // Should take approximately 150ms (3 x 50ms) since operations are sequential
            stopwatch.ElapsedMilliseconds.Should().BeGreaterThan(120);
            stopwatch.ElapsedMilliseconds.Should().BeLessThan(300);
        }

        #endregion

    }

    #region Test Helper Classes

    /// <summary>
    /// Test implementation of IAsyncObjectToTypeConverter for testing purposes.
    /// </summary>
    internal class TestAsyncObjectToTypeConverter<T> : IAsyncObjectToTypeConverter<T>
    {
        public bool ShouldSucceed { get; set; } = false;
        public T Result { get; set; } = default;
        public int TryConvertCallCount { get; private set; }
        public object LastInput { get; private set; }
        public CancellationToken LastCancellationToken { get; private set; }

        public Task<ConversionResult<T>> TryConvertAsync(object input, CancellationToken cancellationToken)
        {
            TryConvertCallCount++;
            LastInput = input;
            LastCancellationToken = cancellationToken;

            var result = new ConversionResult<T>
            {
                Succeeded = ShouldSucceed,
                Result = ShouldSucceed ? Result : default
            };

            return Task.FromResult(result);
        }
    }

    /// <summary>
    /// Converter that throws exceptions for testing error handling.
    /// </summary>
    internal class ThrowingAsyncObjectToTypeConverter<T> : IAsyncObjectToTypeConverter<T>
    {
        public Task<ConversionResult<T>> TryConvertAsync(object input, CancellationToken cancellationToken)
        {
            throw new InvalidOperationException("Test exception from async converter");
        }
    }

    /// <summary>
    /// Delayed converter for performance and cancellation testing.
    /// </summary>
    internal class DelayedAsyncConverter<T> : IAsyncObjectToTypeConverter<T>
    {
        public int DelayMs { get; set; } = 100;
        public bool ShouldSucceed { get; set; } = false;
        public T Result { get; set; } = default;
        public int TryConvertCallCount { get; private set; }

        public async Task<ConversionResult<T>> TryConvertAsync(object input, CancellationToken cancellationToken)
        {
            TryConvertCallCount++;
            
            await Task.Delay(DelayMs, cancellationToken);

            return new ConversionResult<T>
            {
                Succeeded = ShouldSucceed,
                Result = ShouldSucceed ? Result : default
            };
        }
    }

    /// <summary>
    /// Async string to integer converter for testing.
    /// </summary>
    internal class AsyncStringToIntConverter : IAsyncObjectToTypeConverter<int>
    {
        public async Task<ConversionResult<int>> TryConvertAsync(object input, CancellationToken cancellationToken)
        {
            await Task.Delay(1, cancellationToken); // Simulate async operation

            if (input is string stringInput && int.TryParse(stringInput, out var result))
            {
                return new ConversionResult<int> { Succeeded = true, Result = result };
            }

            return new ConversionResult<int> { Succeeded = false, Result = default };
        }
    }

    /// <summary>
    /// Async object to integer converter for testing.
    /// </summary>
    internal class AsyncObjectToIntConverter : IAsyncObjectToTypeConverter<int>
    {
        public async Task<ConversionResult<int>> TryConvertAsync(object input, CancellationToken cancellationToken)
        {
            await Task.Delay(1, cancellationToken); // Simulate async operation

            try
            {
                var result = Convert.ToInt32(input);
                return new ConversionResult<int> { Succeeded = true, Result = result };
            }
            catch
            {
                return new ConversionResult<int> { Succeeded = false, Result = default };
            }
        }
    }

    /// <summary>
    /// Async string to Person converter for testing.
    /// </summary>
    internal class AsyncStringToPersonConverter : IAsyncObjectToTypeConverter<Person>
    {
        public async Task<ConversionResult<Person>> TryConvertAsync(object input, CancellationToken cancellationToken)
        {
            await Task.Delay(1, cancellationToken); // Simulate async operation

            if (input is string name && !string.IsNullOrWhiteSpace(name))
            {
                return new ConversionResult<Person> 
                { 
                    Succeeded = true, 
                    Result = new Person { Name = name } 
                };
            }

            return new ConversionResult<Person> { Succeeded = false, Result = default };
        }
    }

    /// <summary>
    /// Async integer to Person converter for testing.
    /// </summary>
    internal class AsyncIntToPersonConverter : IAsyncObjectToTypeConverter<Person>
    {
        public async Task<ConversionResult<Person>> TryConvertAsync(object input, CancellationToken cancellationToken)
        {
            await Task.Delay(1, cancellationToken); // Simulate async operation

            if (input is int age && age > 0)
            {
                return new ConversionResult<Person> 
                { 
                    Succeeded = true, 
                    Result = new Person { Age = age } 
                };
            }

            return new ConversionResult<Person> { Succeeded = false, Result = default };
        }
    }

    /// <summary>
    /// Async JSON string to Order converter for testing.
    /// </summary>
    internal class AsyncJsonStringToOrderConverter : IAsyncObjectToTypeConverter<Order>
    {
        public async Task<ConversionResult<Order>> TryConvertAsync(object input, CancellationToken cancellationToken)
        {
            await Task.Delay(10, cancellationToken); // Simulate async JSON parsing

            if (input is string jsonString && jsonString.TrimStart().StartsWith("{"))
            {
                try
                {
                    var order = System.Text.Json.JsonSerializer.Deserialize<Order>(jsonString);
                    if (order is not null)
                    {
                        return new ConversionResult<Order> { Succeeded = true, Result = order };
                    }
                }
                catch
                {
                    // Fall through to failure
                }
            }

            return new ConversionResult<Order> { Succeeded = false, Result = default };
        }
    }

    /// <summary>
    /// Async XML string to Order converter for testing.
    /// </summary>
    internal class AsyncXmlStringToOrderConverter : IAsyncObjectToTypeConverter<Order>
    {
        public async Task<ConversionResult<Order>> TryConvertAsync(object input, CancellationToken cancellationToken)
        {
            await Task.Delay(15, cancellationToken); // Simulate async XML parsing

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
                            var order = new Order { OrderId = orderId, Amount = amount };
                            return new ConversionResult<Order> { Succeeded = true, Result = order };
                        }
                    }
                }
                catch
                {
                    // Fall through to failure
                }
            }

            return new ConversionResult<Order> { Succeeded = false, Result = default };
        }
    }

    /// <summary>
    /// Async API lookup converter for testing external service calls.
    /// </summary>
    internal class AsyncApiLookupConverter : IAsyncObjectToTypeConverter<Order>
    {
        public async Task<ConversionResult<Order>> TryConvertAsync(object input, CancellationToken cancellationToken)
        {
            await Task.Delay(50, cancellationToken); // Simulate API call delay

            if (input is string reference && reference.StartsWith("ORDER_REF_"))
            {
                try
                {
                    // Simulate API lookup
                    var orderIdStr = reference.Substring("ORDER_REF_".Length);
                    if (int.TryParse(orderIdStr, out var orderId))
                    {
                        // Simulate API response
                        var order = new Order 
                        { 
                            OrderId = orderId, 
                            Amount = 999.99 // Fixed amount from "API"
                        };
                        
                        return new ConversionResult<Order> { Succeeded = true, Result = order };
                    }
                }
                catch
                {
                    // Fall through to failure
                }
            }

            return new ConversionResult<Order> { Succeeded = false, Result = default };
        }
    }

    #endregion

}
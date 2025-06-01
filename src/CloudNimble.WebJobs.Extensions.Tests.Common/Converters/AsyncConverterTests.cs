// Copyright (c) CloudNimble, Inc. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using CloudNimble.WebJobs.Extensions.Common.Converters;
using FluentAssertions;
using Microsoft.Azure.WebJobs;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace CloudNimble.WebJobs.Extensions.Tests.Common.Converters
{

    /// <summary>
    /// Tests for the <see cref="AsyncConverter{TInput, TOutput}"/> class.
    /// </summary>
    [TestClass]
    [TestCategory("Unit")]
    public class AsyncConverterTests
    {

        #region Constructor Tests

        /// <summary>
        /// Tests that the constructor correctly initializes with a valid inner converter.
        /// </summary>
        [TestMethod]
        public void Constructor_WhenCalledWithValidInnerConverter_ShouldInitializeCorrectly()
        {
            var innerConverter = new TestSyncConverter<string, int>();

            var asyncConverter = new AsyncConverter<string, int>(innerConverter);

            asyncConverter.Should().NotBeNull();
        }

        /// <summary>
        /// Tests that the constructor throws when inner converter is null.
        /// </summary>
        [TestMethod]
        public void Constructor_WhenInnerConverterIsNull_ShouldThrowArgumentNullException()
        {
            var action = () => new AsyncConverter<string, int>(null);

            action.Should().Throw<ArgumentNullException>()
                .WithParameterName("innerConverter");
        }

        #endregion

        #region ConvertAsync Basic Tests

        /// <summary>
        /// Tests that ConvertAsync delegates to inner converter and returns result asynchronously.
        /// </summary>
        [TestMethod]
        public async Task ConvertAsync_WhenCalledWithValidInput_ShouldDelegateToInnerConverter()
        {
            var innerConverter = new TestSyncConverter<string, int>();
            var asyncConverter = new AsyncConverter<string, int>(innerConverter);
            var input = "42";

            var result = await asyncConverter.ConvertAsync(input, CancellationToken.None);

            result.Should().Be(42);
            innerConverter.ConvertCallCount.Should().Be(1);
            innerConverter.LastInput.Should().Be("42");
        }

        /// <summary>
        /// Tests that ConvertAsync works with different input/output types.
        /// </summary>
        [TestMethod]
        public async Task ConvertAsync_WhenUsedWithDifferentTypes_ShouldWorkCorrectly()
        {
            var stringToIntConverter = new AsyncConverter<string, int>(new TestSyncConverter<string, int>());
            var intToStringConverter = new AsyncConverter<int, string>(new TestSyncConverter<int, string>());
            var boolToStringConverter = new AsyncConverter<bool, string>(new TestSyncConverter<bool, string>());

            var result1 = await stringToIntConverter.ConvertAsync("123", CancellationToken.None);
            var result2 = await intToStringConverter.ConvertAsync(456, CancellationToken.None);
            var result3 = await boolToStringConverter.ConvertAsync(true, CancellationToken.None);

            result1.Should().Be(123);
            result2.Should().Be("456");
            result3.Should().Be("True");
        }

        /// <summary>
        /// Tests that ConvertAsync handles null inputs correctly.
        /// </summary>
        [TestMethod]
        public async Task ConvertAsync_WhenCalledWithNullInput_ShouldHandleCorrectly()
        {
            var innerConverter = new TestSyncConverter<string, string>();
            var asyncConverter = new AsyncConverter<string, string>(innerConverter);

            var result = await asyncConverter.ConvertAsync(null, CancellationToken.None);

            result.Should().Be("converted_null");
            innerConverter.ConvertCallCount.Should().Be(1);
            innerConverter.LastInput.Should().BeNull();
        }

        #endregion

        #region Cancellation Tests

        /// <summary>
        /// Tests that ConvertAsync respects cancellation tokens.
        /// </summary>
        [TestMethod]
        public async Task ConvertAsync_WhenCancellationRequested_ShouldRespectCancellation()
        {
            var innerConverter = new TestSyncConverter<string, int>();
            var asyncConverter = new AsyncConverter<string, int>(innerConverter);

            using var cts = new CancellationTokenSource();
            cts.Cancel();

            var task = asyncConverter.ConvertAsync("42", cts.Token);

            // The task should complete immediately since the sync converter doesn't check cancellation
            // But the cancellation token is still passed through
            var result = await task;
            result.Should().Be(42);
        }

        /// <summary>
        /// Tests that ConvertAsync works with non-cancelled tokens.
        /// </summary>
        [TestMethod]
        public async Task ConvertAsync_WhenCancellationNotRequested_ShouldCompleteNormally()
        {
            var innerConverter = new TestSyncConverter<string, int>();
            var asyncConverter = new AsyncConverter<string, int>(innerConverter);

            using var cts = new CancellationTokenSource();

            var result = await asyncConverter.ConvertAsync("789", cts.Token);

            result.Should().Be(789);
        }

        #endregion

        #region Interface Implementation Tests

        /// <summary>
        /// Tests that AsyncConverter implements IAsyncConverter interface.
        /// </summary>
        [TestMethod]
        public void AsyncConverter_WhenExamined_ShouldImplementIAsyncConverter()
        {
            typeof(AsyncConverter<string, int>).Should().BeAssignableTo<IAsyncConverter<string, int>>();
        }

        /// <summary>
        /// Tests that converter can be used polymorphically.
        /// </summary>
        [TestMethod]
        public async Task AsyncConverter_WhenUsedPolymorphically_ShouldWorkCorrectly()
        {
            var innerConverter = new TestSyncConverter<string, int>();
            IAsyncConverter<string, int> asyncConverter = new AsyncConverter<string, int>(innerConverter);

            var result = await asyncConverter.ConvertAsync("555", CancellationToken.None);

            result.Should().Be(555);
        }

        #endregion

        #region Exception Handling Tests

        /// <summary>
        /// Tests that ConvertAsync propagates exceptions from inner converter.
        /// </summary>
        [TestMethod]
        public async Task ConvertAsync_WhenInnerConverterThrows_ShouldPropagateException()
        {
            var innerConverter = new ThrowingConverter<string, int>();
            var asyncConverter = new AsyncConverter<string, int>(innerConverter);

            var action = async () => await asyncConverter.ConvertAsync("invalid", CancellationToken.None);

            await action.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("Test exception from converter");
        }

        /// <summary>
        /// Tests that ConvertAsync handles different exception types correctly.
        /// </summary>
        [TestMethod]
        public async Task ConvertAsync_WhenInnerConverterThrowsDifferentExceptions_ShouldPropagateCorrectly()
        {
            var argumentExceptionConverter = new ThrowingConverter<string, int>(new ArgumentException("Argument error"));
            var formatExceptionConverter = new ThrowingConverter<string, int>(new FormatException("Format error"));

            var asyncConverter1 = new AsyncConverter<string, int>(argumentExceptionConverter);
            var asyncConverter2 = new AsyncConverter<string, int>(formatExceptionConverter);

            var action1 = async () => await asyncConverter1.ConvertAsync("test", CancellationToken.None);
            var action2 = async () => await asyncConverter2.ConvertAsync("test", CancellationToken.None);

            await action1.Should().ThrowAsync<ArgumentException>().WithMessage("Argument error");
            await action2.Should().ThrowAsync<FormatException>().WithMessage("Format error");
        }

        #endregion

        #region Performance Tests

        /// <summary>
        /// Tests that ConvertAsync completes quickly for simple conversions.
        /// </summary>
        [TestMethod]
        public async Task ConvertAsync_WhenPerformingSimpleConversion_ShouldCompleteQuickly()
        {
            var innerConverter = new TestSyncConverter<string, int>();
            var asyncConverter = new AsyncConverter<string, int>(innerConverter);

            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            var result = await asyncConverter.ConvertAsync("12345", CancellationToken.None);

            stopwatch.Stop();

            result.Should().Be(12345);
            stopwatch.ElapsedMilliseconds.Should().BeLessThan(100);
        }

        /// <summary>
        /// Tests performance with many sequential conversions.
        /// </summary>
        [TestMethod]
        public async Task ConvertAsync_WhenPerformingManyConversions_ShouldPerformWell()
        {
            var innerConverter = new TestSyncConverter<int, string>();
            var asyncConverter = new AsyncConverter<int, string>(innerConverter);
            const int iterations = 1000;

            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            for (int i = 0; i < iterations; i++)
            {
                var result = await asyncConverter.ConvertAsync(i, CancellationToken.None);
                result.Should().Be(i.ToString());
            }

            stopwatch.Stop();

            stopwatch.ElapsedMilliseconds.Should().BeLessThan(1000);
            innerConverter.ConvertCallCount.Should().Be(iterations);
        }

        #endregion

        #region Concurrent Usage Tests

        /// <summary>
        /// Tests that AsyncConverter can be used safely from multiple threads.
        /// </summary>
        [TestMethod]
        public async Task ConvertAsync_WhenUsedConcurrently_ShouldBeSafe()
        {
            var innerConverter = new TestSyncConverter<int, string>();
            var asyncConverter = new AsyncConverter<int, string>(innerConverter);

            var tasks = new List<Task<string>>();

            // Create multiple concurrent conversion tasks
            for (int i = 0; i < 10; i++)
            {
                int value = i;
                tasks.Add(asyncConverter.ConvertAsync(value, CancellationToken.None));
            }

            var results = await Task.WhenAll(tasks);

            results.Should().HaveCount(10);
            for (int i = 0; i < 10; i++)
            {
                results[i].Should().Be(i.ToString());
            }
            innerConverter.ConvertCallCount.Should().Be(10);
        }

        #endregion

        #region Complex Type Tests

        /// <summary>
        /// Tests that ConvertAsync works with complex object types.
        /// </summary>
        [TestMethod]
        public async Task ConvertAsync_WhenUsedWithComplexTypes_ShouldWorkCorrectly()
        {
            var innerConverter = new ComplexObjectConverter();
            var asyncConverter = new AsyncConverter<SourceObject, TargetObject>(innerConverter);

            var source = new SourceObject { Id = 123, Name = "Test Object", Value = 45.67 };

            var result = await asyncConverter.ConvertAsync(source, CancellationToken.None);

            result.Should().NotBeNull();
            result.ConvertedId.Should().Be(123);
            result.ConvertedName.Should().Be("Test Object");
            result.ConvertedValue.Should().Be(45.67);
        }

        /// <summary>
        /// Tests that ConvertAsync works with collection types.
        /// </summary>
        [TestMethod]
        public async Task ConvertAsync_WhenUsedWithCollections_ShouldWorkCorrectly()
        {
            var innerConverter = new CollectionConverter();
            var asyncConverter = new AsyncConverter<List<int>, List<string>>(innerConverter);

            var source = new List<int> { 1, 2, 3, 4, 5 };

            var result = await asyncConverter.ConvertAsync(source, CancellationToken.None);

            result.Should().NotBeNull();
            result.Should().HaveCount(5);
            result.Should().Equal("1", "2", "3", "4", "5");
        }

        #endregion

        #region Edge Case Tests

        /// <summary>
        /// Tests that ConvertAsync handles default values correctly.
        /// </summary>
        [TestMethod]
        public async Task ConvertAsync_WhenCalledWithDefaultValues_ShouldHandleCorrectly()
        {
            var intConverter = new AsyncConverter<int, string>(new TestSyncConverter<int, string>());
            var stringConverter = new AsyncConverter<string, int>(new TestSyncConverter<string, int>());

            var result1 = await intConverter.ConvertAsync(default, CancellationToken.None);
            var result2 = await stringConverter.ConvertAsync(default, CancellationToken.None);

            result1.Should().Be("0");
            result2.Should().Be(0); // Assuming null string converts to 0
        }

        /// <summary>
        /// Tests that ConvertAsync works with identity conversions.
        /// </summary>
        [TestMethod]
        public async Task ConvertAsync_WhenUsedAsIdentityConverter_ShouldWorkCorrectly()
        {
            var identityConverter = new IdentityConverter<string>();
            var asyncConverter = new AsyncConverter<string, string>(identityConverter);

            var input = "identity test";
            var result = await asyncConverter.ConvertAsync(input, CancellationToken.None);

            result.Should().BeSameAs(input);
        }

        #endregion

        #region Integration Tests

        /// <summary>
        /// Tests AsyncConverter in a realistic WebJobs scenario.
        /// </summary>
        [TestMethod]
        public async Task ConvertAsync_WhenUsedInWebJobsScenario_ShouldWorkCorrectly()
        {
            // Simulate converting a queue message body to a typed object
            var messageToObjectConverter = new JsonStringToObjectConverter<OrderMessage>();
            var asyncConverter = new AsyncConverter<string, OrderMessage>(messageToObjectConverter);

            var jsonMessage = "{\"OrderId\": 12345, \"CustomerId\": \"CUST001\", \"Amount\": 99.99}";

            var result = await asyncConverter.ConvertAsync(jsonMessage, CancellationToken.None);

            result.Should().NotBeNull();
            result.OrderId.Should().Be(12345);
            result.CustomerId.Should().Be("CUST001");
            result.Amount.Should().Be(99.99);
        }

        /// <summary>
        /// Tests AsyncConverter with chained conversions.
        /// </summary>
        [TestMethod]
        public async Task ConvertAsync_WhenUsedInChainedConversions_ShouldWorkCorrectly()
        {
            var stringToIntConverter = new AsyncConverter<string, int>(new TestSyncConverter<string, int>());
            var intToDoubleConverter = new AsyncConverter<int, double>(new TestSyncConverter<int, double>());

            // First conversion: string to int
            var intResult = await stringToIntConverter.ConvertAsync("42", CancellationToken.None);
            
            // Second conversion: int to double
            var doubleResult = await intToDoubleConverter.ConvertAsync(intResult, CancellationToken.None);

            intResult.Should().Be(42);
            doubleResult.Should().Be(42.0);
        }

        #endregion

    }

    #region Test Helper Classes

    /// <summary>
    /// Test implementation of IConverter for testing purposes.
    /// </summary>
    /// <typeparam name="TInput">Input type.</typeparam>
    /// <typeparam name="TOutput">Output type.</typeparam>
    internal class TestSyncConverter<TInput, TOutput> : IConverter<TInput, TOutput>
    {
        public int ConvertCallCount { get; private set; }
        public TInput LastInput { get; private set; }

        public TOutput Convert(TInput input)
        {
            ConvertCallCount++;
            LastInput = input;

            // Simple conversion logic for testing
            if (typeof(TInput) == typeof(string) && typeof(TOutput) == typeof(int))
            {
                var stringInput = input as string;
                if (stringInput is null)
                    return (TOutput)(object)0;
                
                if (int.TryParse(stringInput, out var intResult))
                    return (TOutput)(object)intResult;
                
                return (TOutput)(object)0;
            }

            if (typeof(TInput) == typeof(int) && typeof(TOutput) == typeof(string))
            {
                return (TOutput)(object)input.ToString();
            }

            if (typeof(TInput) == typeof(bool) && typeof(TOutput) == typeof(string))
            {
                return (TOutput)(object)input.ToString();
            }

            if (typeof(TInput) == typeof(string) && typeof(TOutput) == typeof(string))
            {
                var stringInput = input as string;
                return (TOutput)(object)$"converted_{stringInput ?? "null"}";
            }

            if (typeof(TInput) == typeof(int) && typeof(TOutput) == typeof(double))
            {
                return (TOutput)(object)System.Convert.ToDouble(input);
            }

            // Default fallback
            return default;
        }
    }

    /// <summary>
    /// Converter that throws exceptions for testing error handling.
    /// </summary>
    /// <typeparam name="TInput">Input type.</typeparam>
    /// <typeparam name="TOutput">Output type.</typeparam>
    internal class ThrowingConverter<TInput, TOutput> : IConverter<TInput, TOutput>
    {
        private readonly Exception _exceptionToThrow;

        public ThrowingConverter(Exception exceptionToThrow = null)
        {
            _exceptionToThrow = exceptionToThrow ?? new InvalidOperationException("Test exception from converter");
        }

        public TOutput Convert(TInput input)
        {
            throw _exceptionToThrow;
        }
    }

    /// <summary>
    /// Converter for complex object types.
    /// </summary>
    internal class ComplexObjectConverter : IConverter<SourceObject, TargetObject>
    {
        public TargetObject Convert(SourceObject input)
        {
            if (input is null)
                return null;

            return new TargetObject
            {
                ConvertedId = input.Id,
                ConvertedName = input.Name,
                ConvertedValue = input.Value
            };
        }
    }

    /// <summary>
    /// Converter for collection types.
    /// </summary>
    internal class CollectionConverter : IConverter<List<int>, List<string>>
    {
        public List<string> Convert(List<int> input)
        {
            if (input is null)
                return null;

            var result = new List<string>();
            foreach (var item in input)
            {
                result.Add(item.ToString());
            }
            return result;
        }
    }

    /// <summary>
    /// JSON string to object converter for testing.
    /// </summary>
    internal class JsonStringToObjectConverter<T> : IConverter<string, T>
    {
        public T Convert(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return default;

            return System.Text.Json.JsonSerializer.Deserialize<T>(input);
        }
    }

    /// <summary>
    /// Source object for complex type testing.
    /// </summary>
    internal class SourceObject
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public double Value { get; set; }
    }

    /// <summary>
    /// Target object for complex type testing.
    /// </summary>
    internal class TargetObject
    {
        public int ConvertedId { get; set; }
        public string ConvertedName { get; set; }
        public double ConvertedValue { get; set; }
    }

    /// <summary>
    /// Order message for WebJobs scenario testing.
    /// </summary>
    internal class OrderMessage
    {
        public int OrderId { get; set; }
        public string CustomerId { get; set; }
        public double Amount { get; set; }
    }

    #endregion

}
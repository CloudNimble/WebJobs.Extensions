// Copyright (c) CloudNimble, Inc. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using CloudNimble.WebJobs.Extensions.Common.Converters;
using FluentAssertions;
using Microsoft.Azure.WebJobs;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;

namespace CloudNimble.WebJobs.Extensions.Tests.Common.Converters
{

    /// <summary>
    /// Tests for the <see cref="IdentityConverter{TValue}"/> class.
    /// </summary>
    [TestClass]
    [TestCategory("Unit")]
    public class IdentityConverterTests
    {

        #region Basic Conversion Tests

        /// <summary>
        /// Tests that Convert returns the same string instance.
        /// </summary>
        [TestMethod]
        public void Convert_WhenCalledWithString_ShouldReturnSameInstance()
        {
            var converter = new IdentityConverter<string>();
            var input = "test string";

            var result = converter.Convert(input);

            result.Should().BeSameAs(input);
        }

        /// <summary>
        /// Tests that Convert returns the same integer value.
        /// </summary>
        [TestMethod]
        public void Convert_WhenCalledWithInteger_ShouldReturnSameValue()
        {
            var converter = new IdentityConverter<int>();
            var input = 42;

            var result = converter.Convert(input);

            result.Should().Be(input);
        }

        /// <summary>
        /// Tests that Convert returns the same boolean value.
        /// </summary>
        [TestMethod]
        public void Convert_WhenCalledWithBoolean_ShouldReturnSameValue()
        {
            var converter = new IdentityConverter<bool>();
            var inputTrue = true;
            var inputFalse = false;

            var resultTrue = converter.Convert(inputTrue);
            var resultFalse = converter.Convert(inputFalse);

            resultTrue.Should().Be(inputTrue);
            resultFalse.Should().Be(inputFalse);
        }

        /// <summary>
        /// Tests that Convert works with null reference types.
        /// </summary>
        [TestMethod]
        public void Convert_WhenCalledWithNull_ShouldReturnNull()
        {
            var converter = new IdentityConverter<string>();

            var result = converter.Convert(null);

            result.Should().BeNull();
        }

        /// <summary>
        /// Tests that Convert works with nullable value types.
        /// </summary>
        [TestMethod]
        public void Convert_WhenCalledWithNullableTypes_ShouldReturnSameValue()
        {
            var converter = new IdentityConverter<int?>();
            int? nullValue = null;
            int? valueWithValue = 123;

            var resultNull = converter.Convert(nullValue);
            var resultValue = converter.Convert(valueWithValue);

            resultNull.Should().BeNull();
            resultValue.Should().Be(123);
        }

        #endregion

        #region Reference Type Tests

        /// <summary>
        /// Tests that Convert preserves object references for reference types.
        /// </summary>
        [TestMethod]
        public void Convert_WhenCalledWithReferenceType_ShouldPreserveReference()
        {
            var converter = new IdentityConverter<List<string>>();
            var input = new List<string> { "item1", "item2", "item3" };

            var result = converter.Convert(input);

            result.Should().BeSameAs(input);
            result.Should().Equal(input);
        }

        /// <summary>
        /// Tests that Convert works with custom objects.
        /// </summary>
        [TestMethod]
        public void Convert_WhenCalledWithCustomObject_ShouldReturnSameInstance()
        {
            var converter = new IdentityConverter<TestClass>();
            var input = new TestClass { Id = 1, Name = "Test Object" };

            var result = converter.Convert(input);

            result.Should().BeSameAs(input);
            result.Id.Should().Be(1);
            result.Name.Should().Be("Test Object");
        }

        /// <summary>
        /// Tests that Convert works with arrays.
        /// </summary>
        [TestMethod]
        public void Convert_WhenCalledWithArray_ShouldReturnSameArray()
        {
            var converter = new IdentityConverter<int[]>();
            var input = new int[] { 1, 2, 3, 4, 5 };

            var result = converter.Convert(input);

            result.Should().BeSameAs(input);
            result.Should().Equal(input);
        }

        #endregion

        #region Value Type Tests

        /// <summary>
        /// Tests that Convert works with different numeric types.
        /// </summary>
        [TestMethod]
        public void Convert_WhenCalledWithNumericTypes_ShouldReturnSameValues()
        {
            var intConverter = new IdentityConverter<int>();
            var longConverter = new IdentityConverter<long>();
            var doubleConverter = new IdentityConverter<double>();
            var decimalConverter = new IdentityConverter<decimal>();

            var intResult = intConverter.Convert(42);
            var longResult = longConverter.Convert(42L);
            var doubleResult = doubleConverter.Convert(42.5);
            var decimalResult = decimalConverter.Convert(42.5m);

            intResult.Should().Be(42);
            longResult.Should().Be(42L);
            doubleResult.Should().Be(42.5);
            decimalResult.Should().Be(42.5m);
        }

        /// <summary>
        /// Tests that Convert works with DateTime.
        /// </summary>
        [TestMethod]
        public void Convert_WhenCalledWithDateTime_ShouldReturnSameValue()
        {
            var converter = new IdentityConverter<DateTime>();
            var input = new DateTime(2025, 1, 31, 14, 30, 0);

            var result = converter.Convert(input);

            result.Should().Be(input);
        }

        /// <summary>
        /// Tests that Convert works with TimeSpan.
        /// </summary>
        [TestMethod]
        public void Convert_WhenCalledWithTimeSpan_ShouldReturnSameValue()
        {
            var converter = new IdentityConverter<TimeSpan>();
            var input = TimeSpan.FromMinutes(30);

            var result = converter.Convert(input);

            result.Should().Be(input);
        }

        /// <summary>
        /// Tests that Convert works with Guid.
        /// </summary>
        [TestMethod]
        public void Convert_WhenCalledWithGuid_ShouldReturnSameValue()
        {
            var converter = new IdentityConverter<Guid>();
            var input = Guid.NewGuid();

            var result = converter.Convert(input);

            result.Should().Be(input);
        }

        #endregion

        #region Interface Implementation Tests

        /// <summary>
        /// Tests that IdentityConverter implements IConverter interface.
        /// </summary>
        [TestMethod]
        public void IdentityConverter_WhenExamined_ShouldImplementIConverter()
        {
            typeof(IdentityConverter<string>).Should().BeAssignableTo<IConverter<string, string>>();
        }

        /// <summary>
        /// Tests that converter can be used polymorphically.
        /// </summary>
        [TestMethod]
        public void IdentityConverter_WhenUsedPolymorphically_ShouldWorkCorrectly()
        {
            IConverter<string, string> converter = new IdentityConverter<string>();
            var input = "polymorphic test";

            var result = converter.Convert(input);

            result.Should().BeSameAs(input);
        }

        /// <summary>
        /// Tests that different generic type converters are different types.
        /// </summary>
        [TestMethod]
        public void IdentityConverter_WhenExaminingDifferentGenericTypes_ShouldBeDifferentTypes()
        {
            var stringConverter = new IdentityConverter<string>();
            var intConverter = new IdentityConverter<int>();

            stringConverter.GetType().Should().NotBe(intConverter.GetType());
            typeof(IdentityConverter<string>).Should().NotBe(typeof(IdentityConverter<int>));
        }

        #endregion

        #region Complex Type Tests

        /// <summary>
        /// Tests that Convert works with nested objects.
        /// </summary>
        [TestMethod]
        public void Convert_WhenCalledWithNestedObjects_ShouldPreserveStructure()
        {
            var converter = new IdentityConverter<ComplexTestClass>();
            var input = new ComplexTestClass
            {
                Id = 1,
                Name = "Parent",
                Child = new TestClass { Id = 2, Name = "Child" },
                Items = new List<string> { "item1", "item2" }
            };

            var result = converter.Convert(input);

            result.Should().BeSameAs(input);
            result.Child.Should().BeSameAs(input.Child);
            result.Items.Should().BeSameAs(input.Items);
        }

        /// <summary>
        /// Tests that Convert works with dictionary types.
        /// </summary>
        [TestMethod]
        public void Convert_WhenCalledWithDictionary_ShouldReturnSameDictionary()
        {
            var converter = new IdentityConverter<Dictionary<string, object>>();
            var input = new Dictionary<string, object>
            {
                { "key1", "value1" },
                { "key2", 42 },
                { "key3", true }
            };

            var result = converter.Convert(input);

            result.Should().BeSameAs(input);
            result.Should().HaveCount(3);
            result["key1"].Should().Be("value1");
            result["key2"].Should().Be(42);
            result["key3"].Should().Be(true);
        }

        #endregion

        #region Generic Constraint Tests

        /// <summary>
        /// Tests that IdentityConverter works with interface types.
        /// </summary>
        [TestMethod]
        public void Convert_WhenCalledWithInterfaceType_ShouldReturnSameInstance()
        {
            var converter = new IdentityConverter<ITestInterface>();
            ITestInterface input = new TestImplementation { Value = "interface test" };

            var result = converter.Convert(input);

            result.Should().BeSameAs(input);
            result.Value.Should().Be("interface test");
        }

        /// <summary>
        /// Tests that IdentityConverter works with abstract types.
        /// </summary>
        [TestMethod]
        public void Convert_WhenCalledWithAbstractType_ShouldReturnSameInstance()
        {
            var converter = new IdentityConverter<AbstractTestClass>();
            AbstractTestClass input = new ConcreteTestClass { Id = 123, ConcreteProperty = "concrete" };

            var result = converter.Convert(input);

            result.Should().BeSameAs(input);
            result.Id.Should().Be(123);
            ((ConcreteTestClass)result).ConcreteProperty.Should().Be("concrete");
        }

        #endregion

        #region Performance Tests

        /// <summary>
        /// Tests that IdentityConverter performs well with many conversions.
        /// </summary>
        [TestMethod]
        public void Convert_WhenCalledManyTimes_ShouldPerformWell()
        {
            var converter = new IdentityConverter<string>();
            var input = "performance test string";
            const int iterations = 100000;

            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            for (int i = 0; i < iterations; i++)
            {
                var result = converter.Convert(input);
                result.Should().BeSameAs(input);
            }

            stopwatch.Stop();

            // Should complete very quickly since it's just returning the input
            stopwatch.ElapsedMilliseconds.Should().BeLessThan(100);
        }

        /// <summary>
        /// Tests memory behavior with repeated conversions.
        /// </summary>
        [TestMethod]
        public void Convert_WhenCalledRepeatedly_ShouldNotAllocateMemory()
        {
            var converter = new IdentityConverter<TestClass>();
            var input = new TestClass { Id = 1, Name = "Memory Test" };

            // Multiple conversions should not create new objects
            var result1 = converter.Convert(input);
            var result2 = converter.Convert(input);
            var result3 = converter.Convert(input);

            result1.Should().BeSameAs(input);
            result2.Should().BeSameAs(input);
            result3.Should().BeSameAs(input);
            result1.Should().BeSameAs(result2);
            result2.Should().BeSameAs(result3);
        }

        #endregion

        #region Thread Safety Tests

        /// <summary>
        /// Tests that IdentityConverter is safe for concurrent use.
        /// </summary>
        [TestMethod]
        public void Convert_WhenUsedConcurrently_ShouldBeSafe()
        {
            var converter = new IdentityConverter<string>();
            var tasks = new List<System.Threading.Tasks.Task>();
            var results = new System.Collections.Concurrent.ConcurrentBag<string>();

            for (int i = 0; i < 10; i++)
            {
                int taskId = i;
                tasks.Add(System.Threading.Tasks.Task.Run(() =>
                {
                    var input = $"concurrent-test-{taskId}";
                    var result = converter.Convert(input);
                    results.Add(result);
                }));
            }

            System.Threading.Tasks.Task.WaitAll(tasks.ToArray());

            results.Should().HaveCount(10);
            results.Should().OnlyContain(r => r.StartsWith("concurrent-test-"));
        }

        #endregion

        #region Edge Case Tests

        /// <summary>
        /// Tests that Convert works with default values.
        /// </summary>
        [TestMethod]
        public void Convert_WhenCalledWithDefaultValues_ShouldReturnSameValue()
        {
            var stringConverter = new IdentityConverter<string>();
            var intConverter = new IdentityConverter<int>();
            var boolConverter = new IdentityConverter<bool>();

            var stringResult = stringConverter.Convert(default);
            var intResult = intConverter.Convert(default);
            var boolResult = boolConverter.Convert(default);

            stringResult.Should().BeNull();
            intResult.Should().Be(0);
            boolResult.Should().BeFalse();
        }

        /// <summary>
        /// Tests that Convert works with very large objects.
        /// </summary>
        [TestMethod]
        public void Convert_WhenCalledWithLargeObject_ShouldReturnSameInstance()
        {
            var converter = new IdentityConverter<byte[]>();
            var input = new byte[100000]; // Large array
            for (int i = 0; i < input.Length; i++)
            {
                input[i] = (byte)(i % 256);
            }

            var result = converter.Convert(input);

            result.Should().BeSameAs(input);
            result.Should().HaveCount(100000);
        }

        /// <summary>
        /// Tests that Convert works with special floating-point values.
        /// </summary>
        [TestMethod]
        public void Convert_WhenCalledWithSpecialFloatingPointValues_ShouldReturnSameValues()
        {
            var doubleConverter = new IdentityConverter<double>();
            var floatConverter = new IdentityConverter<float>();

            var nanDouble = doubleConverter.Convert(double.NaN);
            var infinityDouble = doubleConverter.Convert(double.PositiveInfinity);
            var negInfinityDouble = doubleConverter.Convert(double.NegativeInfinity);

            var nanFloat = floatConverter.Convert(float.NaN);
            var infinityFloat = floatConverter.Convert(float.PositiveInfinity);

            nanDouble.Should().Be(double.NaN);
            infinityDouble.Should().Be(double.PositiveInfinity);
            negInfinityDouble.Should().Be(double.NegativeInfinity);
            nanFloat.Should().Be(float.NaN);
            infinityFloat.Should().Be(float.PositiveInfinity);
        }

        #endregion

    }

    #region Test Helper Classes

    /// <summary>
    /// Test class for testing identity converter with custom objects.
    /// </summary>
    internal class TestClass
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }

    /// <summary>
    /// Complex test class with nested properties.
    /// </summary>
    internal class ComplexTestClass
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public TestClass Child { get; set; }
        public List<string> Items { get; set; }
    }

    /// <summary>
    /// Test interface for interface type testing.
    /// </summary>
    internal interface ITestInterface
    {
        string Value { get; set; }
    }

    /// <summary>
    /// Implementation of test interface.
    /// </summary>
    internal class TestImplementation : ITestInterface
    {
        public string Value { get; set; }
    }

    /// <summary>
    /// Abstract test class for abstract type testing.
    /// </summary>
    internal abstract class AbstractTestClass
    {
        public int Id { get; set; }
        public abstract string GetDescription();
    }

    /// <summary>
    /// Concrete implementation of abstract test class.
    /// </summary>
    internal class ConcreteTestClass : AbstractTestClass
    {
        public string ConcreteProperty { get; set; }
        
        public override string GetDescription()
        {
            return $"Concrete class with Id={Id}, Property={ConcreteProperty}";
        }
    }

    #endregion

}
// Copyright (c) CloudNimble, Inc. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using CloudNimble.WebJobs.Extensions.Common;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace CloudNimble.WebJobs.Extensions.Tests.Common
{

    /// <summary>
    /// Tests for the <see cref="ContextAccessor{TValue}"/> class.
    /// </summary>
    [TestClass]
    [TestCategory("Unit")]
    public class ContextAccessorTests
    {

        #region Constructor Tests

        /// <summary>
        /// Tests that the constructor initializes the value to default.
        /// </summary>
        [TestMethod]
        public void Constructor_WhenCalled_ShouldInitializeValueToDefault()
        {
            var stringAccessor = new ContextAccessor<string>();
            var intAccessor = new ContextAccessor<int>();
            var objectAccessor = new ContextAccessor<object>();

            stringAccessor.Value.Should().BeNull();
            intAccessor.Value.Should().Be(0);
            objectAccessor.Value.Should().BeNull();
        }

        #endregion

        #region Value Property Tests

        /// <summary>
        /// Tests that the Value property can be retrieved correctly.
        /// </summary>
        [TestMethod]
        public void Value_WhenAccessed_ShouldReturnCurrentValue()
        {
            var accessor = new ContextAccessor<string>();

            accessor.Value.Should().BeNull();
        }

        /// <summary>
        /// Tests that the Value property returns the same instance on multiple calls.
        /// </summary>
        [TestMethod]
        public void Value_WhenAccessedMultipleTimes_ShouldReturnSameValue()
        {
            var accessor = new ContextAccessor<string>();
            accessor.SetValue("test value");

            var value1 = accessor.Value;
            var value2 = accessor.Value;

            value1.Should().BeSameAs(value2);
            value1.Should().Be("test value");
        }

        #endregion

        #region SetValue Method Tests

        /// <summary>
        /// Tests that SetValue correctly sets the value.
        /// </summary>
        [TestMethod]
        public void SetValue_WhenCalledWithValue_ShouldSetValue()
        {
            var accessor = new ContextAccessor<string>();

            accessor.SetValue("test value");

            accessor.Value.Should().Be("test value");
        }

        /// <summary>
        /// Tests that SetValue can be called multiple times.
        /// </summary>
        [TestMethod]
        public void SetValue_WhenCalledMultipleTimes_ShouldUpdateValue()
        {
            var accessor = new ContextAccessor<string>();

            accessor.SetValue("first value");
            accessor.Value.Should().Be("first value");

            accessor.SetValue("second value");
            accessor.Value.Should().Be("second value");

            accessor.SetValue("third value");
            accessor.Value.Should().Be("third value");
        }

        /// <summary>
        /// Tests that SetValue can set null values.
        /// </summary>
        [TestMethod]
        public void SetValue_WhenCalledWithNull_ShouldSetToNull()
        {
            var accessor = new ContextAccessor<string>();
            accessor.SetValue("initial value");

            accessor.SetValue(null);

            accessor.Value.Should().BeNull();
        }

        /// <summary>
        /// Tests that SetValue can set default values for value types.
        /// </summary>
        [TestMethod]
        public void SetValue_WhenCalledWithDefaultValueType_ShouldSetToDefault()
        {
            var intAccessor = new ContextAccessor<int>();
            intAccessor.SetValue(42);

            intAccessor.SetValue(default);

            intAccessor.Value.Should().Be(0);
        }

        #endregion

        #region Interface Implementation Tests

        /// <summary>
        /// Tests that ContextAccessor implements IContextGetter correctly.
        /// </summary>
        [TestMethod]
        public void ContextAccessor_WhenUsedAsIContextGetter_ShouldWorkCorrectly()
        {
            IContextGetter<string> getter = new ContextAccessor<string>();
            var accessor = (ContextAccessor<string>)getter;
            accessor.SetValue("test value");

            getter.Value.Should().Be("test value");
        }

        /// <summary>
        /// Tests that ContextAccessor implements IContextSetter correctly.
        /// </summary>
        [TestMethod]
        public void ContextAccessor_WhenUsedAsIContextSetter_ShouldWorkCorrectly()
        {
            IContextSetter<string> setter = new ContextAccessor<string>();
            var accessor = (ContextAccessor<string>)setter;

            setter.SetValue("test value");

            accessor.Value.Should().Be("test value");
        }

        /// <summary>
        /// Tests that ContextAccessor can be used polymorphically.
        /// </summary>
        [TestMethod]
        public void ContextAccessor_WhenUsedPolymorphically_ShouldWorkCorrectly()
        {
            var accessor = new ContextAccessor<string>();
            IContextGetter<string> getter = accessor;
            IContextSetter<string> setter = accessor;

            setter.SetValue("polymorphic value");
            getter.Value.Should().Be("polymorphic value");
        }

        #endregion

        #region Generic Type Tests

        /// <summary>
        /// Tests that ContextAccessor works with different value types.
        /// </summary>
        [TestMethod]
        public void ContextAccessor_WhenUsedWithValueTypes_ShouldWorkCorrectly()
        {
            var intAccessor = new ContextAccessor<int>();
            var boolAccessor = new ContextAccessor<bool>();
            var doubleAccessor = new ContextAccessor<double>();

            intAccessor.SetValue(42);
            boolAccessor.SetValue(true);
            doubleAccessor.SetValue(3.14);

            intAccessor.Value.Should().Be(42);
            boolAccessor.Value.Should().BeTrue();
            doubleAccessor.Value.Should().Be(3.14);
        }

        /// <summary>
        /// Tests that ContextAccessor works with reference types.
        /// </summary>
        [TestMethod]
        public void ContextAccessor_WhenUsedWithReferenceTypes_ShouldWorkCorrectly()
        {
            var stringAccessor = new ContextAccessor<string>();
            var listAccessor = new ContextAccessor<List<int>>();
            var objectAccessor = new ContextAccessor<object>();

            var testList = new List<int> { 1, 2, 3 };
            var testObject = new { Name = "Test", Value = 42 };

            stringAccessor.SetValue("test string");
            listAccessor.SetValue(testList);
            objectAccessor.SetValue(testObject);

            stringAccessor.Value.Should().Be("test string");
            listAccessor.Value.Should().BeSameAs(testList);
            objectAccessor.Value.Should().BeSameAs(testObject);
        }

        /// <summary>
        /// Tests that ContextAccessor works with nullable value types.
        /// </summary>
        [TestMethod]
        public void ContextAccessor_WhenUsedWithNullableValueTypes_ShouldWorkCorrectly()
        {
            var nullableIntAccessor = new ContextAccessor<int?>();
            var nullableBoolAccessor = new ContextAccessor<bool?>();

            nullableIntAccessor.SetValue(42);
            nullableBoolAccessor.SetValue(true);

            nullableIntAccessor.Value.Should().Be(42);
            nullableBoolAccessor.Value.Should().BeTrue();

            nullableIntAccessor.SetValue(null);
            nullableBoolAccessor.SetValue(null);

            nullableIntAccessor.Value.Should().BeNull();
            nullableBoolAccessor.Value.Should().BeNull();
        }

        #endregion

        #region Complex Object Tests

        /// <summary>
        /// Tests that ContextAccessor works with custom objects.
        /// </summary>
        [TestMethod]
        public void ContextAccessor_WhenUsedWithCustomObjects_ShouldWorkCorrectly()
        {
            var accessor = new ContextAccessor<TestContext>();
            var testContext = new TestContext
            {
                Id = "test-id",
                Name = "Test Context",
                Values = new Dictionary<string, object>
                {
                    { "key1", "value1" },
                    { "key2", 42 }
                }
            };

            accessor.SetValue(testContext);

            accessor.Value.Should().BeSameAs(testContext);
            accessor.Value.Id.Should().Be("test-id");
            accessor.Value.Name.Should().Be("Test Context");
            accessor.Value.Values.Should().HaveCount(2);
        }

        /// <summary>
        /// Tests that modifying the stored object affects the retrieved value.
        /// </summary>
        [TestMethod]
        public void ContextAccessor_WhenStoredObjectIsModified_ShouldReflectChanges()
        {
            var accessor = new ContextAccessor<TestContext>();
            var testContext = new TestContext
            {
                Id = "original-id",
                Name = "Original Name",
                Values = new Dictionary<string, object>()
            };

            accessor.SetValue(testContext);
            
            // Modify the object after storing
            testContext.Id = "modified-id";
            testContext.Values["new-key"] = "new-value";

            // Changes should be reflected since we store by reference
            accessor.Value.Id.Should().Be("modified-id");
            accessor.Value.Values.Should().ContainKey("new-key");
        }

        #endregion

        #region Thread Safety Tests

        /// <summary>
        /// Tests that ContextAccessor behaves correctly under concurrent access.
        /// </summary>
        [TestMethod]
        public void ContextAccessor_WhenAccessedConcurrently_ShouldBehaveCorrectly()
        {
            var accessor = new ContextAccessor<string>();
            var tasks = new List<Task>();
            var results = new List<string>();
            var lockObj = new object();

            // Create multiple tasks that set and get values
            for (int i = 0; i < 10; i++)
            {
                int taskId = i;
                tasks.Add(Task.Run(() =>
                {
                    var value = $"value-{taskId}";
                    accessor.SetValue(value);
                    
                    // Small delay to allow interleaving
                    Thread.Sleep(1);
                    
                    var retrievedValue = accessor.Value;
                    lock (lockObj)
                    {
                        results.Add(retrievedValue);
                    }
                }));
            }

            Task.WaitAll(tasks.ToArray());

            // All tasks should have completed
            results.Should().HaveCount(10);
            
            // The final value should be one of the set values
            var finalValue = accessor.Value;
            finalValue.Should().StartWith("value-");
        }

        /// <summary>
        /// Tests that ContextAccessor maintains thread-local-like behavior when expected.
        /// </summary>
        [TestMethod]
        public void ContextAccessor_WhenUsedInDifferentContexts_ShouldShareState()
        {
            var accessor = new ContextAccessor<string>();
            accessor.SetValue("initial value");

            var taskResult = "";
            var task = Task.Run(() =>
            {
                // ContextAccessor is not thread-local, so it shares state
                taskResult = accessor.Value;
                accessor.SetValue("task value");
            });

            task.Wait();

            // The task should see the initial value and modify the shared state
            taskResult.Should().Be("initial value");
            accessor.Value.Should().Be("task value");
        }

        #endregion

        #region Performance Tests

        /// <summary>
        /// Tests that ContextAccessor can handle many get/set operations efficiently.
        /// </summary>
        [TestMethod]
        public void ContextAccessor_WhenUsedIntensively_ShouldPerformWell()
        {
            var accessor = new ContextAccessor<string>();
            const int iterations = 10000;

            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            for (int i = 0; i < iterations; i++)
            {
                accessor.SetValue($"value-{i}");
                var value = accessor.Value;
                value.Should().Be($"value-{i}");
            }

            stopwatch.Stop();

            // Operations should complete quickly (allowing for some variance in test environments)
            stopwatch.ElapsedMilliseconds.Should().BeLessThan(1000);
        }

        #endregion

        #region Edge Case Tests

        /// <summary>
        /// Tests that ContextAccessor handles very large strings correctly.
        /// </summary>
        [TestMethod]
        public void ContextAccessor_WhenUsedWithLargeStrings_ShouldHandleCorrectly()
        {
            var accessor = new ContextAccessor<string>();
            var largeString = new string('a', 100000);

            accessor.SetValue(largeString);

            accessor.Value.Should().BeSameAs(largeString);
            accessor.Value.Should().HaveLength(100000);
        }

        /// <summary>
        /// Tests that ContextAccessor handles special characters correctly.
        /// </summary>
        [TestMethod]
        public void ContextAccessor_WhenUsedWithSpecialCharacters_ShouldHandleCorrectly()
        {
            var accessor = new ContextAccessor<string>();
            var specialString = "Unicode: 🚀 Special chars: @#$%^&*() Newlines:\n\r\t End";

            accessor.SetValue(specialString);

            accessor.Value.Should().Be(specialString);
            accessor.Value.Should().Contain("🚀");
            accessor.Value.Should().Contain("\n");
        }

        /// <summary>
        /// Tests that ContextAccessor can store and retrieve empty collections.
        /// </summary>
        [TestMethod]
        public void ContextAccessor_WhenUsedWithEmptyCollections_ShouldHandleCorrectly()
        {
            var listAccessor = new ContextAccessor<List<string>>();
            var dictAccessor = new ContextAccessor<Dictionary<string, int>>();

            var emptyList = new List<string>();
            var emptyDict = new Dictionary<string, int>();

            listAccessor.SetValue(emptyList);
            dictAccessor.SetValue(emptyDict);

            listAccessor.Value.Should().BeSameAs(emptyList);
            listAccessor.Value.Should().BeEmpty();

            dictAccessor.Value.Should().BeSameAs(emptyDict);
            dictAccessor.Value.Should().BeEmpty();
        }

        #endregion

    }

    #region Test Helper Classes

    /// <summary>
    /// Test context class for complex object testing.
    /// </summary>
    internal class TestContext
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public Dictionary<string, object> Values { get; set; } = new();
    }

    #endregion

}
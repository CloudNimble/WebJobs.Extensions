// Copyright (c) CloudNimble, Inc. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using CloudNimble.WebJobs.Extensions.Common;
using CloudNimble.WebJobs.Extensions.Common.Queues;
using FluentAssertions;
using Microsoft.Azure.WebJobs.Hosting;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Text.Json;

namespace CloudNimble.WebJobs.Extensions.Tests.Common.Queues
{

    /// <summary>
    /// Tests for the <see cref="QueuesOptionsBase"/> configuration class.
    /// </summary>
    [TestClass]
    [TestCategory("Unit")]
    public class QueuesOptionsBaseTests
    {

        #region Constructor Tests

        /// <summary>
        /// Tests that the default constructor sets expected default values.
        /// </summary>
        [TestMethod]
        public void Constructor_WhenCalled_ShouldSetDefaultValues()
        {
            var options = new QueuesOptionsBase();

            options.BatchSize.Should().Be(16);
            options.MaxDequeueCount.Should().Be(5);
            options.MaxPollingInterval.Should().Be(QueuePollingIntervals.DefaultMaximum);
            options.VisibilityTimeout.Should().Be(TimeSpan.Zero);
            options.MessageEncoding.Should().Be(QueueMessageEncoding.Base64);
        }

        /// <summary>
        /// Tests that NewBatchThreshold is calculated correctly when not explicitly set.
        /// </summary>
        [TestMethod]
        public void Constructor_WhenNewBatchThresholdNotSet_ShouldCalculateDefault()
        {
            var options = new QueuesOptionsBase();

            // Default should be BatchSize/2 * ProcessorCount
            var expected = options.BatchSize / 2 * SkuUtility.ProcessorCount;
            options.NewBatchThreshold.Should().Be(expected);
        }

        #endregion

        #region BatchSize Property Tests

        /// <summary>
        /// Tests that BatchSize can be set to valid values.
        /// </summary>
        [TestMethod]
        public void BatchSize_WhenSetToValidValue_ShouldAcceptValue()
        {
            var options = new QueuesOptionsBase();

            options.BatchSize = 10;

            options.BatchSize.Should().Be(10);
        }

        /// <summary>
        /// Tests that BatchSize throws when set to zero.
        /// </summary>
        [TestMethod]
        public void BatchSize_WhenSetToZero_ShouldThrowArgumentOutOfRangeException()
        {
            var options = new QueuesOptionsBase();

            var action = () => options.BatchSize = 0;

            action.Should().Throw<ArgumentOutOfRangeException>();
        }

        /// <summary>
        /// Tests that BatchSize throws when set to negative value.
        /// </summary>
        [TestMethod]
        public void BatchSize_WhenSetToNegativeValue_ShouldThrowArgumentOutOfRangeException()
        {
            var options = new QueuesOptionsBase();

            var action = () => options.BatchSize = -1;

            action.Should().Throw<ArgumentOutOfRangeException>();
        }

        /// <summary>
        /// Tests that BatchSize throws when set above maximum allowed value.
        /// </summary>
        [TestMethod]
        public void BatchSize_WhenSetAboveMaximum_ShouldThrowArgumentOutOfRangeException()
        {
            var options = new QueuesOptionsBase();

            var action = () => options.BatchSize = 33; // Max is 32

            action.Should().Throw<ArgumentOutOfRangeException>();
        }

        /// <summary>
        /// Tests that BatchSize accepts the maximum allowed value.
        /// </summary>
        [TestMethod]
        public void BatchSize_WhenSetToMaximumValue_ShouldAcceptValue()
        {
            var options = new QueuesOptionsBase();

            options.BatchSize = 32; // Maximum allowed

            options.BatchSize.Should().Be(32);
        }

        #endregion

        #region NewBatchThreshold Property Tests

        /// <summary>
        /// Tests that NewBatchThreshold can be set to valid values.
        /// </summary>
        [TestMethod]
        public void NewBatchThreshold_WhenSetToValidValue_ShouldAcceptValue()
        {
            var options = new QueuesOptionsBase();

            options.NewBatchThreshold = 5;

            options.NewBatchThreshold.Should().Be(5);
        }

        /// <summary>
        /// Tests that NewBatchThreshold accepts zero.
        /// </summary>
        [TestMethod]
        public void NewBatchThreshold_WhenSetToZero_ShouldAcceptValue()
        {
            var options = new QueuesOptionsBase();

            options.NewBatchThreshold = 0;

            options.NewBatchThreshold.Should().Be(0);
        }

        /// <summary>
        /// Tests that NewBatchThreshold throws when set to negative value.
        /// </summary>
        [TestMethod]
        public void NewBatchThreshold_WhenSetToNegativeValue_ShouldThrowArgumentOutOfRangeException()
        {
            var options = new QueuesOptionsBase();

            var action = () => options.NewBatchThreshold = -1;

            action.Should().Throw<ArgumentOutOfRangeException>();
        }

        #endregion

        #region MaxPollingInterval Property Tests

        /// <summary>
        /// Tests that MaxPollingInterval can be set to valid values.
        /// </summary>
        [TestMethod]
        public void MaxPollingInterval_WhenSetToValidValue_ShouldAcceptValue()
        {
            var options = new QueuesOptionsBase();
            var interval = TimeSpan.FromMinutes(2);

            options.MaxPollingInterval = interval;

            options.MaxPollingInterval.Should().Be(interval);
        }

        /// <summary>
        /// Tests that MaxPollingInterval accepts the minimum allowed value.
        /// </summary>
        [TestMethod]
        public void MaxPollingInterval_WhenSetToMinimumValue_ShouldAcceptValue()
        {
            var options = new QueuesOptionsBase();

            options.MaxPollingInterval = QueuePollingIntervals.Minimum;

            options.MaxPollingInterval.Should().Be(QueuePollingIntervals.Minimum);
        }

        /// <summary>
        /// Tests that MaxPollingInterval throws when set below minimum.
        /// </summary>
        [TestMethod]
        public void MaxPollingInterval_WhenSetBelowMinimum_ShouldThrowArgumentException()
        {
            var options = new QueuesOptionsBase();
            var tooSmall = TimeSpan.FromMilliseconds(50); // Below 100ms minimum

            var action = () => options.MaxPollingInterval = tooSmall;

            action.Should().Throw<ArgumentException>()
                .WithMessage("*MaxPollingInterval must not be less than*");
        }

        #endregion

        #region MaxDequeueCount Property Tests

        /// <summary>
        /// Tests that MaxDequeueCount can be set to valid values.
        /// </summary>
        [TestMethod]
        public void MaxDequeueCount_WhenSetToValidValue_ShouldAcceptValue()
        {
            var options = new QueuesOptionsBase();

            options.MaxDequeueCount = 10;

            options.MaxDequeueCount.Should().Be(10);
        }

        /// <summary>
        /// Tests that MaxDequeueCount accepts minimum value of 1.
        /// </summary>
        [TestMethod]
        public void MaxDequeueCount_WhenSetToOne_ShouldAcceptValue()
        {
            var options = new QueuesOptionsBase();

            options.MaxDequeueCount = 1;

            options.MaxDequeueCount.Should().Be(1);
        }

        /// <summary>
        /// Tests that MaxDequeueCount throws when set to zero.
        /// </summary>
        [TestMethod]
        public void MaxDequeueCount_WhenSetToZero_ShouldThrowArgumentException()
        {
            var options = new QueuesOptionsBase();

            var action = () => options.MaxDequeueCount = 0;

            action.Should().Throw<ArgumentException>()
                .WithMessage("*MaxDequeueCount must not be less than 1*");
        }

        /// <summary>
        /// Tests that MaxDequeueCount throws when set to negative value.
        /// </summary>
        [TestMethod]
        public void MaxDequeueCount_WhenSetToNegativeValue_ShouldThrowArgumentException()
        {
            var options = new QueuesOptionsBase();

            var action = () => options.MaxDequeueCount = -5;

            action.Should().Throw<ArgumentException>()
                .WithMessage("*MaxDequeueCount must not be less than 1*");
        }

        #endregion

        #region VisibilityTimeout Property Tests

        /// <summary>
        /// Tests that VisibilityTimeout can be set to valid values.
        /// </summary>
        [TestMethod]
        public void VisibilityTimeout_WhenSetToValidValue_ShouldAcceptValue()
        {
            var options = new QueuesOptionsBase();
            var timeout = TimeSpan.FromMinutes(5);

            options.VisibilityTimeout = timeout;

            options.VisibilityTimeout.Should().Be(timeout);
        }

        /// <summary>
        /// Tests that VisibilityTimeout can be set to zero.
        /// </summary>
        [TestMethod]
        public void VisibilityTimeout_WhenSetToZero_ShouldAcceptValue()
        {
            var options = new QueuesOptionsBase();

            options.VisibilityTimeout = TimeSpan.Zero;

            options.VisibilityTimeout.Should().Be(TimeSpan.Zero);
        }

        #endregion

        #region MessageEncoding Property Tests

        /// <summary>
        /// Tests that MessageEncoding can be set to valid enum values.
        /// </summary>
        [TestMethod]
        public void MessageEncoding_WhenSetToValidValue_ShouldAcceptValue()
        {
            var options = new QueuesOptionsBase();

            options.MessageEncoding = QueueMessageEncoding.None;

            options.MessageEncoding.Should().Be(QueueMessageEncoding.None);
        }

        #endregion

        #region Clone Method Tests

        /// <summary>
        /// Tests that Clone creates a deep copy with all properties.
        /// </summary>
        [TestMethod]
        public void Clone_WhenCalled_ShouldCreateDeepCopyWithAllProperties()
        {
            var original = new QueuesOptionsBase
            {
                BatchSize = 20,
                NewBatchThreshold = 10,
                MaxPollingInterval = TimeSpan.FromMinutes(3),
                MaxDequeueCount = 8,
                VisibilityTimeout = TimeSpan.FromMinutes(2),
                MessageEncoding = QueueMessageEncoding.None
            };

            var clone = original.Clone();

            clone.Should().NotBeSameAs(original);
            clone.BatchSize.Should().Be(original.BatchSize);
            clone.NewBatchThreshold.Should().Be(original.NewBatchThreshold);
            clone.MaxPollingInterval.Should().Be(original.MaxPollingInterval);
            clone.MaxDequeueCount.Should().Be(original.MaxDequeueCount);
            clone.VisibilityTimeout.Should().Be(original.VisibilityTimeout);
            clone.MessageEncoding.Should().Be(original.MessageEncoding);
        }

        /// <summary>
        /// Tests that Clone preserves unset NewBatchThreshold behavior.
        /// </summary>
        [TestMethod]
        public void Clone_WhenNewBatchThresholdNotExplicitlySet_ShouldPreserveCalculatedBehavior()
        {
            var original = new QueuesOptionsBase
            {
                BatchSize = 24
            };
            // Don't set NewBatchThreshold explicitly so it uses calculated value

            var clone = original.Clone();

            // Both should calculate the same value
            clone.NewBatchThreshold.Should().Be(original.NewBatchThreshold);
        }

        /// <summary>
        /// Tests that modifying cloned options doesn't affect original.
        /// </summary>
        [TestMethod]
        public void Clone_WhenClonedOptionsModified_ShouldNotAffectOriginal()
        {
            var original = new QueuesOptionsBase
            {
                BatchSize = 16,
                MaxDequeueCount = 5
            };

            var clone = original.Clone();
            clone.BatchSize = 32;
            clone.MaxDequeueCount = 10;

            original.BatchSize.Should().Be(16);
            original.MaxDequeueCount.Should().Be(5);
        }

        #endregion

        #region Format Method Tests

        /// <summary>
        /// Tests that Format method returns valid JSON.
        /// </summary>
        [TestMethod]
        public void Format_WhenCalled_ShouldReturnValidJson()
        {
            var options = new QueuesOptionsBase();
            var formatter = (IOptionsFormatter)options;

            var json = formatter.Format();

            json.Should().NotBeNullOrWhiteSpace();
            var action = () => JsonDocument.Parse(json);
            action.Should().NotThrow();
        }

        /// <summary>
        /// Tests that Format method includes all expected properties in camelCase.
        /// </summary>
        [TestMethod]
        public void Format_WhenCalled_ShouldIncludeAllPropertiesInCamelCase()
        {
            var options = new QueuesOptionsBase
            {
                BatchSize = 20,
                NewBatchThreshold = 10,
                MaxPollingInterval = TimeSpan.FromMinutes(2),
                MaxDequeueCount = 8,
                VisibilityTimeout = TimeSpan.FromSeconds(30),
                MessageEncoding = QueueMessageEncoding.None
            };
            var formatter = (IOptionsFormatter)options;

            var json = formatter.Format();

            using var document = JsonDocument.Parse(json);
            var root = document.RootElement;

            // Verify camelCase property names
            root.GetProperty("batchSize").GetInt32().Should().Be(20);
            root.GetProperty("newBatchThreshold").GetInt32().Should().Be(10);
            root.GetProperty("maxDequeueCount").GetInt32().Should().Be(8);
            root.GetProperty("messageEncoding").GetString().Should().Be("none");
        }

        /// <summary>
        /// Tests that Format method handles TimeSpan properties correctly with camelCase.
        /// </summary>
        [TestMethod]
        public void Format_WhenCalledWithTimeSpanProperties_ShouldSerializeCorrectlyInCamelCase()
        {
            var options = new QueuesOptionsBase
            {
                MaxPollingInterval = TimeSpan.FromMinutes(5),
                VisibilityTimeout = TimeSpan.FromSeconds(120)
            };
            var formatter = (IOptionsFormatter)options;

            var json = formatter.Format();

            // Verify camelCase property names for TimeSpan properties
            json.Should().Contain("maxPollingInterval");
            json.Should().Contain("visibilityTimeout");
            var action = () => JsonDocument.Parse(json);
            action.Should().NotThrow();
        }

        /// <summary>
        /// Tests that enum values are serialized in camelCase.
        /// </summary>
        [TestMethod]
        public void Format_WhenCalledWithEnumValues_ShouldSerializeEnumsInCamelCase()
        {
            var optionsBase64 = new QueuesOptionsBase
            {
                MessageEncoding = QueueMessageEncoding.Base64
            };
            var optionsNone = new QueuesOptionsBase
            {
                MessageEncoding = QueueMessageEncoding.None
            };

            var jsonBase64 = ((IOptionsFormatter)optionsBase64).Format();
            var jsonNone = ((IOptionsFormatter)optionsNone).Format();

            // Verify enum values are camelCase
            jsonBase64.Should().Contain("\"messageEncoding\": \"base64\"");
            jsonNone.Should().Contain("\"messageEncoding\": \"none\"");
        }

        #endregion

        #region Edge Case Tests

        /// <summary>
        /// Tests behavior when BatchSize is changed after NewBatchThreshold is set.
        /// </summary>
        [TestMethod]
        public void NewBatchThreshold_WhenBatchSizeChangedAfterExplicitSet_ShouldMaintainExplicitValue()
        {
            var options = new QueuesOptionsBase();
            options.NewBatchThreshold = 15; // Set explicitly
            var originalThreshold = options.NewBatchThreshold;

            options.BatchSize = 32; // Change BatchSize

            options.NewBatchThreshold.Should().Be(originalThreshold);
        }

        /// <summary>
        /// Tests that large values are handled correctly.
        /// </summary>
        [TestMethod]
        public void Properties_WhenSetToLargeValidValues_ShouldHandleCorrectly()
        {
            var options = new QueuesOptionsBase
            {
                MaxDequeueCount = int.MaxValue,
                NewBatchThreshold = int.MaxValue - 1000,
                VisibilityTimeout = TimeSpan.FromDays(365)
            };

            options.MaxDequeueCount.Should().Be(int.MaxValue);
            options.NewBatchThreshold.Should().Be(int.MaxValue - 1000);
            options.VisibilityTimeout.Should().Be(TimeSpan.FromDays(365));
        }

        #endregion

    }

}
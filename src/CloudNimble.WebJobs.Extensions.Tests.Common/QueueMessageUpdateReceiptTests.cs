// Copyright (c) CloudNimble, Inc. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using CloudNimble.WebJobs.Extensions.Common.Queues;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace CloudNimble.WebJobs.Extensions.Common.Tests.Queues
{

    /// <summary>
    /// Tests for the <see cref="QueueMessageUpdateReceipt"/> class.
    /// </summary>
    [TestClass]
    [TestCategory("Unit")]
    public class QueueMessageUpdateReceiptTests
    {

        #region Constructor Tests

        /// <summary>
        /// Tests that the internal constructor initializes properties to default values.
        /// </summary>
        [TestMethod]
        public void Constructor_WhenCalled_ShouldInitializeToDefaults()
        {
            var receipt = new QueueMessageUpdateReceipt();

            receipt.NextVisibleOn.Should().Be(default(DateTimeOffset));
            receipt.PopReceipt.Should().BeNull();
        }

        #endregion

        #region NextVisibleOn Property Tests

        /// <summary>
        /// Tests that NextVisibleOn can be set and retrieved correctly.
        /// </summary>
        [TestMethod]
        public void NextVisibleOn_WhenSet_ShouldReturnCorrectValue()
        {
            var receipt = new QueueMessageUpdateReceipt();
            var expectedTime = DateTimeOffset.UtcNow.AddMinutes(5);

            receipt.NextVisibleOn = expectedTime;

            receipt.NextVisibleOn.Should().Be(expectedTime);
        }

        /// <summary>
        /// Tests that NextVisibleOn can be set to a past date.
        /// </summary>
        [TestMethod]
        public void NextVisibleOn_WhenSetToPastDate_ShouldAcceptValue()
        {
            var receipt = new QueueMessageUpdateReceipt();
            var pastTime = DateTimeOffset.UtcNow.AddHours(-2);

            receipt.NextVisibleOn = pastTime;

            receipt.NextVisibleOn.Should().Be(pastTime);
        }

        /// <summary>
        /// Tests that NextVisibleOn can be set to a future date.
        /// </summary>
        [TestMethod]
        public void NextVisibleOn_WhenSetToFutureDate_ShouldAcceptValue()
        {
            var receipt = new QueueMessageUpdateReceipt();
            var futureTime = DateTimeOffset.UtcNow.AddDays(1);

            receipt.NextVisibleOn = futureTime;

            receipt.NextVisibleOn.Should().Be(futureTime);
        }

        /// <summary>
        /// Tests that NextVisibleOn can be set to minimum DateTimeOffset value.
        /// </summary>
        [TestMethod]
        public void NextVisibleOn_WhenSetToMinValue_ShouldAcceptValue()
        {
            var receipt = new QueueMessageUpdateReceipt();

            receipt.NextVisibleOn = DateTimeOffset.MinValue;

            receipt.NextVisibleOn.Should().Be(DateTimeOffset.MinValue);
        }

        /// <summary>
        /// Tests that NextVisibleOn can be set to maximum DateTimeOffset value.
        /// </summary>
        [TestMethod]
        public void NextVisibleOn_WhenSetToMaxValue_ShouldAcceptValue()
        {
            var receipt = new QueueMessageUpdateReceipt();

            receipt.NextVisibleOn = DateTimeOffset.MaxValue;

            receipt.NextVisibleOn.Should().Be(DateTimeOffset.MaxValue);
        }

        /// <summary>
        /// Tests that NextVisibleOn preserves timezone information.
        /// </summary>
        [TestMethod]
        public void NextVisibleOn_WhenSetWithDifferentTimezone_ShouldPreserveTimezone()
        {
            var receipt = new QueueMessageUpdateReceipt();
            var easternTime = new DateTimeOffset(2025, 1, 15, 14, 30, 0, TimeSpan.FromHours(-5));
            var pacificTime = new DateTimeOffset(2025, 1, 15, 11, 30, 0, TimeSpan.FromHours(-8));

            receipt.NextVisibleOn = easternTime;
            receipt.NextVisibleOn.Should().Be(easternTime);
            receipt.NextVisibleOn.Offset.Should().Be(TimeSpan.FromHours(-5));

            receipt.NextVisibleOn = pacificTime;
            receipt.NextVisibleOn.Should().Be(pacificTime);
            receipt.NextVisibleOn.Offset.Should().Be(TimeSpan.FromHours(-8));
        }

        #endregion

        #region PopReceipt Property Tests

        /// <summary>
        /// Tests that PopReceipt can be set and retrieved correctly.
        /// </summary>
        [TestMethod]
        public void PopReceipt_WhenSet_ShouldReturnCorrectValue()
        {
            var receipt = new QueueMessageUpdateReceipt();
            var expectedReceipt = "test-pop-receipt-12345";

            receipt.PopReceipt = expectedReceipt;

            receipt.PopReceipt.Should().Be(expectedReceipt);
        }

        /// <summary>
        /// Tests that PopReceipt can be set to null.
        /// </summary>
        [TestMethod]
        public void PopReceipt_WhenSetToNull_ShouldAcceptNull()
        {
            var receipt = new QueueMessageUpdateReceipt
            {
                PopReceipt = "initial-receipt"
            };

            receipt.PopReceipt = null;

            receipt.PopReceipt.Should().BeNull();
        }

        /// <summary>
        /// Tests that PopReceipt can be set to empty string.
        /// </summary>
        [TestMethod]
        public void PopReceipt_WhenSetToEmptyString_ShouldAcceptEmptyString()
        {
            var receipt = new QueueMessageUpdateReceipt();

            receipt.PopReceipt = "";

            receipt.PopReceipt.Should().BeEmpty();
        }

        /// <summary>
        /// Tests that PopReceipt can be set to whitespace.
        /// </summary>
        [TestMethod]
        public void PopReceipt_WhenSetToWhitespace_ShouldAcceptWhitespace()
        {
            var receipt = new QueueMessageUpdateReceipt();
            var whitespace = "   \t\n  ";

            receipt.PopReceipt = whitespace;

            receipt.PopReceipt.Should().Be(whitespace);
        }

        /// <summary>
        /// Tests that PopReceipt can handle very long strings.
        /// </summary>
        [TestMethod]
        public void PopReceipt_WhenSetToVeryLongString_ShouldHandleCorrectly()
        {
            var receipt = new QueueMessageUpdateReceipt();
            var longReceipt = new string('a', 10000);

            receipt.PopReceipt = longReceipt;

            receipt.PopReceipt.Should().Be(longReceipt);
            receipt.PopReceipt.Should().HaveLength(10000);
        }

        /// <summary>
        /// Tests that PopReceipt can contain special characters.
        /// </summary>
        [TestMethod]
        public void PopReceipt_WhenSetToSpecialCharacters_ShouldHandleCorrectly()
        {
            var receipt = new QueueMessageUpdateReceipt();
            var specialReceipt = "receipt-with-unicode-🚀-and-symbols-@#$%^&*()";

            receipt.PopReceipt = specialReceipt;

            receipt.PopReceipt.Should().Be(specialReceipt);
            receipt.PopReceipt.Should().Contain("🚀");
        }

        #endregion

        #region Integration Tests

        /// <summary>
        /// Tests that all properties work correctly together.
        /// </summary>
        [TestMethod]
        public void QueueMessageUpdateReceipt_WhenSettingMultipleProperties_ShouldMaintainAllValues()
        {
            var receipt = new QueueMessageUpdateReceipt();
            var expectedTime = DateTimeOffset.UtcNow.AddMinutes(10);
            var expectedReceipt = "integration-test-receipt";

            receipt.NextVisibleOn = expectedTime;
            receipt.PopReceipt = expectedReceipt;

            receipt.NextVisibleOn.Should().Be(expectedTime);
            receipt.PopReceipt.Should().Be(expectedReceipt);
        }

        /// <summary>
        /// Tests that properties remain independent when modified.
        /// </summary>
        [TestMethod]
        public void QueueMessageUpdateReceipt_WhenModifyingOneProperty_ShouldNotAffectOthers()
        {
            var receipt = new QueueMessageUpdateReceipt();
            var originalTime = DateTimeOffset.UtcNow.AddMinutes(5);
            var originalReceipt = "original-receipt";

            receipt.NextVisibleOn = originalTime;
            receipt.PopReceipt = originalReceipt;

            receipt.NextVisibleOn = DateTimeOffset.UtcNow.AddMinutes(15);

            receipt.PopReceipt.Should().Be(originalReceipt);
        }

        /// <summary>
        /// Tests that multiple receipts can be created independently.
        /// </summary>
        [TestMethod]
        public void QueueMessageUpdateReceipt_WhenCreatingMultipleInstances_ShouldBeIndependent()
        {
            var receipt1 = new QueueMessageUpdateReceipt
            {
                NextVisibleOn = DateTimeOffset.UtcNow.AddMinutes(5),
                PopReceipt = "receipt-1"
            };

            var receipt2 = new QueueMessageUpdateReceipt
            {
                NextVisibleOn = DateTimeOffset.UtcNow.AddMinutes(10),
                PopReceipt = "receipt-2"
            };

            receipt1.PopReceipt.Should().Be("receipt-1");
            receipt2.PopReceipt.Should().Be("receipt-2");
            receipt1.NextVisibleOn.Should().NotBe(receipt2.NextVisibleOn);
        }

        #endregion

        #region Realistic Scenario Tests

        /// <summary>
        /// Tests a realistic scenario of updating message visibility timeout.
        /// </summary>
        [TestMethod]
        public void QueueMessageUpdateReceipt_WhenSimulatingVisibilityUpdate_ShouldReflectRealScenario()
        {
            var receipt = new QueueMessageUpdateReceipt();
            var currentTime = DateTimeOffset.UtcNow;
            var visibilityTimeout = TimeSpan.FromMinutes(5);
            var expectedVisibleTime = currentTime.Add(visibilityTimeout);
            var messageReceiptHandle = "AgQAAQABAAAAAAAAAAAAAA..."; // Simulated SQS receipt handle

            receipt.NextVisibleOn = expectedVisibleTime;
            receipt.PopReceipt = messageReceiptHandle;

            receipt.NextVisibleOn.Should().BeAfter(currentTime);
            receipt.NextVisibleOn.Should().BeCloseTo(expectedVisibleTime, TimeSpan.FromSeconds(1));
            receipt.PopReceipt.Should().NotBeNullOrWhiteSpace();
            receipt.PopReceipt.Should().StartWith("AgQAAQABAAAAAAAAAAAAAA");
        }

        /// <summary>
        /// Tests the scenario where visibility timeout is extended multiple times.
        /// </summary>
        [TestMethod]
        public void QueueMessageUpdateReceipt_WhenExtendingVisibilityMultipleTimes_ShouldUpdateCorrectly()
        {
            var receipt = new QueueMessageUpdateReceipt();
            var baseTime = DateTimeOffset.UtcNow;

            // First extension
            receipt.NextVisibleOn = baseTime.AddMinutes(5);
            receipt.PopReceipt = "first-receipt";

            var firstExtension = receipt.NextVisibleOn;

            // Second extension
            receipt.NextVisibleOn = baseTime.AddMinutes(10);
            receipt.PopReceipt = "second-receipt";

            receipt.NextVisibleOn.Should().BeAfter(firstExtension);
            receipt.PopReceipt.Should().Be("second-receipt");
        }

        #endregion

        #region Edge Cases Tests

        /// <summary>
        /// Tests behavior with UTC and local times.
        /// </summary>
        [TestMethod]
        public void NextVisibleOn_WhenUsingUtcAndLocalTimes_ShouldHandleBothCorrectly()
        {
            var receipt = new QueueMessageUpdateReceipt();
            var utcTime = DateTimeOffset.UtcNow;
            var localTime = DateTimeOffset.Now;

            receipt.NextVisibleOn = utcTime;
            receipt.NextVisibleOn.Should().Be(utcTime);

            receipt.NextVisibleOn = localTime;
            receipt.NextVisibleOn.Should().Be(localTime);
        }

        /// <summary>
        /// Tests that the receipt can handle concurrent modifications.
        /// </summary>
        [TestMethod]
        public void QueueMessageUpdateReceipt_WhenModifiedConcurrently_ShouldHandleCorrectly()
        {
            var receipt = new QueueMessageUpdateReceipt();
            var time1 = DateTimeOffset.UtcNow.AddMinutes(1);
            var time2 = DateTimeOffset.UtcNow.AddMinutes(2);

            // Simulate rapid updates (like what might happen in a high-throughput scenario)
            receipt.NextVisibleOn = time1;
            receipt.PopReceipt = "receipt-1";
            
            receipt.NextVisibleOn = time2;
            receipt.PopReceipt = "receipt-2";

            receipt.NextVisibleOn.Should().Be(time2);
            receipt.PopReceipt.Should().Be("receipt-2");
        }

        /// <summary>
        /// Tests that the receipt handles unusual but valid receipt handle formats.
        /// </summary>
        [TestMethod]
        public void PopReceipt_WhenSetToUnusualButValidFormats_ShouldAcceptValues()
        {
            var receipt = new QueueMessageUpdateReceipt();

            // Base64-like strings (common in AWS SQS)
            receipt.PopReceipt = "AgQAAQABAAAAAAAAAAAAAA==";
            receipt.PopReceipt.Should().EndWith("==");

            // GUID-like strings
            receipt.PopReceipt = "12345678-1234-1234-1234-123456789012";
            receipt.PopReceipt.Should().Contain("-");

            // JSON-like strings (escaped)
            receipt.PopReceipt = "{\"receiptHandle\":\"test\"}";
            receipt.PopReceipt.Should().Contain("receiptHandle");
        }

        #endregion

    }

}
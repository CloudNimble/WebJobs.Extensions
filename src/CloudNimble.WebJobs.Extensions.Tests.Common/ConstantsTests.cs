// Copyright (c) CloudNimble, Inc. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using CloudNimble.WebJobs.Extensions.Common;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace CloudNimble.WebJobs.Extensions.Tests.Common
{

    /// <summary>
    /// Tests for the <see cref="Constants"/> class.
    /// </summary>
    [TestClass]
    [TestCategory("Unit")]
    public class ConstantsTests
    {

        #region DateTimeFormatString Tests

        /// <summary>
        /// Tests that DateTimeFormatString has the expected value.
        /// </summary>
        [TestMethod]
        public void DateTimeFormatString_WhenAccessed_ShouldHaveExpectedValue()
        {
            Constants.DateTimeFormatString.Should().Be("yyyy'-'MM'-'dd'T'HH':'mm':'ss'.'fffK");
        }

        /// <summary>
        /// Tests that DateTimeFormatString is not null or empty.
        /// </summary>
        [TestMethod]
        public void DateTimeFormatString_WhenAccessed_ShouldNotBeNullOrEmpty()
        {
            Constants.DateTimeFormatString.Should().NotBeNullOrWhiteSpace();
        }

        /// <summary>
        /// Tests that DateTimeFormatString can format DateTime correctly.
        /// </summary>
        [TestMethod]
        public void DateTimeFormatString_WhenUsedToFormatDateTime_ShouldProduceExpectedFormat()
        {
            var testDate = new DateTime(2025, 1, 31, 14, 30, 45, 123, DateTimeKind.Utc);

            var formatted = testDate.ToString(Constants.DateTimeFormatString, CultureInfo.InvariantCulture);

            formatted.Should().Be("2025-01-31T14:30:45.123Z");
        }

        /// <summary>
        /// Tests that DateTimeFormatString can format DateTimeOffset correctly.
        /// </summary>
        [TestMethod]
        public void DateTimeFormatString_WhenUsedToFormatDateTimeOffset_ShouldProduceExpectedFormat()
        {
            var testDate = new DateTimeOffset(2025, 1, 31, 14, 30, 45, 123, TimeSpan.Zero);

            var formatted = testDate.ToString(Constants.DateTimeFormatString, CultureInfo.InvariantCulture);

            formatted.Should().Be("2025-01-31T14:30:45.123+00:00");
        }

        /// <summary>
        /// Tests that DateTimeFormatString handles different time zones correctly.
        /// </summary>
        [TestMethod]
        public void DateTimeFormatString_WhenUsedWithDifferentTimeZones_ShouldIncludeTimeZoneInfo()
        {
            var utcDate = new DateTimeOffset(2025, 1, 31, 14, 30, 45, 123, TimeSpan.Zero);
            var easternDate = new DateTimeOffset(2025, 1, 31, 9, 30, 45, 123, TimeSpan.FromHours(-5));
            var pacificDate = new DateTimeOffset(2025, 1, 31, 6, 30, 45, 123, TimeSpan.FromHours(-8));

            var utcFormatted = utcDate.ToString(Constants.DateTimeFormatString, CultureInfo.InvariantCulture);
            var easternFormatted = easternDate.ToString(Constants.DateTimeFormatString, CultureInfo.InvariantCulture);
            var pacificFormatted = pacificDate.ToString(Constants.DateTimeFormatString, CultureInfo.InvariantCulture);

            utcFormatted.Should().Be("2025-01-31T14:30:45.123+00:00");
            easternFormatted.Should().Be("2025-01-31T09:30:45.123-05:00");
            pacificFormatted.Should().Be("2025-01-31T06:30:45.123-08:00");
        }

        /// <summary>
        /// Tests that DateTimeFormatString is compatible with ISO 8601 standard.
        /// </summary>
        [TestMethod]
        public void DateTimeFormatString_WhenUsed_ShouldProduceIso8601CompatibleFormat()
        {
            var testDate = new DateTimeOffset(2025, 12, 31, 23, 59, 59, 999, TimeSpan.FromHours(2));

            var formatted = testDate.ToString(Constants.DateTimeFormatString, CultureInfo.InvariantCulture);

            // Should be parseable by standard ISO 8601 parsers
            var parsed = DateTimeOffset.Parse(formatted, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);
            parsed.Should().Be(testDate);
        }

        /// <summary>
        /// Tests that formatted dates can be parsed back correctly.
        /// </summary>
        [TestMethod]
        public void DateTimeFormatString_WhenUsedForRoundtripFormatting_ShouldMaintainPrecision()
        {
            var originalDates = new[]
            {
                new DateTimeOffset(2025, 1, 1, 0, 0, 0, 0, TimeSpan.Zero),
                new DateTimeOffset(2025, 6, 15, 12, 30, 45, 123, TimeSpan.FromHours(-5)),
                new DateTimeOffset(2025, 12, 31, 23, 59, 59, 999, TimeSpan.FromHours(8))
            };

            foreach (var originalDate in originalDates)
            {
                var formatted = originalDate.ToString(Constants.DateTimeFormatString, CultureInfo.InvariantCulture);
                var parsed = DateTimeOffset.ParseExact(formatted, Constants.DateTimeFormatString, CultureInfo.InvariantCulture);

                parsed.Should().Be(originalDate);
            }
        }

        #endregion

        #region AzureWebJobsStorage Tests

        /// <summary>
        /// Tests that AzureWebJobsStorage has the expected value.
        /// </summary>
        [TestMethod]
        public void AzureWebJobsStorage_WhenAccessed_ShouldHaveExpectedValue()
        {
            Constants.AzureWebJobsStorage.Should().Be("AzureWebJobsStorage");
        }

        /// <summary>
        /// Tests that AzureWebJobsStorage is not null or empty.
        /// </summary>
        [TestMethod]
        public void AzureWebJobsStorage_WhenAccessed_ShouldNotBeNullOrEmpty()
        {
            Constants.AzureWebJobsStorage.Should().NotBeNullOrWhiteSpace();
        }

        /// <summary>
        /// Tests that AzureWebJobsStorage matches the standard Azure WebJobs setting name.
        /// </summary>
        [TestMethod]
        public void AzureWebJobsStorage_WhenAccessed_ShouldMatchStandardSettingName()
        {
            // This is the standard configuration key used by Azure WebJobs
            Constants.AzureWebJobsStorage.Should().Be("AzureWebJobsStorage");
        }

        #endregion

        #region Constants Class Tests

        /// <summary>
        /// Tests that Constants class is static and internal.
        /// </summary>
        [TestMethod]
        public void Constants_WhenExamined_ShouldBeStaticAndInternal()
        {
            var constantsType = typeof(Constants);

            constantsType.IsClass.Should().BeTrue();
            constantsType.IsAbstract.Should().BeTrue(); // Static classes are abstract
            constantsType.IsSealed.Should().BeTrue();   // Static classes are sealed
        }

        /// <summary>
        /// Tests that Constants class cannot be instantiated.
        /// </summary>
        [TestMethod]
        public void Constants_WhenAttemptingToInstantiate_ShouldNotBePossible()
        {
            var constantsType = typeof(Constants);
            var constructors = constantsType.GetConstructors();

            // Static classes have no public constructors
            constructors.Should().BeEmpty();
        }

        /// <summary>
        /// Tests that all constants are accessible via reflection.
        /// </summary>
        [TestMethod]
        public void Constants_WhenAccessedViaReflection_ShouldExposeExpectedFields()
        {
            var constantsType = typeof(Constants);
            var fields = constantsType.GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);

            fields.Should().HaveCountGreaterOrEqualTo(2);
            
            var fieldNames = fields.Select(f => f.Name).ToArray();
            fieldNames.Should().Contain("DateTimeFormatString");
            fieldNames.Should().Contain("AzureWebJobsStorage");
        }

        /// <summary>
        /// Tests that all constant fields are indeed constants (not readonly).
        /// </summary>
        [TestMethod]
        public void Constants_WhenFieldsExamined_ShouldBeConstants()
        {
            var constantsType = typeof(Constants);
            var fields = constantsType.GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);

            foreach (var field in fields)
            {
                field.IsLiteral.Should().BeTrue($"Field {field.Name} should be a constant");
                field.IsStatic.Should().BeTrue($"Field {field.Name} should be static");
            }
        }

        #endregion

        #region Usage Scenario Tests

        /// <summary>
        /// Tests DateTimeFormatString usage in logging scenarios.
        /// </summary>
        [TestMethod]
        public void DateTimeFormatString_WhenUsedInLoggingScenario_ShouldProduceReadableOutput()
        {
            var timestamp = DateTimeOffset.UtcNow;
            var logMessage = $"Event occurred at: {timestamp.ToString(Constants.DateTimeFormatString, CultureInfo.InvariantCulture)}";

            logMessage.Should().Contain("Event occurred at:");
            logMessage.Should().MatchRegex(@"\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}\.\d{3}[Z\+\-]\d{0,2}:?\d{0,2}");
        }

        /// <summary>
        /// Tests AzureWebJobsStorage usage in configuration scenarios.
        /// </summary>
        [TestMethod]
        public void AzureWebJobsStorage_WhenUsedInConfigurationScenario_ShouldWorkAsConfigurationKey()
        {
            var configDictionary = new Dictionary<string, string>
            {
                { Constants.AzureWebJobsStorage, "DefaultEndpointsProtocol=https;AccountName=test;AccountKey=key;EndpointSuffix=core.windows.net" }
            };

            configDictionary.Should().ContainKey(Constants.AzureWebJobsStorage);
            configDictionary[Constants.AzureWebJobsStorage].Should().Contain("AccountName=test");
        }

        #endregion

        #region Edge Case Tests

        /// <summary>
        /// Tests DateTimeFormatString with edge case dates.
        /// </summary>
        [TestMethod]
        public void DateTimeFormatString_WhenUsedWithEdgeCaseDates_ShouldHandleCorrectly()
        {
            var edgeCases = new[]
            {
                DateTimeOffset.MinValue,
                DateTimeOffset.MaxValue,
                new DateTimeOffset(2000, 1, 1, 0, 0, 0, 0, TimeSpan.Zero), // Y2K
                new DateTimeOffset(2038, 1, 19, 3, 14, 7, 0, TimeSpan.Zero) // Unix timestamp edge
            };

            foreach (var edgeCase in edgeCases)
            {
                var action = () => edgeCase.ToString(Constants.DateTimeFormatString, CultureInfo.InvariantCulture);
                action.Should().NotThrow($"Date {edgeCase} should format without throwing");
                
                var formatted = action();
                formatted.Should().NotBeNullOrWhiteSpace();
            }
        }

        /// <summary>
        /// Tests that constants work correctly with different cultures.
        /// </summary>
        [TestMethod]
        public void Constants_WhenUsedWithDifferentCultures_ShouldBeConsistent()
        {
            var testDate = new DateTimeOffset(2025, 1, 31, 14, 30, 45, 123, TimeSpan.Zero);
            var cultures = new[]
            {
                CultureInfo.InvariantCulture,
                CultureInfo.GetCultureInfo("en-US"),
                CultureInfo.GetCultureInfo("fr-FR"),
                CultureInfo.GetCultureInfo("ja-JP")
            };

            var expectedFormat = "2025-01-31T14:30:45.123+00:00";
            
            foreach (var culture in cultures)
            {
                var formatted = testDate.ToString(Constants.DateTimeFormatString, culture);
                formatted.Should().Be(expectedFormat, $"Format should be consistent across cultures, but failed for {culture.Name}");
            }
        }

        /// <summary>
        /// Tests that constants have the expected string lengths for optimization purposes.
        /// </summary>
        [TestMethod]
        public void Constants_WhenExaminingStringLengths_ShouldHaveExpectedLengths()
        {
            Constants.DateTimeFormatString.Should().HaveLength(36);
            Constants.AzureWebJobsStorage.Should().HaveLength(19);
        }

        #endregion

        #region Integration Tests

        /// <summary>
        /// Tests that constants can be used together in realistic scenarios.
        /// </summary>
        [TestMethod]
        public void Constants_WhenUsedTogether_ShouldWorkInRealisticScenarios()
        {
            var timestamp = DateTimeOffset.UtcNow;
            var formattedTimestamp = timestamp.ToString(Constants.DateTimeFormatString, CultureInfo.InvariantCulture);
            
            var diagnosticInfo = new Dictionary<string, object>
            {
                { "timestamp", formattedTimestamp },
                { "storageConnectionKey", Constants.AzureWebJobsStorage },
                { "event", "Queue processing started" }
            };

            diagnosticInfo.Should().ContainKey("timestamp");
            diagnosticInfo.Should().ContainKey("storageConnectionKey");
            diagnosticInfo["storageConnectionKey"].Should().Be("AzureWebJobsStorage");
            diagnosticInfo["timestamp"].ToString().Should().MatchRegex(@"\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}\.\d{3}[Z\+\-]\d{0,2}:?\d{0,2}");
        }

        #endregion

    }

}
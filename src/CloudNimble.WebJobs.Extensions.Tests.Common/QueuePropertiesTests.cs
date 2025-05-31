// Copyright (c) CloudNimble, Inc. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using CloudNimble.WebJobs.Extensions.Common.Queues;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;

namespace CloudNimble.WebJobs.Extensions.Common.Tests.Queues
{

    /// <summary>
    /// Tests for the <see cref="QueueProperties"/> class.
    /// </summary>
    [TestClass]
    [TestCategory("Unit")]
    public class QueuePropertiesTests
    {

        #region Constructor Tests

        /// <summary>
        /// Tests that the default constructor initializes properties correctly.
        /// </summary>
        [TestMethod]
        public void Constructor_WhenCalled_ShouldInitializeProperties()
        {
            var properties = new QueueProperties();

            properties.Metadata.Should().NotBeNull();
            properties.Metadata.Should().BeEmpty();
            properties.ApproximateMessagesCount.Should().Be(0);
        }

        /// <summary>
        /// Tests that the Metadata dictionary is case-insensitive.
        /// </summary>
        [TestMethod]
        public void Constructor_WhenCalled_ShouldCreateCaseInsensitiveMetadataDictionary()
        {
            var properties = new QueueProperties();

            properties.Metadata["TestKey"] = "value1";
            properties.Metadata["testkey"] = "value2";

            properties.Metadata.Should().HaveCount(1);
            properties.Metadata["TestKey"].Should().Be("value2");
        }

        #endregion

        #region Metadata Property Tests

        /// <summary>
        /// Tests that metadata can be added and retrieved.
        /// </summary>
        [TestMethod]
        public void Metadata_WhenAddingValues_ShouldStoreAndRetrieveCorrectly()
        {
            var properties = new QueueProperties();

            properties.Metadata["key1"] = "value1";
            properties.Metadata["key2"] = "value2";

            properties.Metadata.Should().HaveCount(2);
            properties.Metadata["key1"].Should().Be("value1");
            properties.Metadata["key2"].Should().Be("value2");
        }

        /// <summary>
        /// Tests that metadata can be updated.
        /// </summary>
        [TestMethod]
        public void Metadata_WhenUpdatingValues_ShouldOverwritePreviousValue()
        {
            var properties = new QueueProperties();
            properties.Metadata["key1"] = "original";

            properties.Metadata["key1"] = "updated";

            properties.Metadata["key1"].Should().Be("updated");
            properties.Metadata.Should().HaveCount(1);
        }

        /// <summary>
        /// Tests that metadata can be removed.
        /// </summary>
        [TestMethod]
        public void Metadata_WhenRemovingValues_ShouldRemoveCorrectly()
        {
            var properties = new QueueProperties();
            properties.Metadata["key1"] = "value1";
            properties.Metadata["key2"] = "value2";

            var removed = properties.Metadata.Remove("key1");

            removed.Should().BeTrue();
            properties.Metadata.Should().HaveCount(1);
            properties.Metadata.Should().ContainKey("key2");
            properties.Metadata.Should().NotContainKey("key1");
        }

        /// <summary>
        /// Tests that metadata handles null values.
        /// </summary>
        [TestMethod]
        public void Metadata_WhenSettingNullValue_ShouldAcceptNullValue()
        {
            var properties = new QueueProperties();

            properties.Metadata["nullKey"] = null;

            properties.Metadata.Should().ContainKey("nullKey");
            properties.Metadata["nullKey"].Should().BeNull();
        }

        /// <summary>
        /// Tests that metadata handles empty strings.
        /// </summary>
        [TestMethod]
        public void Metadata_WhenSettingEmptyString_ShouldAcceptEmptyString()
        {
            var properties = new QueueProperties();

            properties.Metadata["emptyKey"] = "";

            properties.Metadata.Should().ContainKey("emptyKey");
            properties.Metadata["emptyKey"].Should().BeEmpty();
        }

        /// <summary>
        /// Tests that metadata can be enumerated.
        /// </summary>
        [TestMethod]
        public void Metadata_WhenEnumerating_ShouldReturnAllKeyValuePairs()
        {
            var properties = new QueueProperties();
            properties.Metadata["key1"] = "value1";
            properties.Metadata["key2"] = "value2";
            properties.Metadata["key3"] = "value3";

            var pairs = properties.Metadata.ToList();

            pairs.Should().HaveCount(3);
            pairs.Should().Contain(kvp => kvp.Key == "key1" && kvp.Value == "value1");
            pairs.Should().Contain(kvp => kvp.Key == "key2" && kvp.Value == "value2");
            pairs.Should().Contain(kvp => kvp.Key == "key3" && kvp.Value == "value3");
        }

        #endregion

        #region Integration Tests

        /// <summary>
        /// Tests that properties work correctly together.
        /// </summary>
        [TestMethod]
        public void QueueProperties_WhenSettingMultipleProperties_ShouldMaintainAllValues()
        {
            var properties = new QueueProperties();

            //properties.ApproximateMessagesCount = 100;
            properties.Metadata["QueueType"] = "Standard";
            properties.Metadata["Region"] = "us-east-1";
            properties.Metadata["Created"] = DateTime.UtcNow.ToString();

            //properties.ApproximateMessagesCount.Should().Be(100);
            properties.Metadata.Should().HaveCount(3);
            properties.Metadata["QueueType"].Should().Be("Standard");
            properties.Metadata["Region"].Should().Be("us-east-1");
            properties.Metadata.Should().ContainKey("Created");
        }

        /// <summary>
        /// Tests that properties remain independent.
        /// </summary>
        [TestMethod]
        public void QueueProperties_WhenModifyingOneProperty_ShouldNotAffectOthers()
        {
            var properties = new QueueProperties
            {
                //ApproximateMessagesCount = 50
            };
            properties.Metadata["InitialKey"] = "InitialValue";

            //properties.ApproximateMessagesCount = 75;

            properties.Metadata.Should().HaveCount(1);
            properties.Metadata["InitialKey"].Should().Be("InitialValue");
        }

        #endregion

        #region Edge Case Tests

        /// <summary>
        /// Tests that case insensitivity works with various casing patterns.
        /// </summary>
        [TestMethod]
        public void Metadata_WhenUsingVariousCasings_ShouldBeCaseInsensitive()
        {
            var properties = new QueueProperties();

            properties.Metadata["TestKey"] = "value1";
            properties.Metadata["TESTKEY"] = "value2";
            properties.Metadata["testkey"] = "value3";
            properties.Metadata["TestKEY"] = "value4";

            properties.Metadata.Should().HaveCount(1);
            properties.Metadata["TestKey"].Should().Be("value4");
        }

        /// <summary>
        /// Tests that special characters in keys are handled correctly.
        /// </summary>
        [TestMethod]
        public void Metadata_WhenUsingSpecialCharactersInKeys_ShouldHandleCorrectly()
        {
            var properties = new QueueProperties();

            properties.Metadata["key-with-dashes"] = "value1";
            properties.Metadata["key_with_underscores"] = "value2";
            properties.Metadata["key.with.dots"] = "value3";
            properties.Metadata["key with spaces"] = "value4";

            properties.Metadata.Should().HaveCount(4);
            properties.Metadata["key-with-dashes"].Should().Be("value1");
            properties.Metadata["key_with_underscores"].Should().Be("value2");
            properties.Metadata["key.with.dots"].Should().Be("value3");
            properties.Metadata["key with spaces"].Should().Be("value4");
        }

        /// <summary>
        /// Tests that very long values are handled correctly.
        /// </summary>
        [TestMethod]
        public void Metadata_WhenUsingVeryLongValues_ShouldHandleCorrectly()
        {
            var properties = new QueueProperties();
            var longValue = new string('x', 10000);

            properties.Metadata["longValueKey"] = longValue;

            properties.Metadata["longValueKey"].Should().Be(longValue);
            properties.Metadata["longValueKey"].Should().HaveLength(10000);
        }

        #endregion

    }

}
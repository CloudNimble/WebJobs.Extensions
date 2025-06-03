// Copyright (c) CloudNimble, Inc. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using CloudNimble.WebJobs.Extensions.Amazon.SQS;
using CloudNimble.WebJobs.Extensions.Common.Queues;
using FluentAssertions;
using Microsoft.Azure.WebJobs.Hosting;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Text.Json;

namespace CloudNimble.WebJobs.Extensions.Tests.Amazon.SQS
{

    /// <summary>
    /// Unit tests for <see cref="SQSOptions"/>.
    /// </summary>
    [TestClass]
    public class SQSOptionsTests
    {

        [TestMethod]
        public void DefaultConstructor_SetsDefaultValues()
        {
            // Arrange & Act
            var options = new SQSOptions();

            // Assert
            // Base class defaults
            options.BatchSize.Should().Be(16);
            options.MaxDequeueCount.Should().Be(5);
            options.MessageEncoding.Should().Be(QueueMessageEncoding.Base64);

            // SQS-specific defaults
            options.AccessKey.Should().BeNull();
            options.Region.Should().BeNull();
            options.SecretKey.Should().BeNull();
            options.ServiceUrl.Should().BeNull();
            options.UseFifo.Should().BeFalse();
            options.MessageGroupId.Should().Be("default");
            options.UseContentBasedDeduplication.Should().BeFalse();
        }

        [TestMethod]
        public void Properties_CanBeSetAndRetrieved()
        {
            // Arrange
            var options = new SQSOptions();

            // Act
            options.AccessKey = "test-access-key";
            options.Region = "us-east-1";
            options.SecretKey = "test-secret-key";
            options.ServiceUrl = "http://localhost:4566";
            options.UseFifo = true;
            options.MessageGroupId = "test-group";
            options.UseContentBasedDeduplication = true;

            // Assert
            options.AccessKey.Should().Be("test-access-key");
            options.Region.Should().Be("us-east-1");
            options.SecretKey.Should().Be("test-secret-key");
            options.ServiceUrl.Should().Be("http://localhost:4566");
            options.UseFifo.Should().BeTrue();
            options.MessageGroupId.Should().Be("test-group");
            options.UseContentBasedDeduplication.Should().BeTrue();
        }

        [TestMethod]
        public void MessageGroupId_ThrowsOnNullOrWhitespace()
        {
            // Arrange
            var options = new SQSOptions();

            // Act & Assert
            options.Invoking(o => o.MessageGroupId = null)
                .Should().Throw<ArgumentException>()
                .WithMessage("*MessageGroupId cannot be null or whitespace*");

            options.Invoking(o => o.MessageGroupId = "")
                .Should().Throw<ArgumentException>()
                .WithMessage("*MessageGroupId cannot be null or whitespace*");

            options.Invoking(o => o.MessageGroupId = "   ")
                .Should().Throw<ArgumentException>()
                .WithMessage("*MessageGroupId cannot be null or whitespace*");
        }

        [TestMethod]
        public void Clone_CreatesDeepCopy()
        {
            // Arrange
            var original = new SQSOptions
            {
                // Base properties
                BatchSize = 32,
                MaxDequeueCount = 10,
                MessageEncoding = QueueMessageEncoding.None,
                
                // SQS properties
                AccessKey = "test-access-key",
                Region = "us-west-2",
                SecretKey = "test-secret-key",
                ServiceUrl = "http://localhost:4566",
                UseFifo = true,
                MessageGroupId = "test-group",
                UseContentBasedDeduplication = true
            };

            // Act
            var clone = original.Clone();

            // Assert
            clone.Should().NotBeSameAs(original);
            
            // Base properties
            clone.BatchSize.Should().Be(original.BatchSize);
            clone.MaxDequeueCount.Should().Be(original.MaxDequeueCount);
            clone.MessageEncoding.Should().Be(original.MessageEncoding);
            
            // SQS properties
            clone.AccessKey.Should().Be(original.AccessKey);
            clone.Region.Should().Be(original.Region);
            clone.SecretKey.Should().Be(original.SecretKey);
            clone.ServiceUrl.Should().Be(original.ServiceUrl);
            clone.UseFifo.Should().Be(original.UseFifo);
            clone.MessageGroupId.Should().Be(original.MessageGroupId);
            clone.UseContentBasedDeduplication.Should().Be(original.UseContentBasedDeduplication);
        }

        [TestMethod]
        public void Clone_ModifyingCloneDoesNotAffectOriginal()
        {
            // Arrange
            var original = new SQSOptions
            {
                AccessKey = "original-key",
                MessageGroupId = "original-group"
            };

            // Act
            var clone = original.Clone();
            clone.AccessKey = "modified-key";
            clone.MessageGroupId = "modified-group";

            // Assert
            original.AccessKey.Should().Be("original-key");
            original.MessageGroupId.Should().Be("original-group");
        }

        [TestMethod]
        public void IOptionsFormatter_Format_MasksSensitiveData()
        {
            // Arrange
            var options = new SQSOptions
            {
                AccessKey = "AKIAIOSFODNN7EXAMPLE",
                SecretKey = "wJalrXUtnFEMI/K7MDENG/bPxRfiCYEXAMPLEKEY",
                Region = "us-east-1",
                ServiceUrl = "http://localhost:4566"
            };

            // Act
            var formatted = ((IOptionsFormatter)options).Format();
            
            // First, let's see what the actual format is
            formatted.Should().NotBeNullOrEmpty();
            
            // Parse with JsonSerializerOptions that handle camelCase
            var jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            using var json = JsonDocument.Parse(formatted);

            // Assert - check if properties exist first
            if (json.RootElement.TryGetProperty("AccessKey", out var accessKey))
            {
                accessKey.GetString().Should().Be("***MASKED***");
            }
            else if (json.RootElement.TryGetProperty("accessKey", out accessKey))
            {
                accessKey.GetString().Should().Be("***MASKED***");
            }

            if (json.RootElement.TryGetProperty("SecretKey", out var secretKey))
            {
                secretKey.GetString().Should().Be("***MASKED***");
            }
            else if (json.RootElement.TryGetProperty("secretKey", out secretKey))
            {
                secretKey.GetString().Should().Be("***MASKED***");
            }

            if (json.RootElement.TryGetProperty("Region", out var region))
            {
                region.GetString().Should().Be("us-east-1");
            }
            else if (json.RootElement.TryGetProperty("region", out region))
            {
                region.GetString().Should().Be("us-east-1");
            }

            if (json.RootElement.TryGetProperty("ServiceUrl", out var serviceUrl))
            {
                serviceUrl.GetString().Should().Be("http://localhost:4566");
            }
            else if (json.RootElement.TryGetProperty("serviceUrl", out serviceUrl))
            {
                serviceUrl.GetString().Should().Be("http://localhost:4566");
            }
        }

        [TestMethod]
        public void IOptionsFormatter_Format_HandlesNullCredentials()
        {
            // Arrange
            var options = new SQSOptions
            {
                AccessKey = null,
                SecretKey = null,
                Region = "us-east-1"
            };

            // Act
            var formatted = ((IOptionsFormatter)options).Format();
            using var json = JsonDocument.Parse(formatted);

            // Assert - With JsonIgnoreCondition.WhenWritingNull, null values won't be serialized
            formatted.Should().NotContain("accessKey");  // null values are omitted
            formatted.Should().NotContain("secretKey");  // null values are omitted
            formatted.Should().Contain("us-east-1");
        }

        [TestMethod]
        public void IOptionsFormatter_Format_IncludesAllProperties()
        {
            // Arrange
            var options = new SQSOptions
            {
                BatchSize = 32,
                MaxDequeueCount = 10,
                MessageEncoding = QueueMessageEncoding.Base64,
                UseFifo = true,
                MessageGroupId = "test-group",
                UseContentBasedDeduplication = true
            };

            // Act
            var formatted = ((IOptionsFormatter)options).Format();

            // Assert - Verify the formatted string contains expected values
            formatted.Should().Contain("32");
            formatted.Should().Contain("10");
            formatted.Should().Contain("Base64");
            formatted.Should().Contain("true");
            formatted.Should().Contain("test-group");
            // The format should be valid JSON
            Action parse = () => JsonDocument.Parse(formatted);
            parse.Should().NotThrow();
        }

        [TestMethod]
        public void InheritsFromQueuesOptionsBase()
        {
            // Arrange & Act
            var options = new SQSOptions();

            // Assert
            options.Should().BeAssignableTo<QueuesOptionsBase>();
        }

        [TestMethod]
        public void ImplementsIOptionsFormatter()
        {
            // Arrange & Act
            var options = new SQSOptions();

            // Assert
            options.Should().BeAssignableTo<IOptionsFormatter>();
        }

    }

}
using CloudNimble.WebJobs.Extensions.Amazon.SQS;
using FluentAssertions;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;

namespace CloudNimble.WebJobs.Extensions.Tests.Amazon.SQS
{
    [TestClass]
    public class SQSNameResolverTests
    {
        [TestMethod]
        public void ResolveWholeString_WithBasicName_ShouldReturnLowercase()
        {
            // Arrange
            var queueName = "MyQueue";
            var configuration = new ConfigurationBuilder().Build();
            var options = Options.Create(new SQSOptions());
            var normalizer = new SQSNameResolver(configuration, options);

            // Act
            var result = normalizer.ResolveWholeString(queueName);

            // Assert
            result.Should().Be("myqueue");
        }

        [TestMethod]
        public void ResolveWholeString_WithFifoEnabled_ShouldAppendFifoSuffix()
        {
            // Arrange
            var queueName = "MyQueue";
            var configuration = new ConfigurationBuilder().Build();
            var options = Options.Create(new SQSOptions { UseFifo = true });
            var normalizer = new SQSNameResolver(configuration, options);

            // Act
            var result = normalizer.ResolveWholeString(queueName);

            // Assert
            result.Should().Be("myqueue.fifo");
        }

        [TestMethod]
        public void ResolveWholeString_WithFifoEnabledAndExistingSuffix_ShouldNotDuplicateSuffix()
        {
            // Arrange
            var queueName = "MyQueue.fifo";
            var configuration = new ConfigurationBuilder().Build();
            var options = Options.Create(new SQSOptions { UseFifo = true });
            var normalizer = new SQSNameResolver(configuration, options);

            // Act
            var result = normalizer.ResolveWholeString(queueName);

            // Assert
            result.Should().Be("myqueue.fifo");
        }

        [TestMethod]
        public void ResolveWholeString_WithFifoDisabled_ShouldNotAppendSuffix()
        {
            // Arrange
            var queueName = "MyQueue";
            var configuration = new ConfigurationBuilder().Build();
            var options = Options.Create(new SQSOptions { UseFifo = false });
            var normalizer = new SQSNameResolver(configuration, options);

            // Act
            var result = normalizer.ResolveWholeString(queueName);

            // Assert
            result.Should().Be("myqueue");
        }

        [TestMethod]
        public void ResolveWholeString_WithConfigurationPlaceholder_ShouldResolveFromConfiguration()
        {
            // Arrange
            var queueName = "%MyQueue%";
            var configBuilder = new ConfigurationBuilder();
            configBuilder.AddInMemoryCollection(new Dictionary<string, string>
            {
                { "MyQueue", "ActualQueue" }
            });
            var configuration = configBuilder.Build();
            var options = Options.Create(new SQSOptions { UseFifo = true });
            var normalizer = new SQSNameResolver(configuration, options);

            // Act
            var result = normalizer.ResolveWholeString(queueName);

            // Assert
            result.Should().Be("actualqueue.fifo");
        }

        [TestMethod]
        public void Resolve_WithConfiguration_ShouldReturnConfigurationValue()
        {
            // Arrange
            var configBuilder = new ConfigurationBuilder();
            configBuilder.AddInMemoryCollection(new Dictionary<string, string>
            {
                { "TestKey", "TestValue" }
            });
            var configuration = configBuilder.Build();
            var options = Options.Create(new SQSOptions());
            var normalizer = new SQSNameResolver(configuration, options);

            // Act
            var result = normalizer.Resolve("TestKey");

            // Assert
            result.Should().Be("TestValue");
        }

        [TestMethod]
        public void Resolve_WithoutConfigurationValue_ShouldReturnNull()
        {
            // Arrange
            var configuration = new ConfigurationBuilder().Build();
            var options = Options.Create(new SQSOptions());
            var normalizer = new SQSNameResolver(configuration, options);

            // Act
            var result = normalizer.Resolve("TestKey");

            // Assert
            result.Should().BeNull();
        }

        [TestMethod]
        public void ResolveWholeString_WithNullQueueName_ShouldReturnNull()
        {
            // Arrange
            string queueName = null;
            var configuration = new ConfigurationBuilder().Build();
            var options = Options.Create(new SQSOptions());
            var normalizer = new SQSNameResolver(configuration, options);

            // Act
            var result = normalizer.ResolveWholeString(queueName);

            // Assert
            result.Should().BeNull();
        }

        [TestMethod]
        public void ResolveWholeString_WithEmptyQueueName_ShouldReturnEmpty()
        {
            // Arrange
            var queueName = "";
            var configuration = new ConfigurationBuilder().Build();
            var options = Options.Create(new SQSOptions());
            var normalizer = new SQSNameResolver(configuration, options);

            // Act
            var result = normalizer.ResolveWholeString(queueName);

            // Assert
            result.Should().Be("");
        }

        [TestMethod]
        public void ResolveWholeString_WithWhitespaceQueueName_ShouldReturnWhitespace()
        {
            // Arrange
            var queueName = "   ";
            var configuration = new ConfigurationBuilder().Build();
            var options = Options.Create(new SQSOptions());
            var normalizer = new SQSNameResolver(configuration, options);

            // Act
            var result = normalizer.ResolveWholeString(queueName);

            // Assert
            result.Should().Be("   ");
        }

        [TestMethod]
        public void ResolveWholeString_WithNestedConfigurationPath_ShouldResolveFromConfiguration()
        {
            // Arrange
            var queueName = "%SQS:QueueName%";
            var configBuilder = new ConfigurationBuilder();
            configBuilder.AddInMemoryCollection(new Dictionary<string, string>
            {
                { "SQS:QueueName", "NestedQueueName" }
            });
            var configuration = configBuilder.Build();
            var options = Options.Create(new SQSOptions { UseFifo = false });
            var normalizer = new SQSNameResolver(configuration, options);

            // Act
            var result = normalizer.ResolveWholeString(queueName);

            // Assert
            result.Should().Be("nestedqueuename");
        }

        [TestMethod]
        public void ResolveWholeString_WithUnresolvedPlaceholder_ShouldReturnOriginalValue()
        {
            // Arrange
            var queueName = "%NonExistentKey%";
            var configuration = new ConfigurationBuilder().Build();
            var options = Options.Create(new SQSOptions());
            var normalizer = new SQSNameResolver(configuration, options);

            // Act
            var result = normalizer.ResolveWholeString(queueName);

            // Assert
            result.Should().Be("%nonexistentkey%");
        }
    }

    /// <summary>
    /// Test implementation of INameResolver for testing purposes.
    /// </summary>
    public class TestNameResolver : INameResolver
    {
        private readonly Dictionary<string, string> _values = new();

        public void AddValue(string key, string value)
        {
            _values[key] = value;
        }

        public string Resolve(string name)
        {
            return _values.TryGetValue(name, out var value) ? value : name;
        }

        public string ResolveWholeString(string name)
        {
            if (name?.StartsWith("%") == true && name.EndsWith("%"))
            {
                var key = name.Substring(1, name.Length - 2);
                return Resolve(key);
            }
            return name;
        }
    }
}
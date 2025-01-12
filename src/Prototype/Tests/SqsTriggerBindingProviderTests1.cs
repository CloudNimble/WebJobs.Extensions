// SqsListenerTests.cs
using FluentAssertions;
using Microsoft.Azure.WebJobs.Host.Triggers;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Threading.Tasks;
using System.Threading;
using Amazon.SQS;
using Microsoft.Extensions.Logging;
using CloudNimble.WebJobs.Extensions.Amazon.SQS.AI;
using CloudNimble.WebJobs.Extensions.Common.Queues;


// SqsListenerTests.cs

namespace CloudNimble.WebJobs.Extensions.AWS.Tests
{
    // SqsTriggerBindingProviderTests.cs
    [TestClass]
    public class SqsTriggerBindingProviderTests
    {

        private readonly Mock<IAmazonSQS> _sqsClientMock;
        private readonly Mock<IOptions<SqsOptions>> _sqsOptionsMock;
        private readonly Mock<IOptions<QueuesOptionsBase>> _queueOptionsMock;
        private readonly Mock<IQueueProcessorFactory> _processorFactoryMock;
        private readonly Mock<ILogger> _loggerMock;

        public SqsTriggerBindingProviderTests()
        {
            _sqsClientMock = new Mock<IAmazonSQS>();
            _sqsOptionsMock = new Mock<IOptions<SqsOptions>>();
            _queueOptionsMock = new Mock<IOptions<QueuesOptionsBase>>();
            _processorFactoryMock = new Mock<IQueueProcessorFactory>();
            _loggerMock = new Mock<ILogger>();

            _sqsOptionsMock.Setup(x => x.Value).Returns(new SqsOptions
            {
                AccountUrl = "https://sqs.us-east-1.amazonaws.com/123456789012"
            });
            _queueOptionsMock.Setup(x => x.Value).Returns(new QueuesOptionsBase());
        }

        [TestMethod]
        public void Constructor_WhenParametersNull_ShouldThrowArgumentNullException()
        {
            // Act & Assert
            FluentActions.Invoking(() => new SqsTriggerBindingProvider(
                null,
                _sqsOptionsMock.Object,
                _queueOptionsMock.Object,
                _processorFactoryMock.Object,
                _loggerMock.Object))
                .Should().Throw<ArgumentNullException>();

            // Test other parameters similarly...
        }

        [TestMethod]
        public async Task TryCreateAsync_WhenAttributeNotPresent_ShouldReturnNull()
        {
            // Arrange
            var provider = CreateProvider();
            var context = new TriggerBindingProviderContext(
                new ParameterInfo(), // Parameter without SqsTriggerAttribute
                CancellationToken.None);

            // Act
            var binding = await provider.TryCreateAsync(context);

            // Assert
            binding.Should().BeNull();
        }

        [TestMethod]
        public async Task TryCreateAsync_WhenAttributePresent_ShouldReturnBinding()
        {
            // Arrange
            var provider = CreateProvider();
            var parameterInfo = new ParameterInfo(new SqsTriggerAttribute("test-queue"));
            var context = new TriggerBindingProviderContext(parameterInfo, CancellationToken.None);

            // Act
            var binding = await provider.TryCreateAsync(context);

            // Assert
            binding.Should().NotBeNull();
            binding.Should().BeOfType<SqsTriggerBinding>();
        }

        private SqsTriggerBindingProvider CreateProvider() => new(
            _sqsClientMock.Object,
            _sqsOptionsMock.Object,
            _queueOptionsMock.Object,
            _processorFactoryMock.Object,
            _loggerMock.Object);
    }
}
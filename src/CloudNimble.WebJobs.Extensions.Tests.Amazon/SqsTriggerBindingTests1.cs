// SqsListenerTests.cs
using FluentAssertions;
using Microsoft.Azure.WebJobs.Host.Bindings;
using Microsoft.Azure.WebJobs.Host.Executors;
using Microsoft.Azure.WebJobs.Host.Listeners;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Threading.Tasks;
using System.Threading;
using Amazon.SQS;
using Microsoft.Extensions.Logging;
using Amazon.SQS.Model;
using CloudNimble.WebJobs.Extensions.Amazon.SQS.AI;
using CloudNimble.WebJobs.Extensions.Amazon.SQS;
using CloudNimble.WebJobs.Extensions.Common.Queues;


// SqsListenerTests.cs

namespace CloudNimble.WebJobs.Extensions.AWS.Tests
{
    // SqsTriggerBindingTests.cs
    [TestClass]
    public class SqsTriggerBindingTests
    {

        private readonly Mock<IAmazonSQS> _sqsClientMock;
        private readonly Mock<IQueueProcessorFactory> _processorFactoryMock;
        private readonly Mock<IOptions<QueuesOptionsBase>> _queueOptionsMock;
        private readonly Mock<ILogger> _loggerMock;

        public SqsTriggerBindingTests()
        {
            _sqsClientMock = new Mock<IAmazonSQS>();
            _processorFactoryMock = new Mock<IQueueProcessorFactory>();
            _queueOptionsMock = new Mock<IOptions<QueuesOptionsBase>>();
            _loggerMock = new Mock<ILogger>();

            _queueOptionsMock.Setup(x => x.Value).Returns(new QueuesOptionsBase());
        }

        [TestMethod]
        public void Constructor_WhenParametersValid_ShouldInitializeCorrectly()
        {
            // Arrange & Act
            var binding = CreateBinding();

            // Assert
            binding.TriggerValueType.Should().Be(typeof(SQSMessage));
            binding.BindingDataContract.Should().NotBeNull();
        }

        [TestMethod]
        public void Constructor_WhenParametersNull_ShouldThrowArgumentNullException()
        {
            // Act & Assert
            FluentActions.Invoking(() => new SqsTriggerBinding(
                null,
                _sqsClientMock.Object,
                typeof(string),
                _queueOptionsMock.Object,
                _processorFactoryMock.Object,
                _loggerMock.Object))
                .Should().Throw<ArgumentNullException>();

            // Test other parameters similarly...
        }

        [TestMethod]
        public async Task BindAsync_WhenValueValid_ShouldReturnTriggerData()
        {
            // Arrange
            var binding = CreateBinding();
            var message = new SQSMessage(new Message { Body = "test" }, "test-queue");

            // Act
            var result = await binding.BindAsync(message, new ValueBindingContext());

            // Assert
            result.Should().NotBeNull();
            result.ValueProvider.Should().NotBeNull();
        }

        [TestMethod]
        public async Task CreateListenerAsync_WhenCalled_ShouldReturnListener()
        {
            // Arrange
            var binding = CreateBinding();
            var context = new ListenerFactoryContext(new Mock<ITriggeredFunctionExecutor>().Object, CancellationToken.None);

            // Act
            var listener = await binding.CreateListenerAsync(context);

            // Assert
            listener.Should().NotBeNull();
            listener.Should().BeOfType<SqsListener>();
        }

        private SqsTriggerBinding CreateBinding() => new(
            "test-queue",
            _sqsClientMock.Object,
            typeof(string),
            _queueOptionsMock.Object,
            _processorFactoryMock.Object,
            _loggerMock.Object);
    }
}
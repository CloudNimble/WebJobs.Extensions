// SqsListenerTests.cs
using Amazon.SQS;
using CloudNimble.WebJobs.Extensions.Amazon.SQS;
using CloudNimble.WebJobs.Extensions.Amazon.SQS.AI;
using CloudNimble.WebJobs.Extensions.Common.Queues;
using FluentAssertions;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Threading;
using System.Threading.Tasks;


// SqsQueueProcessorTests.cs
namespace CloudNimble.WebJobs.Extensions.AWS.Tests
{
    [TestClass]
    public class SqsQueueProcessorTests
    {

        private readonly Mock<IAmazonSQS> _sqsClientMock;
        private readonly Mock<IQueueProcessorFactory> _queueProcessorFactoryMock;
        private readonly Mock<IQueueProcessor> _queueProcessorMock;
        private readonly Mock<ILogger> _loggerMock;
        private readonly Mock<IOptions<QueuesOptionsBase>> _queueOptionsMock;
        private const string TestQueueUrl = "https://sqs.us-east-1.amazonaws.com/123456789012/test-queue";

        public SqsQueueProcessorTests()
        {
            _sqsClientMock = new Mock<IAmazonSQS>();
            _queueProcessorFactoryMock = new Mock<IQueueProcessorFactory>();
            _queueProcessorMock = new Mock<IQueueProcessor>();
            _loggerMock = new Mock<ILogger>();
            _queueOptionsMock = new Mock<IOptions<QueuesOptionsBase>>();

            _queueOptionsMock.Setup(x => x.Value).Returns(new QueuesOptionsBase
            {
                MaxDequeueCount = 5
            });

            _queueProcessorFactoryMock
                .Setup(x => x.Create(It.IsAny<string>()))
                .Returns(_queueProcessorMock.Object);
        }

        [TestMethod]
        public void Constructor_WhenParametersNull_ShouldThrowArgumentNullException()
        {
            // Act & Assert
            FluentActions.Invoking(() => new SqsQueueProcessor(
                null,
                _queueProcessorFactoryMock.Object,
                _queueOptionsMock.Object,
                _loggerMock.Object))
                .Should().Throw<ArgumentNullException>()
                .And.ParamName.Should().Be("sqsClient");

            FluentActions.Invoking(() => new SqsQueueProcessor(
                _sqsClientMock.Object,
                null,
                _queueOptionsMock.Object,
                _loggerMock.Object))
                .Should().Throw<ArgumentNullException>()
                .And.ParamName.Should().Be("queueProcessorFactory");

            FluentActions.Invoking(() => new SqsQueueProcessor(
                _sqsClientMock.Object,
                _queueProcessorFactoryMock.Object,
                null,
                _loggerMock.Object))
                .Should().Throw<ArgumentNullException>()
                .And.ParamName.Should().Be("queueOptions");

            FluentActions.Invoking(() => new SqsQueueProcessor(
                _sqsClientMock.Object,
                _queueProcessorFactoryMock.Object,
                _queueOptionsMock.Object,
                null))
                .Should().Throw<ArgumentNullException>()
                .And.ParamName.Should().Be("logger");
        }

        [TestMethod]
        public async Task ProcessMessageAsync_WhenSuccessful_ShouldCompleteProcessing()
        {
            // Arrange
            var processor = new SqsQueueProcessor(
                _sqsClientMock.Object,
                _queueProcessorFactoryMock.Object,
                _queueOptionsMock.Object,
                _loggerMock.Object);

            var message = CreateTestMessage();
            var processingCalled = false;

            // Act
            await processor.ProcessMessageAsync(
                message,
                _ =>
                {
                    processingCalled = true;
                    return Task.CompletedTask;
                },
                CancellationToken.None);

            // Assert
            processingCalled.Should().BeTrue();
            _queueProcessorMock.Verify(
                x => x.BeginProcessingMessageAsync(It.IsAny<QueueProcessorContext>(), It.IsAny<CancellationToken>()),
                Times.Once);
            _queueProcessorMock.Verify(
                x => x.CompleteProcessingMessageAsync(It.IsAny<QueueProcessorContext>(), null, It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [TestMethod]
        public async Task ProcessMessageAsync_WhenProcessingFails_ShouldHandleError()
        {
            // Arrange
            var processor = new SqsQueueProcessor(
                _sqsClientMock.Object,
                _queueProcessorFactoryMock.Object,
                _queueOptionsMock.Object,
                _loggerMock.Object);

            var message = CreateTestMessage();
            var testException = new Exception("Test processing error");

            // Act & Assert
            await FluentActions.Invoking(async () =>
                await processor.ProcessMessageAsync(
                    message,
                    _ => throw testException,
                    CancellationToken.None))
                .Should().ThrowAsync<Exception>()
                .WithMessage("Test processing error");

            _queueProcessorMock.Verify(
                x => x.CompleteProcessingMessageAsync(
                    It.IsAny<QueueProcessorContext>(),
                    It.Is<Exception>(e => e == testException),
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [TestMethod]
        public async Task ProcessMessageAsync_WhenMaxDequeueCountExceeded_ShouldMoveToPoisonQueue()
        {
            // Arrange
            var processor = new SqsQueueProcessor(
                _sqsClientMock.Object,
                _queueProcessorFactoryMock.Object,
                _queueOptionsMock.Object,
                _loggerMock.Object);

            var message = CreateTestMessage(dequeueCount: 6); // Exceeds MaxDequeueCount of 5
            var testException = new Exception("Test processing error");

            _sqsClientMock
                .Setup(x => x.SendMessageAsync(It.IsAny<SendMessageRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new SendMessageResponse());

            // Act & Assert
            await FluentActions.Invoking(async () =>
                await processor.ProcessMessageAsync(
                    message,
                    _ => throw testException,
                    CancellationToken.None))
                .Should().ThrowAsync<Exception>();

            _sqsClientMock.Verify(
                x => x.SendMessageAsync(
                    It.Is<SendMessageRequest>(r => r.QueueUrl.EndsWith("-poison")),
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }

        private static SQSMessage CreateTestMessage(int dequeueCount = 1)
        {
            var message = new Message
            {
                MessageId = "test-id",
                ReceiptHandle = "test-receipt",
                Body = "test-body",
                Attributes = new Dictionary<string, string>
                {
                    { "ApproximateReceiveCount", dequeueCount.ToString() }
                }
            };

            return new SqsMessage(message, TestQueueUrl);
        }
    }
}
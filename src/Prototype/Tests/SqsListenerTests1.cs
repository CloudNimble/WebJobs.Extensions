// SqsListenerTests.cs
using FluentAssertions;
using Microsoft.Azure.WebJobs.Host.Executors;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;
using Amazon.SQS;
using Microsoft.Extensions.Logging;
using Amazon.SQS.Model;
using CloudNimble.WebJobs.Extensions.Amazon.SQS.AI;
using CloudNimble.WebJobs.Extensions.Common.Queues;


// SqsListenerTests.cs

namespace CloudNimble.WebJobs.Extensions.AWS.Tests
{
    [TestClass]
    public class SqsListenerTests
    {

        private readonly Mock<IAmazonSQS> _sqsClientMock;
        private readonly Mock<ITriggeredFunctionExecutor> _executorMock;
        private readonly Mock<SqsQueueProcessor> _queueProcessorMock;
        private readonly Mock<IOptions<QueuesOptionsBase>> _queueOptionsMock;
        private readonly Mock<ILogger> _loggerMock;
        private const string TestQueueUrl = "https://sqs.us-east-1.amazonaws.com/123456789012/test-queue";

        public SqsListenerTests()
        {
            _sqsClientMock = new Mock<IAmazonSQS>();
            _executorMock = new Mock<ITriggeredFunctionExecutor>();
            _queueProcessorMock = new Mock<SqsQueueProcessor>(
                _sqsClientMock.Object,
                Mock.Of<IQueueProcessorFactory>(),
                Mock.Of<IOptions<QueuesOptionsBase>>(),
                Mock.Of<ILogger>());
            _queueOptionsMock = new Mock<IOptions<QueuesOptionsBase>>();
            _loggerMock = new Mock<ILogger>();

            _queueOptionsMock.Setup(x => x.Value).Returns(new QueuesOptionsBase
            {
                BatchSize = 10,
                MaxPollingInterval = TimeSpan.FromSeconds(20),
                VisibilityTimeout = TimeSpan.FromSeconds(30),
                MessageEncoding = QueueMessageEncoding.None
            });
        }

        [TestMethod]
        public void Constructor_WhenParametersNull_ShouldThrowArgumentNullException()
        {
            // Act & Assert
            FluentActions.Invoking(() => new SqsListener(
                null,
                _sqsClientMock.Object,
                _executorMock.Object,
                _queueProcessorMock.Object,
                _queueOptionsMock.Object,
                _loggerMock.Object))
                .Should().Throw<ArgumentNullException>()
                .And.ParamName.Should().Be("queueUrl");

            // Test other parameters similarly...
        }

        [TestMethod]
        public async Task StartAsync_WhenCalled_ShouldStartPolling()
        {
            // Arrange
            var listener = CreateListener();
            var cts = new CancellationTokenSource();

            _sqsClientMock
                .Setup(x => x.ReceiveMessageAsync(It.IsAny<ReceiveMessageRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ReceiveMessageResponse { Messages = new List<Message>() });

            // Act
            await listener.StartAsync(cts.Token);
            await Task.Delay(100); // Allow some time for polling to start

            // Assert
            _sqsClientMock.Verify(
                x => x.ReceiveMessageAsync(It.IsAny<ReceiveMessageRequest>(), It.IsAny<CancellationToken>()),
                Times.AtLeastOnce());
        }

        [TestMethod]
        public async Task StopAsync_WhenCalled_ShouldStopPolling()
        {
            // Arrange
            var listener = CreateListener();
            var cts = new CancellationTokenSource();

            _sqsClientMock
                .Setup(x => x.ReceiveMessageAsync(It.IsAny<ReceiveMessageRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ReceiveMessageResponse { Messages = new List<Message>() });

            // Act
            await listener.StartAsync(cts.Token);
            await Task.Delay(100);
            await listener.StopAsync(CancellationToken.None);

            // Assert
            // Verify that polling has stopped by checking if ReceiveMessageAsync is no longer being called
            await Task.Delay(200);
            _sqsClientMock.Verify(
                x => x.ReceiveMessageAsync(It.IsAny<ReceiveMessageRequest>(), It.IsAny<CancellationToken>()),
                Times.AtMost(5)); // Assuming a reasonable number of polls during our test
        }

        [TestMethod]
        public async Task ProcessMessage_WhenSuccessful_ShouldDeleteMessage()
        {
            // Arrange
            var listener = CreateListener();
            var message = new Message
            {
                MessageId = "test-id",
                ReceiptHandle = "test-receipt",
                Body = "test-body"
            };

            _sqsClientMock
                .Setup(x => x.ReceiveMessageAsync(It.IsAny<ReceiveMessageRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ReceiveMessageResponse { Messages = new List<Message> { message } });

            _executorMock
                .Setup(x => x.TryExecuteAsync(It.IsAny<TriggeredFunctionData>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new FunctionResult(true));

            // Act
            await listener.StartAsync(CancellationToken.None);
            await Task.Delay(200); // Allow time for message processing

            // Assert
            _sqsClientMock.Verify(
                x => x.DeleteMessageAsync(TestQueueUrl, message.ReceiptHandle, It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [TestMethod]
        public async Task ProcessMessage_WhenFunctionFails_ShouldNotDeleteMessage()
        {
            // Arrange
            var listener = CreateListener();
            var message = new Message
            {
                MessageId = "test-id",
                ReceiptHandle = "test-receipt",
                Body = "test-body"
            };

            _sqsClientMock
                .Setup(x => x.ReceiveMessageAsync(It.IsAny<ReceiveMessageRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ReceiveMessageResponse { Messages = new List<Message> { message } });

            _executorMock
                .Setup(x => x.TryExecuteAsync(It.IsAny<TriggeredFunctionData>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception("Test failure"));

            // Act
            await listener.StartAsync(CancellationToken.None);
            await Task.Delay(200); // Allow time for message processing

            // Assert
            _sqsClientMock.Verify(
                x => x.DeleteMessageAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
        }

        private SqsListener CreateListener() => new(
            TestQueueUrl,
            _sqsClientMock.Object,
            _executorMock.Object,
            _queueProcessorMock.Object,
            _queueOptionsMock.Object,
            _loggerMock.Object);
    }
}
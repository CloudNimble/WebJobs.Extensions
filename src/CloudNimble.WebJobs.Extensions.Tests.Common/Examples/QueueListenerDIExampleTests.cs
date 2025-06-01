// Copyright (c) CloudNimble, Inc. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using CloudNimble.WebJobs.Extensions.Common.Listeners;
using CloudNimble.WebJobs.Extensions.Common.Queues;
using CloudNimble.WebJobs.Extensions.Common.Timers;
using CloudNimble.WebJobs.Extensions.Tests.Common.Extensions;
using CloudNimble.WebJobs.Extensions.Tests.Common.Models;
using FluentAssertions;
using Microsoft.Azure.WebJobs.Host.Protocols;
using Microsoft.Azure.WebJobs.Host.Timers;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace CloudNimble.WebJobs.Extensions.Tests.Common.Examples
{

    /// <summary>
    /// Example tests showing how to use the DI-based testing approach.
    /// </summary>
    [TestClass]
    public class QueueListenerDIExampleTests : WebJobsTestBase
    {

        public void Setup()
        {
            // Register base services for all tests
            RegisterBaseServices();
            TestHostBuilder.ConfigureServices((hostContext, services) =>
            {
                // Configure a specific queue with custom settings
                services.AddTestQueueClient("my-test-queue", "test-account", existsResult: false);
            });
            TestSetup();
        }


        [TestMethod]
        [TestCategory("Example")]
        public async Task QueueListener_WithDI_ShouldCheckQueueExistence()
        {
            Setup();

            // Arrange - All dependencies are injected from the DI container
            var queue = GetService<IQueueClient>() as TestQueueClient;
            var triggerExecutor = GetService<ITriggerExecutor<IQueueMessage>>();
            var queueProcessor = CreateQueueProcessor(); // Helper method creates with DI dependencies

            // Create listener using resolved dependencies
            var listener = new TestQueueListener(
                queue,
                null, // poisonQueue
                triggerExecutor,
                GetService<IWebJobsExceptionHandler>(),
                GetService<ILoggerFactory>(),
                GetService<SharedQueueWatcher>(),
                GetService<QueuesOptionsBase>(),
                queueProcessor,
                GetService<FunctionDescriptor>(),
                GetService<IQueueRequestExceptionClassifier>()
            );

            // Act
            var result = await listener.ExecuteAsync(CancellationToken.None);

            // Assert
            queue.Should().NotBeNull();
            queue.ExistsCallCount.Should().Be(1);
            queue.ExistsResult.Should().BeFalse(); // Configured in ConfigureAdditionalServices
        }

        [TestMethod]
        [TestCategory("Example")]
        public void QueueProcessor_WithDI_ShouldHaveCorrectConfiguration()
        {
            Setup();

            // Arrange & Act
            var queueProcessor = CreateQueueProcessor();
            var queueOptions = GetService<QueuesOptionsBase>();

            // Assert
            queueProcessor.Should().NotBeNull();
            queueOptions.BatchSize.Should().Be(16); // Default from base class ConfigureServices
            queueOptions.MaxDequeueCount.Should().Be(5); // Default from base class ConfigureServices
        }

        [TestMethod]
        [TestCategory("Example")]
        public void ServiceResolution_ShouldProvideConsistentInstances()
        {
            Setup();

            // Act - Get services multiple times
            var logger1 = GetService<ILoggerFactory>();
            var logger2 = GetService<ILoggerFactory>();
            var queue1 = GetService<IQueueClient>();
            var queue2 = GetService<IQueueClient>();

            // Assert - Singleton services should return same instance
            logger1.Should().BeSameAs(logger2);
            queue1.Should().BeSameAs(queue2);
        }

        [TestMethod]
        [TestCategory("Example")]
        public void TestLogger_WithDI_ShouldCaptureLogMessages()
        {
            Setup();

            // Arrange
            var testLogger = GetTestLogger<QueueListener>();

            // Act - Simulate logging
            testLogger.Log(
                Microsoft.Extensions.Logging.LogLevel.Information,
                default,
                "Test message",
                null,
                (state, ex) => state.ToString()
            );

            // Assert
            testLogger.Logs.Should().Contain("Test message");
        }

        [TestMethod]
        [TestCategory("Example")]
        public void QueueProcessor_WithCustomConfig_ShouldHaveOverriddenConfiguration()
        {
            TestHostBuilder.ConfigureServices((hostContext, services) =>
            {
                services.AddWebJobsTestServices();
                services.AddQueueTestServices(options =>
                {
                    options.BatchSize = 32;
                    options.MaxDequeueCount = 10;
                });
                services.AddTriggerTestServices();
            });
            TestSetup();

            // Arrange & Act
            var queueProcessor = CreateQueueProcessor();
            var queueOptions = GetService<QueuesOptionsBase>();

            // Assert
            queueProcessor.Should().NotBeNull();
            queueOptions.BatchSize.Should().Be(32); // Custom configuration
            queueOptions.MaxDequeueCount.Should().Be(10); // Custom configuration
        }

        #region Test Helpers

        /// <summary>
        /// Test queue listener that exposes ExecuteAsync for testing.
        /// </summary>
        private class TestQueueListener : QueueListener
        {
            public TestQueueListener(
                IQueueClient queue,
                IQueueClient poisonQueue,
                ITriggerExecutor<IQueueMessage> triggerExecutor,
                IWebJobsExceptionHandler exceptionHandler,
                ILoggerFactory loggerFactory,
                SharedQueueWatcher sharedWatcher,
                QueuesOptionsBase queueOptions,
                QueueProcessor queueProcessor,
                FunctionDescriptor functionDescriptor,
                IQueueRequestExceptionClassifier exceptionClassifier)
                : base(queue, poisonQueue, triggerExecutor, exceptionHandler, loggerFactory, sharedWatcher,
                      queueOptions, queueProcessor, functionDescriptor, exceptionClassifier, null,
                      null, null, null)
            {
            }

            protected override Action<IQueueMessage, QueueMessageUpdateReceipt> OnUpdateReceipt => null;

            protected override bool IsExceptionFatal(Exception exception) => false;

            // Expose ExecuteAsync for testing
            public new Task<TaskSeriesCommandResult> ExecuteAsync(CancellationToken cancellationToken)
            {
                return base.ExecuteAsync(cancellationToken);
            }
        }

        #endregion

    }

}
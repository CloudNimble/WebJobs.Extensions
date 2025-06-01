// Copyright (c) CloudNimble, Inc. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using CloudNimble.WebJobs.Extensions.Common.Queues;
using CloudNimble.WebJobs.Extensions.Tests.Common.Models;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace CloudNimble.WebJobs.Extensions.Tests.Common.Queues
{

    /// <summary>
    /// Tests for the <see cref="QueueProcessorOptions"/> class.
    /// </summary>
    [TestClass]
    [TestCategory("Unit")]
    public class QueueProcessorOptionsTests
    {

        #region Constructor Tests

        /// <summary>
        /// Tests that the constructor correctly initializes all properties.
        /// </summary>
        [TestMethod]
        public void Constructor_WhenCalledWithValidParameters_ShouldInitializeAllProperties()
        {
            var queue = new TestQueueClient { Name = "test-queue", AccountName = "test-account" };
            var poisonQueue = new TestQueueClient { Name = "test-queue-poison", AccountName = "test-account" };
            var loggerFactory = new TestLoggerFactory();
            var options = new QueuesOptionsBase
            {
                BatchSize = 10,
                MaxDequeueCount = 3,
                VisibilityTimeout = TimeSpan.FromMinutes(2)
            };

            var processorOptions = new QueueProcessorOptions(queue, loggerFactory, options, poisonQueue);

            processorOptions.Queue.Should().BeSameAs(queue);
            processorOptions.PoisonQueue.Should().BeSameAs(poisonQueue);
            processorOptions.Logger.Should().NotBeNull();
            processorOptions.Options.Should().NotBeSameAs(options); // Should be a clone
            processorOptions.Options.BatchSize.Should().Be(10);
            processorOptions.Options.MaxDequeueCount.Should().Be(3);
            processorOptions.Options.VisibilityTimeout.Should().Be(TimeSpan.FromMinutes(2));
        }

        /// <summary>
        /// Tests that the constructor works without a poison queue.
        /// </summary>
        [TestMethod]
        public void Constructor_WhenCalledWithoutPoisonQueue_ShouldInitializeCorrectly()
        {
            var queue = new TestQueueClient { Name = "test-queue", AccountName = "test-account" };
            var loggerFactory = new TestLoggerFactory();
            var options = new QueuesOptionsBase();

            var processorOptions = new QueueProcessorOptions(queue, loggerFactory, options);

            processorOptions.Queue.Should().BeSameAs(queue);
            processorOptions.PoisonQueue.Should().BeNull();
            processorOptions.Logger.Should().NotBeNull();
            processorOptions.Options.Should().NotBeNull();
        }

        /// <summary>
        /// Tests that the constructor throws when queue is null.
        /// </summary>
        [TestMethod]
        public void Constructor_WhenQueueIsNull_ShouldThrowArgumentNullException()
        {
            var loggerFactory = new TestLoggerFactory();
            var options = new QueuesOptionsBase();

            var action = () => new QueueProcessorOptions(null, loggerFactory, options);

            action.Should().Throw<ArgumentNullException>()
                .WithParameterName("queue");
        }

        /// <summary>
        /// Tests that the constructor throws when options is null.
        /// </summary>
        [TestMethod]
        public void Constructor_WhenOptionsIsNull_ShouldThrowArgumentNullException()
        {
            var queue = new TestQueueClient { Name = "test-queue" };
            var loggerFactory = new TestLoggerFactory();

            var action = () => new QueueProcessorOptions(queue, loggerFactory, null);

            action.Should().Throw<ArgumentNullException>()
                .WithParameterName("options");
        }

        /// <summary>
        /// Tests that the constructor accepts null logger factory.
        /// </summary>
        [TestMethod]
        public void Constructor_WhenLoggerFactoryIsNull_ShouldCreateNullLogger()
        {
            var queue = new TestQueueClient { Name = "test-queue" };
            var options = new QueuesOptionsBase();

            var processorOptions = new QueueProcessorOptions(queue, null, options);

            processorOptions.Logger.Should().BeNull();
        }

        /// <summary>
        /// Tests that the constructor accepts null poison queue explicitly.
        /// </summary>
        [TestMethod]
        public void Constructor_WhenPoisonQueueIsExplicitlyNull_ShouldAcceptNull()
        {
            var queue = new TestQueueClient { Name = "test-queue" };
            var loggerFactory = new TestLoggerFactory();
            var options = new QueuesOptionsBase();

            var processorOptions = new QueueProcessorOptions(queue, loggerFactory, options, null);

            processorOptions.PoisonQueue.Should().BeNull();
        }

        #endregion

        #region Property Tests

        /// <summary>
        /// Tests that Queue property returns the correct value.
        /// </summary>
        [TestMethod]
        public void Queue_WhenAccessed_ShouldReturnCorrectValue()
        {
            var queue = new TestQueueClient { Name = "main-queue", AccountName = "production" };
            var loggerFactory = new TestLoggerFactory();
            var options = new QueuesOptionsBase();

            var processorOptions = new QueueProcessorOptions(queue, loggerFactory, options);

            processorOptions.Queue.Should().BeSameAs(queue);
            processorOptions.Queue.Name.Should().Be("main-queue");
            processorOptions.Queue.AccountName.Should().Be("production");
        }

        /// <summary>
        /// Tests that PoisonQueue property returns the correct value.
        /// </summary>
        [TestMethod]
        public void PoisonQueue_WhenAccessed_ShouldReturnCorrectValue()
        {
            var queue = new TestQueueClient { Name = "main-queue" };
            var poisonQueue = new TestQueueClient { Name = "main-queue-poison", AccountName = "production" };
            var loggerFactory = new TestLoggerFactory();
            var options = new QueuesOptionsBase();

            var processorOptions = new QueueProcessorOptions(queue, loggerFactory, options, poisonQueue);

            processorOptions.PoisonQueue.Should().BeSameAs(poisonQueue);
            processorOptions.PoisonQueue.Name.Should().Be("main-queue-poison");
            processorOptions.PoisonQueue.AccountName.Should().Be("production");
        }

        /// <summary>
        /// Tests that Logger property returns the correct value.
        /// </summary>
        [TestMethod]
        public void Logger_WhenAccessedWithValidLoggerFactory_ShouldReturnCorrectLogger()
        {
            var queue = new TestQueueClient { Name = "test-queue" };
            var loggerFactory = new TestLoggerFactory();
            var options = new QueuesOptionsBase();

            var processorOptions = new QueueProcessorOptions(queue, loggerFactory, options);

            processorOptions.Logger.Should().NotBeNull();
            processorOptions.Logger.Should().BeOfType<TestLogger<QueueProcessor>>();
        }

        /// <summary>
        /// Tests that Options property returns a cloned copy.
        /// </summary>
        [TestMethod]
        public void Options_WhenAccessed_ShouldReturnClonedCopy()
        {
            var queue = new TestQueueClient { Name = "test-queue" };
            var loggerFactory = new TestLoggerFactory();
            var originalOptions = new QueuesOptionsBase
            {
                BatchSize = 25,
                MaxDequeueCount = 8,
                VisibilityTimeout = TimeSpan.FromMinutes(5)
            };

            var processorOptions = new QueueProcessorOptions(queue, loggerFactory, originalOptions);

            processorOptions.Options.Should().NotBeSameAs(originalOptions);
            processorOptions.Options.BatchSize.Should().Be(25);
            processorOptions.Options.MaxDequeueCount.Should().Be(8);
            processorOptions.Options.VisibilityTimeout.Should().Be(TimeSpan.FromMinutes(5));
        }

        /// <summary>
        /// Tests that modifying original options doesn't affect the processor options.
        /// </summary>
        [TestMethod]
        public void Options_WhenOriginalOptionsModified_ShouldNotBeAffected()
        {
            var queue = new TestQueueClient { Name = "test-queue" };
            var loggerFactory = new TestLoggerFactory();
            var originalOptions = new QueuesOptionsBase
            {
                BatchSize = 16,
                MaxDequeueCount = 5
            };

            var processorOptions = new QueueProcessorOptions(queue, loggerFactory, originalOptions);
            
            // Modify original options after creating processor options
            originalOptions.BatchSize = 32;
            originalOptions.MaxDequeueCount = 10;

            // Processor options should remain unchanged
            processorOptions.Options.BatchSize.Should().Be(16);
            processorOptions.Options.MaxDequeueCount.Should().Be(5);
        }

        /// <summary>
        /// Tests that all properties are read-only.
        /// </summary>
        [TestMethod]
        public void Properties_WhenExamined_ShouldBeReadOnly()
        {
            var queueProperty = typeof(QueueProcessorOptions).GetProperty(nameof(QueueProcessorOptions.Queue));
            var poisonQueueProperty = typeof(QueueProcessorOptions).GetProperty(nameof(QueueProcessorOptions.PoisonQueue));
            var loggerProperty = typeof(QueueProcessorOptions).GetProperty(nameof(QueueProcessorOptions.Logger));
            var optionsProperty = typeof(QueueProcessorOptions).GetProperty(nameof(QueueProcessorOptions.Options));

            queueProperty.CanWrite.Should().BeFalse("Queue property should be read-only");
            poisonQueueProperty.CanWrite.Should().BeFalse("PoisonQueue property should be read-only");
            loggerProperty.CanWrite.Should().BeFalse("Logger property should be read-only");
            optionsProperty.CanWrite.Should().BeFalse("Options property should be read-only");
        }

        #endregion

        #region Integration Tests

        /// <summary>
        /// Tests that QueueProcessorOptions works correctly with real queue configurations.
        /// </summary>
        [TestMethod]
        public void QueueProcessorOptions_WhenUsedWithRealisticConfiguration_ShouldWorkCorrectly()
        {
            var mainQueue = new TestQueueClient 
            { 
                Name = "orders-processing", 
                AccountName = "production-storage" 
            };
            var poisonQueue = new TestQueueClient 
            { 
                Name = "orders-processing-poison", 
                AccountName = "production-storage" 
            };
            var loggerFactory = new TestLoggerFactory();
            var queueOptions = new QueuesOptionsBase
            {
                BatchSize = 10,
                MaxDequeueCount = 5,
                VisibilityTimeout = TimeSpan.FromMinutes(1),
                MaxPollingInterval = TimeSpan.FromMinutes(2),
                MessageEncoding = QueueMessageEncoding.Base64
            };

            var processorOptions = new QueueProcessorOptions(mainQueue, loggerFactory, queueOptions, poisonQueue);

            processorOptions.Queue.Name.Should().Be("orders-processing");
            processorOptions.PoisonQueue.Name.Should().Be("orders-processing-poison");
            processorOptions.Queue.AccountName.Should().Be(processorOptions.PoisonQueue.AccountName);
            processorOptions.Options.BatchSize.Should().Be(10);
            processorOptions.Options.MaxDequeueCount.Should().Be(5);
            processorOptions.Options.VisibilityTimeout.Should().Be(TimeSpan.FromMinutes(1));
            processorOptions.Options.MessageEncoding.Should().Be(QueueMessageEncoding.Base64);
            processorOptions.Logger.Should().NotBeNull();
        }

        /// <summary>
        /// Tests QueueProcessorOptions with different queue types.
        /// </summary>
        [TestMethod]
        public void QueueProcessorOptions_WhenUsedWithDifferentQueueTypes_ShouldWorkCorrectly()
        {
            var scenarios = new[]
            {
                new { QueueName = "high-priority", PoisonQueueName = "high-priority-poison", BatchSize = 5 },
                new { QueueName = "low-priority", PoisonQueueName = "low-priority-poison", BatchSize = 20 },
                new { QueueName = "real-time", PoisonQueueName = "real-time-poison", BatchSize = 1 }
            };

            foreach (var scenario in scenarios)
            {
                var queue = new TestQueueClient { Name = scenario.QueueName };
                var poisonQueue = new TestQueueClient { Name = scenario.PoisonQueueName };
                var loggerFactory = new TestLoggerFactory();
                var options = new QueuesOptionsBase { BatchSize = scenario.BatchSize };

                var processorOptions = new QueueProcessorOptions(queue, loggerFactory, options, poisonQueue);

                processorOptions.Queue.Name.Should().Be(scenario.QueueName);
                processorOptions.PoisonQueue.Name.Should().Be(scenario.PoisonQueueName);
                processorOptions.Options.BatchSize.Should().Be(scenario.BatchSize);
            }
        }

        #endregion

        #region Edge Case Tests

        /// <summary>
        /// Tests QueueProcessorOptions with edge case queue names.
        /// </summary>
        [TestMethod]
        public void QueueProcessorOptions_WhenUsedWithEdgeCaseQueueNames_ShouldHandleCorrectly()
        {
            var edgeCaseQueues = new[]
            {
                new TestQueueClient { Name = "", AccountName = "" },
                new TestQueueClient { Name = "a", AccountName = "b" },
                new TestQueueClient { Name = new string('x', 1000), AccountName = new string('y', 1000) },
                new TestQueueClient { Name = "queue-with-unicode-🚀", AccountName = "account-with-symbols-@#$%" }
            };

            foreach (var queue in edgeCaseQueues)
            {
                var loggerFactory = new TestLoggerFactory();
                var options = new QueuesOptionsBase();

                var processorOptions = new QueueProcessorOptions(queue, loggerFactory, options);

                processorOptions.Queue.Should().BeSameAs(queue);
                processorOptions.Queue.Name.Should().Be(queue.Name);
                processorOptions.Queue.AccountName.Should().Be(queue.AccountName);
            }
        }

        /// <summary>
        /// Tests QueueProcessorOptions with edge case options values.
        /// </summary>
        [TestMethod]
        public void QueueProcessorOptions_WhenUsedWithEdgeCaseOptions_ShouldHandleCorrectly()
        {
            var queue = new TestQueueClient { Name = "test-queue" };
            var loggerFactory = new TestLoggerFactory();
            var edgeCaseOptions = new QueuesOptionsBase
            {
                BatchSize = 1, // Minimum
                MaxDequeueCount = 1, // Minimum
                VisibilityTimeout = TimeSpan.Zero,
                MaxPollingInterval = QueuePollingIntervals.Minimum
            };

            var processorOptions = new QueueProcessorOptions(queue, loggerFactory, edgeCaseOptions);

            processorOptions.Options.BatchSize.Should().Be(1);
            processorOptions.Options.MaxDequeueCount.Should().Be(1);
            processorOptions.Options.VisibilityTimeout.Should().Be(TimeSpan.Zero);
            processorOptions.Options.MaxPollingInterval.Should().Be(QueuePollingIntervals.Minimum);
        }

        #endregion

        #region Performance Tests

        /// <summary>
        /// Tests that creating many QueueProcessorOptions instances performs well.
        /// </summary>
        [TestMethod]
        public void QueueProcessorOptions_WhenCreatedMany_ShouldPerformWell()
        {
            var queue = new TestQueueClient { Name = "perf-test-queue" };
            var loggerFactory = new TestLoggerFactory();
            var options = new QueuesOptionsBase();

            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            const int iterations = 1000;

            for (int i = 0; i < iterations; i++)
            {
                var processorOptions = new QueueProcessorOptions(queue, loggerFactory, options);
                processorOptions.Queue.Should().NotBeNull();
            }

            stopwatch.Stop();

            // Should complete quickly (allowing for some variance in test environments)
            stopwatch.ElapsedMilliseconds.Should().BeLessThan(1000);
        }

        /// <summary>
        /// Tests that options cloning doesn't cause memory issues.
        /// </summary>
        [TestMethod]
        public void QueueProcessorOptions_WhenOptionsClonedRepeatedly_ShouldNotCauseMemoryIssues()
        {
            var queue = new TestQueueClient { Name = "memory-test-queue" };
            var loggerFactory = new TestLoggerFactory();
            var originalOptions = new QueuesOptionsBase
            {
                BatchSize = 16,
                MaxDequeueCount = 5,
                VisibilityTimeout = TimeSpan.FromMinutes(1)
            };

            // Create many instances to test memory behavior
            for (int i = 0; i < 1000; i++)
            {
                var processorOptions = new QueueProcessorOptions(queue, loggerFactory, originalOptions);
                processorOptions.Options.Should().NotBeSameAs(originalOptions);
            }

            // Force garbage collection to help detect potential issues
            GC.Collect();
            GC.WaitForPendingFinalizers();
        }

        #endregion

        #region Thread Safety Tests

        /// <summary>
        /// Tests that QueueProcessorOptions is safe for concurrent creation.
        /// </summary>
        [TestMethod]
        public void QueueProcessorOptions_WhenCreatedConcurrently_ShouldBeSafe()
        {
            var queue = new TestQueueClient { Name = "concurrent-queue" };
            var loggerFactory = new TestLoggerFactory();
            var options = new QueuesOptionsBase { BatchSize = 20 };

            var tasks = new System.Collections.Generic.List<System.Threading.Tasks.Task>();
            var results = new System.Collections.Concurrent.ConcurrentBag<QueueProcessorOptions>();

            // Create multiple tasks that create processor options
            for (int i = 0; i < 10; i++)
            {
                tasks.Add(System.Threading.Tasks.Task.Run(() =>
                {
                    var processorOptions = new QueueProcessorOptions(queue, loggerFactory, options);
                    results.Add(processorOptions);
                }));
            }

            System.Threading.Tasks.Task.WaitAll(tasks.ToArray());

            results.Should().HaveCount(10);
            results.Should().OnlyContain(po => po.Queue == queue && po.Options.BatchSize == 20);
        }

        #endregion

        #region Validation Tests

        /// <summary>
        /// Tests that QueueProcessorOptions maintains data integrity.
        /// </summary>
        [TestMethod]
        public void QueueProcessorOptions_WhenExamined_ShouldMaintainDataIntegrity()
        {
            var queue = new TestQueueClient { Name = "integrity-test-queue", AccountName = "test-account" };
            var poisonQueue = new TestQueueClient { Name = "integrity-test-queue-poison", AccountName = "test-account" };
            var loggerFactory = new TestLoggerFactory();
            var options = new QueuesOptionsBase
            {
                BatchSize = 15,
                MaxDequeueCount = 7,
                VisibilityTimeout = TimeSpan.FromMinutes(3),
                MessageEncoding = QueueMessageEncoding.None
            };

            var processorOptions = new QueueProcessorOptions(queue, loggerFactory, options, poisonQueue);

            // Verify all data is correctly preserved
            processorOptions.Queue.Name.Should().Be("integrity-test-queue");
            processorOptions.Queue.AccountName.Should().Be("test-account");
            processorOptions.PoisonQueue.Name.Should().Be("integrity-test-queue-poison");
            processorOptions.PoisonQueue.AccountName.Should().Be("test-account");
            processorOptions.Options.BatchSize.Should().Be(15);
            processorOptions.Options.MaxDequeueCount.Should().Be(7);
            processorOptions.Options.VisibilityTimeout.Should().Be(TimeSpan.FromMinutes(3));
            processorOptions.Options.MessageEncoding.Should().Be(QueueMessageEncoding.None);
        }

        #endregion

    }

}
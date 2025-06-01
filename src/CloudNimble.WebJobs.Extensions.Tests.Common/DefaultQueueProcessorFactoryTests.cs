// Copyright (c) CloudNimble, Inc. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using CloudNimble.WebJobs.Extensions.Common;
using CloudNimble.WebJobs.Extensions.Common.Queues;
using CloudNimble.WebJobs.Extensions.Tests.Common.Models;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace CloudNimble.WebJobs.Extensions.Tests.Common
{

    /// <summary>
    /// Tests for the <see cref="DefaultQueueProcessorFactory"/> class.
    /// </summary>
    [TestClass]
    [TestCategory("Unit")]
    public class DefaultQueueProcessorFactoryTests
    {

        #region Constructor Tests

        /// <summary>
        /// Tests that the constructor correctly initializes with a valid exception classifier.
        /// </summary>
        [TestMethod]
        public void Constructor_WhenCalledWithValidExceptionClassifier_ShouldInitializeCorrectly()
        {
            var exceptionClassifier = new TestQueueRequestExceptionClassifier();

            var factory = new DefaultQueueProcessorFactory(exceptionClassifier);

            factory.Should().NotBeNull();
        }

        /// <summary>
        /// Tests that the constructor throws when exception classifier is null.
        /// </summary>
        [TestMethod]
        public void Constructor_WhenExceptionClassifierIsNull_ShouldThrowArgumentNullException()
        {
            var action = () => new DefaultQueueProcessorFactory(null);

            action.Should().Throw<ArgumentNullException>()
                .WithParameterName("exceptionClassifier");
        }

        #endregion

        #region Create Method Tests

        /// <summary>
        /// Tests that Create method returns a valid QueueProcessor instance.
        /// </summary>
        [TestMethod]
        public void Create_WhenCalledWithValidContext_ShouldReturnQueueProcessor()
        {
            var exceptionClassifier = new TestQueueRequestExceptionClassifier();
            var factory = new DefaultQueueProcessorFactory(exceptionClassifier);
            var context = CreateValidQueueProcessorOptions();

            var processor = factory.Create(context);

            processor.Should().NotBeNull();
            processor.Should().BeOfType<QueueProcessor>();
        }

        /// <summary>
        /// Tests that Create method throws when context is null.
        /// </summary>
        [TestMethod]
        public void Create_WhenContextIsNull_ShouldThrowArgumentNullException()
        {
            var exceptionClassifier = new TestQueueRequestExceptionClassifier();
            var factory = new DefaultQueueProcessorFactory(exceptionClassifier);

            var action = () => factory.Create(null);

            action.Should().Throw<ArgumentNullException>()
                .WithParameterName("context");
        }

        /// <summary>
        /// Tests that Create method can be called multiple times.
        /// </summary>
        [TestMethod]
        public void Create_WhenCalledMultipleTimes_ShouldReturnDifferentInstances()
        {
            var exceptionClassifier = new TestQueueRequestExceptionClassifier();
            var factory = new DefaultQueueProcessorFactory(exceptionClassifier);
            var context = CreateValidQueueProcessorOptions();

            var processor1 = factory.Create(context);
            var processor2 = factory.Create(context);

            processor1.Should().NotBeSameAs(processor2);
            processor1.Should().BeOfType<QueueProcessor>();
            processor2.Should().BeOfType<QueueProcessor>();
        }

        /// <summary>
        /// Tests that Create method works with different queue configurations.
        /// </summary>
        [TestMethod]
        public void Create_WhenCalledWithDifferentConfigurations_ShouldCreateAppropriateProcessors()
        {
            var exceptionClassifier = new TestQueueRequestExceptionClassifier();
            var factory = new DefaultQueueProcessorFactory(exceptionClassifier);

            var configurations = new[]
            {
                new { QueueName = "high-priority", BatchSize = 5, MaxDequeueCount = 3 },
                new { QueueName = "low-priority", BatchSize = 20, MaxDequeueCount = 5 },
                new { QueueName = "real-time", BatchSize = 1, MaxDequeueCount = 1 }
            };

            foreach (var config in configurations)
            {
                var context = CreateQueueProcessorOptions(config.QueueName, config.BatchSize, config.MaxDequeueCount);
                var processor = factory.Create(context);

                processor.Should().NotBeNull();
                processor.Should().BeOfType<QueueProcessor>();
            }
        }

        #endregion

        #region Interface Implementation Tests

        /// <summary>
        /// Tests that DefaultQueueProcessorFactory implements IQueueProcessorFactory.
        /// </summary>
        [TestMethod]
        public void DefaultQueueProcessorFactory_WhenExamined_ShouldImplementIQueueProcessorFactory()
        {
            typeof(DefaultQueueProcessorFactory).Should().BeAssignableTo<IQueueProcessorFactory>();
        }

        /// <summary>
        /// Tests that factory can be used polymorphically.
        /// </summary>
        [TestMethod]
        public void DefaultQueueProcessorFactory_WhenUsedPolymorphically_ShouldWorkCorrectly()
        {
            var exceptionClassifier = new TestQueueRequestExceptionClassifier();
            IQueueProcessorFactory factory = new DefaultQueueProcessorFactory(exceptionClassifier);
            var context = CreateValidQueueProcessorOptions();

            var processor = factory.Create(context);

            processor.Should().NotBeNull();
            processor.Should().BeOfType<QueueProcessor>();
        }

        #endregion

        #region Integration Tests

        /// <summary>
        /// Tests that the factory creates processors that work with realistic scenarios.
        /// </summary>
        [TestMethod]
        public void Create_WhenUsedInRealisticScenario_ShouldCreateFunctionalProcessors()
        {
            var exceptionClassifier = new TestQueueRequestExceptionClassifier();
            var factory = new DefaultQueueProcessorFactory(exceptionClassifier);

            // Simulate a realistic queue processing scenario
            var mainQueue = new TestQueueClient { Name = "orders", AccountName = "production" };
            var poisonQueue = new TestQueueClient { Name = "orders-poison", AccountName = "production" };
            var loggerFactory = new TestLoggerFactory();
            var options = new QueuesOptionsBase
            {
                BatchSize = 16,
                MaxDequeueCount = 5,
                VisibilityTimeout = TimeSpan.FromMinutes(1),
                MessageEncoding = QueueMessageEncoding.Base64
            };

            var context = new QueueProcessorOptions(mainQueue, loggerFactory, options, poisonQueue);
            var processor = factory.Create(context);

            processor.Should().NotBeNull();
            processor.QueuesOptions.Should().NotBeNull();
            processor.QueuesOptions.BatchSize.Should().Be(16);
            processor.QueuesOptions.MaxDequeueCount.Should().Be(5);
        }

        /// <summary>
        /// Tests that the factory creates processors for queues without poison queues.
        /// </summary>
        [TestMethod]
        public void Create_WhenUsedWithoutPoisonQueue_ShouldCreateValidProcessor()
        {
            var exceptionClassifier = new TestQueueRequestExceptionClassifier();
            var factory = new DefaultQueueProcessorFactory(exceptionClassifier);

            var mainQueue = new TestQueueClient { Name = "simple-queue" };
            var loggerFactory = new TestLoggerFactory();
            var options = new QueuesOptionsBase();

            var context = new QueueProcessorOptions(mainQueue, loggerFactory, options);
            var processor = factory.Create(context);

            processor.Should().NotBeNull();
            processor.Should().BeOfType<QueueProcessor>();
        }

        #endregion

        #region Error Handling Tests

        /// <summary>
        /// Tests factory behavior when queue processor options have null queue.
        /// </summary>
        [TestMethod]
        public void Create_WhenQueueInContextIsNull_ShouldThrowArgumentNullException()
        {
            var exceptionClassifier = new TestQueueRequestExceptionClassifier();
            var factory = new DefaultQueueProcessorFactory(exceptionClassifier);
            var loggerFactory = new TestLoggerFactory();
            var options = new QueuesOptionsBase();

            // Create context with null queue (this should fail in QueueProcessorOptions constructor)
            var action = () => new QueueProcessorOptions(null, loggerFactory, options);

            action.Should().Throw<ArgumentNullException>();
        }

        /// <summary>
        /// Tests factory behavior when queue processor options have null options.
        /// </summary>
        [TestMethod]
        public void Create_WhenOptionsInContextIsNull_ShouldThrowArgumentNullException()
        {
            var exceptionClassifier = new TestQueueRequestExceptionClassifier();
            var factory = new DefaultQueueProcessorFactory(exceptionClassifier);
            var queue = new TestQueueClient { Name = "test-queue" };
            var loggerFactory = new TestLoggerFactory();

            // Create context with null options (this should fail in QueueProcessorOptions constructor)
            var action = () => new QueueProcessorOptions(queue, loggerFactory, null);

            action.Should().Throw<ArgumentNullException>();
        }

        #endregion

        #region Thread Safety Tests

        /// <summary>
        /// Tests that the factory is safe for concurrent use.
        /// </summary>
        [TestMethod]
        public void Create_WhenUsedConcurrently_ShouldBeSafe()
        {
            var exceptionClassifier = new TestQueueRequestExceptionClassifier();
            var factory = new DefaultQueueProcessorFactory(exceptionClassifier);

            var tasks = new System.Collections.Generic.List<System.Threading.Tasks.Task>();
            var results = new System.Collections.Concurrent.ConcurrentBag<QueueProcessor>();

            // Create multiple tasks that use the factory
            for (int i = 0; i < 10; i++)
            {
                int taskId = i;
                tasks.Add(System.Threading.Tasks.Task.Run(() =>
                {
                    var context = CreateQueueProcessorOptions($"queue-{taskId}", 16, 5);
                    var processor = factory.Create(context);
                    results.Add(processor);
                }));
            }

            System.Threading.Tasks.Task.WaitAll(tasks.ToArray());

            results.Should().HaveCount(10);
            results.Should().OnlyContain(p => p != null && p.GetType() == typeof(QueueProcessor));
        }

        /// <summary>
        /// Tests that multiple factories can be used concurrently.
        /// </summary>
        [TestMethod]
        public void Create_WhenMultipleFactoriesUsedConcurrently_ShouldBeSafe()
        {
            var tasks = new System.Collections.Generic.List<System.Threading.Tasks.Task>();
            var results = new System.Collections.Concurrent.ConcurrentBag<QueueProcessor>();

            // Create multiple tasks that each create their own factory
            for (int i = 0; i < 10; i++)
            {
                int taskId = i;
                tasks.Add(System.Threading.Tasks.Task.Run(() =>
                {
                    var exceptionClassifier = new TestQueueRequestExceptionClassifier();
                    var factory = new DefaultQueueProcessorFactory(exceptionClassifier);
                    var context = CreateQueueProcessorOptions($"queue-{taskId}", 16, 5);
                    var processor = factory.Create(context);
                    results.Add(processor);
                }));
            }

            System.Threading.Tasks.Task.WaitAll(tasks.ToArray());

            results.Should().HaveCount(10);
            results.Should().OnlyContain(p => p != null);
        }

        #endregion

        #region Performance Tests

        /// <summary>
        /// Tests that the factory can create many processors efficiently.
        /// </summary>
        [TestMethod]
        public void Create_WhenCreatingManyProcessors_ShouldPerformWell()
        {
            var exceptionClassifier = new TestQueueRequestExceptionClassifier();
            var factory = new DefaultQueueProcessorFactory(exceptionClassifier);
            var context = CreateValidQueueProcessorOptions();

            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            const int iterations = 1000;

            for (int i = 0; i < iterations; i++)
            {
                var processor = factory.Create(context);
                processor.Should().NotBeNull();
            }

            stopwatch.Stop();

            // Should complete quickly (allowing for some variance in test environments)
            stopwatch.ElapsedMilliseconds.Should().BeLessThan(2000);
        }

        #endregion

        #region Edge Case Tests

        /// <summary>
        /// Tests factory with edge case queue configurations.
        /// </summary>
        [TestMethod]
        public void Create_WhenUsedWithEdgeCaseConfigurations_ShouldHandleCorrectly()
        {
            var exceptionClassifier = new TestQueueRequestExceptionClassifier();
            var factory = new DefaultQueueProcessorFactory(exceptionClassifier);

            var edgeCases = new[]
            {
                new { QueueName = "", BatchSize = 1, MaxDequeueCount = 1 },
                new { QueueName = "a", BatchSize = 32, MaxDequeueCount = 1000 },
                new { QueueName = new string('x', 1000), BatchSize = 1, MaxDequeueCount = 1 },
                new { QueueName = "queue-with-unicode-🚀", BatchSize = 16, MaxDequeueCount = 5 }
            };

            foreach (var edgeCase in edgeCases)
            {
                var context = CreateQueueProcessorOptions(edgeCase.QueueName, edgeCase.BatchSize, edgeCase.MaxDequeueCount);
                var processor = factory.Create(context);

                processor.Should().NotBeNull();
                processor.Should().BeOfType<QueueProcessor>();
            }
        }

        /// <summary>
        /// Tests factory with various exception classifier implementations.
        /// </summary>
        [TestMethod]
        public void Create_WhenUsedWithDifferentExceptionClassifiers_ShouldWorkCorrectly()
        {
            var classifiers = new IQueueRequestExceptionClassifier[]
            {
                new TestQueueRequestExceptionClassifier(),
                new AlwaysTrueExceptionClassifier(),
                new AlwaysFalseExceptionClassifier()
            };

            foreach (var classifier in classifiers)
            {
                var factory = new DefaultQueueProcessorFactory(classifier);
                var context = CreateValidQueueProcessorOptions();
                var processor = factory.Create(context);

                processor.Should().NotBeNull();
                processor.Should().BeOfType<QueueProcessor>();
            }
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Creates a valid QueueProcessorOptions instance for testing.
        /// </summary>
        private static QueueProcessorOptions CreateValidQueueProcessorOptions()
        {
            return CreateQueueProcessorOptions("test-queue", 16, 5);
        }

        /// <summary>
        /// Creates a QueueProcessorOptions instance with specified parameters.
        /// </summary>
        /// <param name="queueName">The queue name.</param>
        /// <param name="batchSize">The batch size.</param>
        /// <param name="maxDequeueCount">The maximum dequeue count.</param>
        /// <returns>A configured QueueProcessorOptions instance.</returns>
        private static QueueProcessorOptions CreateQueueProcessorOptions(string queueName, int batchSize, int maxDequeueCount)
        {
            var queue = new TestQueueClient { Name = queueName, AccountName = "test-account" };
            var loggerFactory = new TestLoggerFactory();
            var options = new QueuesOptionsBase
            {
                BatchSize = batchSize,
                MaxDequeueCount = maxDequeueCount
            };

            return new QueueProcessorOptions(queue, loggerFactory, options);
        }

        #endregion

    }

    #region Test Helper Classes

    /// <summary>
    /// Exception classifier that always returns true for testing edge cases.
    /// </summary>
    internal class AlwaysTrueExceptionClassifier : IQueueRequestExceptionClassifier
    {
        public bool IsServerSideException(Exception exception) => true;
        public bool IsPopReceiptMismatch(Exception exception) => true;
        public bool IsNotFoundException(Exception exception) => true;
        public bool IsConflictException(Exception exception) => true;
    }

    /// <summary>
    /// Exception classifier that always returns false for testing edge cases.
    /// </summary>
    internal class AlwaysFalseExceptionClassifier : IQueueRequestExceptionClassifier
    {
        public bool IsServerSideException(Exception exception) => false;
        public bool IsPopReceiptMismatch(Exception exception) => false;
        public bool IsNotFoundException(Exception exception) => false;
        public bool IsConflictException(Exception exception) => false;
    }

    #endregion

}
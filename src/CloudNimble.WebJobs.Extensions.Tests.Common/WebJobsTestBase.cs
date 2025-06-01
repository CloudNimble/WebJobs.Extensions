// Copyright (c) CloudNimble, Inc. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using CloudNimble.Breakdance.Extensions.MSTest2;
using CloudNimble.WebJobs.Extensions.Common.Listeners;
using CloudNimble.WebJobs.Extensions.Common.Queues;
using CloudNimble.WebJobs.Extensions.Tests.Common.Models;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Azure.WebJobs.Host.Protocols;
using Microsoft.Azure.WebJobs.Host.Timers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace CloudNimble.WebJobs.Extensions.Tests.Common
{

    /// <summary>
    /// Base class for WebJobs extension tests that provides dependency injection setup.
    /// Follows the same pattern as Breakdance.BreakdanceTestBase.
    /// </summary>
    public abstract class WebJobsTestBase : BreakdanceMSTestBase, IDisposable
    {

        /// <summary>
        /// Initializes the test base. Call this in your test setup method.
        /// </summary>
        public virtual void RegisterBaseServices()
        {
            TestHostBuilder
                .ConfigureServices(services =>
                {
                    // Register WebJobs core services
                    services.AddSingleton<ILoggerFactory, TestLoggerFactory>();
                    services.AddSingleton<IWebJobsExceptionHandler, TestExceptionHandler>();
                    services.AddSingleton<IDrainModeManager, TestDrainModeManager>();

                    // Register queue-related services
                    services.AddSingleton<IQueueClient, TestQueueClient>();
                    services.AddSingleton<IQueueProcessorFactory, TestQueueProcessorFactory>();
                    services.AddSingleton<IQueueRequestExceptionClassifier, TestQueueRequestExceptionClassifier>();
                    services.AddSingleton<SharedQueueWatcher>();

                    // Register queue options
                    services.AddSingleton(new QueuesOptionsBase { BatchSize = 16, MaxDequeueCount = 5 });

                    // Register trigger executor for queue messages
                    services.AddSingleton<ITriggerExecutor<IQueueMessage>, TestTriggerExecutor<IQueueMessage>>();

                    // Register function descriptor
                    services.AddSingleton(new FunctionDescriptor { Id = "test-function", LogName = "TestFunction" });

                    //services.AddSingleton<ITestService, TestService>();
                });
        }

        /// <summary>
        /// Cleans up test resources. Call this in your test cleanup method.
        /// </summary>
        [TestCleanup]
        public virtual void TestCleanup()
        {
            Dispose();
        }

        /// <summary>
        /// Gets a test logger that captures log messages for verification.
        /// </summary>
        protected TestLogger GetTestLogger<T>()
        {
            var loggerFactory = GetService<ILoggerFactory>() as TestLoggerFactory;
            return loggerFactory?.CreateLogger<T>() as TestLogger ?? new TestLogger<T>();
        }

        /// <summary>
        /// Gets a configured queue client for testing.
        /// </summary>
        protected TestQueueClient GetTestQueueClient()
        {
            return GetService<IQueueClient>() as TestQueueClient ?? new TestQueueClient();
        }

        /// <summary>
        /// Creates a queue processor with proper dependencies.
        /// </summary>
        protected QueueProcessor CreateQueueProcessor(IQueueClient queue = null, IQueueClient poisonQueue = null)
        {
            var loggerFactory = GetService<ILoggerFactory>();
            var options = GetService<QueuesOptionsBase>();
            var exceptionClassifier = GetService<IQueueRequestExceptionClassifier>();

            var queueProcessorOptions = new QueueProcessorOptions(
                queue ?? GetService<IQueueClient>(),
                loggerFactory,
                options,
                poisonQueue
            );

            return new QueueProcessor(queueProcessorOptions, exceptionClassifier);
        }

    }

}
// Copyright (c) CloudNimble, Inc. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using CloudNimble.WebJobs.Extensions.Common.Listeners;
using CloudNimble.WebJobs.Extensions.Common.Queues;
using CloudNimble.WebJobs.Extensions.Tests.Common.Models;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Azure.WebJobs.Host.Executors;
using Microsoft.Azure.WebJobs.Host.Listeners;
using Microsoft.Azure.WebJobs.Host.Protocols;
using Microsoft.Azure.WebJobs.Host.Scale;
using Microsoft.Azure.WebJobs.Host.Timers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;

namespace CloudNimble.WebJobs.Extensions.Tests.Common.Extensions
{

    /// <summary>
    /// Extension methods for configuring WebJobs test services in DI container.
    /// </summary>
    public static class ServiceCollectionExtensions
    {

        /// <summary>
        /// Adds the core WebJobs testing services to the service collection.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddWebJobsTestServices(this IServiceCollection services)
        {
            // Core WebJobs services
            services.AddSingleton<ILoggerFactory, TestLoggerFactory>();
            services.AddSingleton<IWebJobsExceptionHandler, TestExceptionHandler>();
            services.AddSingleton<IDrainModeManager, TestDrainModeManager>();

            return services;
        }

        /// <summary>
        /// Adds queue testing services to the service collection.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="configureOptions">Optional action to configure queue options.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddQueueTestServices(this IServiceCollection services, Action<QueuesOptionsBase> configureOptions = null)
        {
            // Queue services
            services.AddSingleton<IQueueClient, TestQueueClient>();
            services.AddSingleton<IQueueProcessorFactory, TestQueueProcessorFactory>();
            services.AddSingleton<IQueueRequestExceptionClassifier, TestQueueRequestExceptionClassifier>();
            services.AddSingleton<SharedQueueWatcher>();

            // Queue options
            var options = new QueuesOptionsBase { BatchSize = 16, MaxDequeueCount = 5 };
            configureOptions?.Invoke(options);
            services.AddSingleton(options);

            return services;
        }

        /// <summary>
        /// Adds trigger execution services to the service collection.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddTriggerTestServices(this IServiceCollection services)
        {
            services.AddSingleton<ITriggerExecutor<IQueueMessage>, TestTriggerExecutor<IQueueMessage>>();
            services.AddSingleton(new FunctionDescriptor { Id = "test-function", LogName = "TestFunction" });

            return services;
        }

        /// <summary>
        /// Adds a test queue client with specific configuration.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="name">The queue name.</param>
        /// <param name="accountName">The account name.</param>
        /// <param name="existsResult">Whether the queue should exist.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddTestQueueClient(this IServiceCollection services, string name = "test-queue", string accountName = "test-account", bool existsResult = true)
        {
            services.AddSingleton<IQueueClient>(new TestQueueClient 
            { 
                Name = name, 
                AccountName = accountName, 
                ExistsResult = existsResult 
            });

            return services;
        }

        /// <summary>
        /// Adds a poison queue client for testing.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="name">The poison queue name.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddTestPoisonQueueClient(this IServiceCollection services, string name = "test-queue-poison")
        {
            services.AddSingleton<IQueueClient>(provider => new TestQueueClient { Name = name });

            return services;
        }

    }

}
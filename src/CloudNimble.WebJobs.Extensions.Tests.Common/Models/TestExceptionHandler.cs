// Copyright (c) CloudNimble, Inc. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Microsoft.Azure.WebJobs.Host.Timers;
using System;
using System.Runtime.ExceptionServices;
using System.Threading.Tasks;

namespace CloudNimble.WebJobs.Extensions.Tests.Common.Models
{

    /// <summary>
    /// Test implementation of IWebJobsExceptionHandler for testing purposes.
    /// </summary>
    public class TestExceptionHandler : IWebJobsExceptionHandler
    {
        public int UnhandledExceptionCount { get; private set; }
        public int TimeoutExceptionCount { get; private set; }
        public ExceptionDispatchInfo LastException { get; private set; }
        public TimeSpan? LastTimeout { get; private set; }

        public Task OnUnhandledExceptionAsync(ExceptionDispatchInfo exceptionInfo)
        {
            UnhandledExceptionCount++;
            LastException = exceptionInfo;
            return Task.CompletedTask;
        }

        public Task OnTimeoutExceptionAsync(ExceptionDispatchInfo exceptionInfo, TimeSpan timeout)
        {
            TimeoutExceptionCount++;
            LastException = exceptionInfo;
            LastTimeout = timeout;
            return Task.CompletedTask;
        }
    }

}
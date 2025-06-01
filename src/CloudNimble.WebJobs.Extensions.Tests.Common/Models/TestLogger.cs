// Copyright (c) CloudNimble, Inc. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;

namespace CloudNimble.WebJobs.Extensions.Tests.Common.Models
{
    /// <summary>
    /// Test implementation of ILogger for testing purposes.
    /// </summary>
    public class TestLogger : ILogger
    {
        public List<string> Logs { get; } = new List<string>();

        public IDisposable BeginScope<TState>(TState state) => null;
        public bool IsEnabled(LogLevel logLevel) => true;
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            if (formatter != null)
            {
                Logs.Add(formatter(state, exception));
            }
        }
    }

}
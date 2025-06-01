// Copyright (c) CloudNimble, Inc. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Microsoft.Extensions.Logging;
using System.Collections.Generic;

namespace CloudNimble.WebJobs.Extensions.Tests.Common.Models
{
    /// <summary>
    /// Test implementation of ILoggerFactory for testing purposes.
    /// </summary>
    public class TestLoggerFactory : ILoggerFactory
    {
        private readonly Dictionary<string, object> _loggers = new Dictionary<string, object>();

        public void AddProvider(ILoggerProvider provider) { }

        public ILogger CreateLogger(string categoryName)
        {
            if (!_loggers.ContainsKey(categoryName))
            {
                _loggers[categoryName] = new TestLogger();
            }
            return (TestLogger)_loggers[categoryName];
        }

        public ILogger<T> CreateLogger<T>()
        {
            var categoryName = typeof(T).FullName;
            if (!_loggers.ContainsKey(categoryName))
            {
                _loggers[categoryName] = new TestLogger<T>();
            }
            return (TestLogger<T>)_loggers[categoryName];
        }

        public void Dispose() { }
    }

}
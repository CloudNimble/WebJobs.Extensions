// Copyright (c) CloudNimble, Inc. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Microsoft.Extensions.Logging;

namespace CloudNimble.WebJobs.Extensions.Tests.Common.Models
{
    /// <summary>
    /// Test implementation of ILoggerFactory for testing purposes.
    /// </summary>
    internal class TestLoggerFactory : ILoggerFactory
    {
        public void AddProvider(ILoggerProvider provider) { }

        public ILogger CreateLogger(string categoryName)
        {
            return new TestLogger();
        }

        public ILogger<T> CreateLogger<T>()
        {
            return new TestLogger<T>();
        }

        public void Dispose() { }
    }

}
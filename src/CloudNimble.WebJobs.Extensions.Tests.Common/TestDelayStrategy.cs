// Copyright (c) CloudNimble, Inc. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using CloudNimble.WebJobs.Extensions.Common.Timers;
using System;

namespace CloudNimble.WebJobs.Extensions.Tests.Common.Commands
{
    #region Test Helper Classes

    /// <summary>
    /// Test implementation of IDelayStrategy for testing purposes.
    /// </summary>
    internal class TestDelayStrategy : IDelayStrategy
    {
        public TimeSpan NextDelay { get; set; } = TimeSpan.FromSeconds(30);
        public bool LastExecutionSucceeded { get; private set; }
        public int GetNextDelayCallCount { get; private set; }

        public TimeSpan GetNextDelay(bool executionSucceeded)
        {
            LastExecutionSucceeded = executionSucceeded;
            GetNextDelayCallCount++;
            return NextDelay;
        }
    }

    #endregion

}
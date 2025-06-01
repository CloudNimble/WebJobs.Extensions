// Copyright (c) CloudNimble, Inc. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using CloudNimble.WebJobs.Extensions.Common.Timers;
using Microsoft.Azure.WebJobs.Host.Timers;
using System;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;

namespace CloudNimble.WebJobs.Extensions.Tests.Common.Timers
{

    #region Test Helper Classes

    /// <summary>
    /// Test implementation of IRecurrentCommand for testing purposes.
    /// </summary>
    internal class TestRecurrentCommand : IRecurrentCommand
    {
        public int ExecuteCallCount { get; private set; }
        public bool ThrowOnExecute { get; set; }
        public Exception ExceptionToThrow { get; set; } = new InvalidOperationException("Test exception");
        public TimeSpan ExecuteDelay { get; set; } = TimeSpan.Zero;
        public bool ReturnValue { get; set; } = true;

        public async Task<bool> TryExecuteAsync(CancellationToken cancellationToken)
        {
            ExecuteCallCount++;
            
            if (ExecuteDelay > TimeSpan.Zero)
            {
                await Task.Delay(ExecuteDelay, cancellationToken);
            }

            if (ThrowOnExecute)
            {
                throw ExceptionToThrow;
            }

            return ReturnValue;
        }
    }


    /// <summary>
    /// Test implementation of ITaskSeriesCommand for testing purposes.
    /// </summary>
    internal class TestTaskSeriesCommand : ITaskSeriesCommand
    {
        public int ExecuteCallCount { get; private set; }
        public TaskSeriesCommandResult NextResult { get; set; } = new TaskSeriesCommandResult(wait: Task.Delay(100));
        public bool ThrowOnExecute { get; set; }
        public Exception ExceptionToThrow { get; set; } = new InvalidOperationException("Test exception");

        public Task<TaskSeriesCommandResult> ExecuteAsync(CancellationToken cancellationToken)
        {
            ExecuteCallCount++;

            if (ThrowOnExecute)
            {
                throw ExceptionToThrow;
            }

            return Task.FromResult(NextResult);
        }
    }

    #endregion

}
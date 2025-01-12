// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Threading;
using System.Threading.Tasks;

namespace CloudNimble.WebJobs.Extensions.Common.Timers
{

    /// <summary>
    /// Defines a timer that executes a series of tasks.
    /// </summary>
    public interface ITaskSeriesTimer : IDisposable
    {
        /// <summary>
        /// Starts the timer.
        /// </summary>
        void Start();

        /// <summary>
        /// Stops the timer asynchronously.
        /// </summary>
        /// <param name="cancellationToken">A token to cancel the stop operation.</param>
        /// <returns>A task that represents the asynchronous stop operation.</returns>
        Task StopAsync(CancellationToken cancellationToken);

        /// <summary>
        /// Cancels the timer.
        /// </summary>
        void Cancel();

    }

}

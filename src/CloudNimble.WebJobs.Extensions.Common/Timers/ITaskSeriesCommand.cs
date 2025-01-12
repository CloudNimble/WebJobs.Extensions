// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Threading;
using System.Threading.Tasks;

namespace CloudNimble.WebJobs.Extensions.Common.Timers
{
    /// <summary>
    /// Represents a command that can be executed as part of a task series.
    /// </summary>
    public interface ITaskSeriesCommand
    {
        /// <summary>
        /// Executes the command asynchronously.
        /// </summary>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>A <see cref="TaskSeriesCommandResult"/> that represents the result of the command execution.</returns>
        Task<TaskSeriesCommandResult> ExecuteAsync(CancellationToken cancellationToken);
    }
}

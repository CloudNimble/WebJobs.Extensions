// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Threading.Tasks;

namespace CloudNimble.WebJobs.Extensions.Common.Timers
{

    /// <summary>
    /// Represents the result of a task series command, containing a task to wait for before executing the next command.
    /// </summary>
    public struct TaskSeriesCommandResult
    {

        private readonly Task _wait;

        /// <summary>
        /// Initializes a new instance of the <see cref="TaskSeriesCommandResult"/> struct with the specified task.
        /// </summary>
        /// <param name="wait">The task to wait for before executing the next command.</param>
        public TaskSeriesCommandResult(Task wait)
        {
            _wait = wait;
        }

        /// <summary>
        /// Gets the task to wait for before calling <see cref="ITaskSeriesCommand.ExecuteAsync"/> again.
        /// </summary>
        public readonly Task Wait => _wait;
    }

}

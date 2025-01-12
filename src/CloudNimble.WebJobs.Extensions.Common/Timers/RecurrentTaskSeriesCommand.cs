// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Threading;
using System.Threading.Tasks;

namespace CloudNimble.WebJobs.Extensions.Common.Timers
{
    /// <summary>
    /// Represents a command that executes a recurrent task series with a specified delay strategy.
    /// </summary>
    public class RecurrentTaskSeriesCommand : ITaskSeriesCommand
    {
        private readonly IRecurrentCommand _innerCommand;
        private readonly IDelayStrategy _delayStrategy;

        /// <summary>
        /// Initializes a new instance of the <see cref="RecurrentTaskSeriesCommand"/> class.
        /// </summary>
        /// <param name="innerCommand">The recurrent command to be executed.</param>
        /// <param name="delayStrategy">The strategy for calculating the delay before the next execution attempt.</param>
        public RecurrentTaskSeriesCommand(IRecurrentCommand innerCommand, IDelayStrategy delayStrategy)
        {
            _innerCommand = innerCommand;
            _delayStrategy = delayStrategy;
        }

        /// <summary>
        /// Executes the recurrent command and returns the result with a delay before the next execution.
        /// </summary>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>A <see cref="TaskSeriesCommandResult"/> that contains the task to wait for before the next execution.</returns>
        public async Task<TaskSeriesCommandResult> ExecuteAsync(CancellationToken cancellationToken)
        {
            var succeeded = await _innerCommand.TryExecuteAsync(cancellationToken).ConfigureAwait(false);
            var wait = Task.Delay(_delayStrategy.GetNextDelay(succeeded), cancellationToken);
            return new TaskSeriesCommandResult(wait);
        }
    }
}

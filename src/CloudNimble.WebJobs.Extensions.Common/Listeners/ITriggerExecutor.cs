// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs.Host.Executors;

namespace CloudNimble.WebJobs.Extensions.Common.Listeners
{

    /// <summary>
    /// Defines an interface for executing a trigger with a specified value.
    /// </summary>
    /// <typeparam name="TTriggerValue">The type of the trigger value.</typeparam>
    public interface ITriggerExecutor<TTriggerValue>
    {

        /// <summary>
        /// Executes the trigger with the specified value.
        /// </summary>
        /// <param name="value">The value of the trigger.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the function result.</returns>
        Task<FunctionResult> ExecuteAsync(TTriggerValue value, CancellationToken cancellationToken);

    }

}

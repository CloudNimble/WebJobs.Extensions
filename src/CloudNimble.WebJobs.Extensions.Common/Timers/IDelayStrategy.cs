// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;

namespace CloudNimble.WebJobs.Extensions.Common.Timers
{

    /// <summary>
    /// Defines a strategy for calculating the delay before the next execution attempt.
    /// </summary>
    public interface IDelayStrategy
    {

        /// <summary>
        /// Gets the delay before the next execution attempt.
        /// </summary>
        /// <param name="executionSucceeded">A boolean indicating whether the previous execution succeeded.</param>
        /// <returns>A <see cref="TimeSpan"/> representing the delay before the next execution attempt.</returns>
        TimeSpan GetNextDelay(bool executionSucceeded);

    }

}

// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs.Host.Timers;

namespace CloudNimble.WebJobs.Extensions.Common.Timers
{

    /// <summary>
    /// Implements a delay strategy that linearly reduces the delay interval after each failure until a minimum interval is reached.
    /// </summary>
    public class LinearSpeedupStrategy : IDelayStrategy
    {
        private readonly TimeSpan _normalInterval;
        private readonly TimeSpan _minimumInterval;
        private readonly int _failureSpeedupDivisor;
        private TimeSpan _currentInterval;

        /// <summary>
        /// Initializes a new instance of the <see cref="LinearSpeedupStrategy"/> class with the specified normal and minimum intervals.
        /// </summary>
        /// <param name="normalInterval">The normal interval between executions.</param>
        /// <param name="minimumInterval">The minimum interval between executions.</param>
        public LinearSpeedupStrategy(TimeSpan normalInterval, TimeSpan minimumInterval)
            : this(normalInterval, minimumInterval, 2)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="LinearSpeedupStrategy"/> class with the specified normal and minimum intervals, and a failure speedup divisor.
        /// </summary>
        /// <param name="normalInterval">The normal interval between executions.</param>
        /// <param name="minimumInterval">The minimum interval between executions.</param>
        /// <param name="failureSpeedupDivisor">The divisor used to reduce the interval after each failure.</param>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when the normalInterval or minimumInterval is negative, or when the failureSpeedupDivisor is less than 1.</exception>
        /// <exception cref="ArgumentException">Thrown when the minimumInterval is greater than the normalInterval.</exception>
        public LinearSpeedupStrategy(TimeSpan normalInterval, TimeSpan minimumInterval, int failureSpeedupDivisor)
        {
            if (normalInterval.Ticks < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(normalInterval), "The TimeSpan must not be negative.");
            }

            if (minimumInterval.Ticks < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(minimumInterval), "The TimeSpan must not be negative.");
            }

            if (minimumInterval.Ticks > normalInterval.Ticks)
            {
                throw new ArgumentException("The minimumInterval must not be greater than the normalInterval.",
                    nameof(minimumInterval));
            }

            if (failureSpeedupDivisor < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(failureSpeedupDivisor),
                    "The failureSpeedupDivisor must not be less than 1.");
            }

            _normalInterval = normalInterval;
            _minimumInterval = minimumInterval;
            _failureSpeedupDivisor = failureSpeedupDivisor;
            _currentInterval = normalInterval;
        }

        /// <summary>
        /// Gets the delay before the next execution attempt.
        /// </summary>
        /// <param name="executionSucceeded">A boolean indicating whether the previous execution succeeded.</param>
        /// <returns>A <see cref="TimeSpan"/> representing the delay before the next execution attempt.</returns>
        public TimeSpan GetNextDelay(bool executionSucceeded)
        {
            if (executionSucceeded)
            {
                _currentInterval = _normalInterval;
            }
            else
            {
                var speedupInterval = new TimeSpan(_currentInterval.Ticks / _failureSpeedupDivisor);
                _currentInterval = Max(speedupInterval, _minimumInterval);
            }

            return _currentInterval;
        }

        /// <summary>
        /// Returns the maximum of two <see cref="TimeSpan"/> values.
        /// </summary>
        /// <param name="x">The first <see cref="TimeSpan"/> value.</param>
        /// <param name="y">The second <see cref="TimeSpan"/> value.</param>
        /// <returns>The maximum of the two <see cref="TimeSpan"/> values.</returns>
        private static TimeSpan Max(TimeSpan x, TimeSpan y)
        {
            return x.Ticks > y.Ticks ? x : y;
        }

        /// <summary>
        /// Creates a new <see cref="ITaskSeriesTimer"/> with the specified command, normal interval, minimum interval, and exception handler.
        /// </summary>
        /// <param name="command">The command to be executed by the timer.</param>
        /// <param name="normalInterval">The normal interval between executions.</param>
        /// <param name="minimumInterval">The minimum interval between executions.</param>
        /// <param name="exceptionHandler">The exception handler to be used by the timer.</param>
        /// <returns>A new <see cref="ITaskSeriesTimer"/> instance.</returns>
        public static ITaskSeriesTimer CreateTimer(IRecurrentCommand command, TimeSpan normalInterval,
            TimeSpan minimumInterval, IWebJobsExceptionHandler exceptionHandler)
        {
            return new TaskSeriesTimer(new RecurrentTaskSeriesCommand(command, new LinearSpeedupStrategy(normalInterval, minimumInterval)), exceptionHandler, Task.Delay(normalInterval));
        }

    }

}

// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;

namespace CloudNimble.WebJobs.Extensions.Common.Timers
{
    /// <summary>
    /// Implements a randomized exponential backoff strategy for calculating delays between execution attempts.
    /// </summary>
    public class RandomizedExponentialBackoffStrategy : IDelayStrategy
    {
        /// <summary>
        /// The factor used to randomize the backoff interval.
        /// </summary>
        public const double RandomizationFactor = 0.2;

        private readonly TimeSpan _minimumInterval;
        private readonly TimeSpan _maximumInterval;
        private readonly TimeSpan _deltaBackoff;

        private TimeSpan _currentInterval;
        private uint _backoffExponent;
        private Random _random;

        /// <summary>
        /// Initializes a new instance of the <see cref="RandomizedExponentialBackoffStrategy"/> class with specified minimum and maximum intervals.
        /// </summary>
        /// <param name="minimumInterval">The minimum interval for the backoff strategy.</param>
        /// <param name="maximumInterval">The maximum interval for the backoff strategy.</param>
        public RandomizedExponentialBackoffStrategy(TimeSpan minimumInterval, TimeSpan maximumInterval)
            : this(minimumInterval, maximumInterval, minimumInterval)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RandomizedExponentialBackoffStrategy"/> class with specified minimum, maximum, and delta backoff intervals.
        /// </summary>
        /// <param name="minimumInterval">The minimum interval for the backoff strategy.</param>
        /// <param name="maximumInterval">The maximum interval for the backoff strategy.</param>
        /// <param name="deltaBackoff">The delta backoff interval for the strategy.</param>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when the minimum or maximum interval is negative.</exception>
        /// <exception cref="ArgumentException">Thrown when the minimum interval is greater than the maximum interval.</exception>
        public RandomizedExponentialBackoffStrategy(TimeSpan minimumInterval, TimeSpan maximumInterval,
            TimeSpan deltaBackoff)
        {
            if (minimumInterval.Ticks < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(minimumInterval), "The TimeSpan must not be negative.");
            }

            if (maximumInterval.Ticks < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(maximumInterval), "The TimeSpan must not be negative.");
            }

            if (minimumInterval.Ticks > maximumInterval.Ticks)
            {
                throw new ArgumentException("The minimumInterval must not be greater than the maximumInterval.",
                    nameof(minimumInterval));
            }

            _minimumInterval = minimumInterval;
            _maximumInterval = maximumInterval;
            _deltaBackoff = deltaBackoff;
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
                _currentInterval = _minimumInterval;
                _backoffExponent = 1;
            }
            else if (_currentInterval != _maximumInterval)
            {
                var backoffInterval = _minimumInterval;

                if (_backoffExponent > 0)
                {
                    _random ??= new Random();

                    var incrementMsec = _random.Next(1.0 - RandomizationFactor, 1.0 + RandomizationFactor) *
                        Math.Pow(2.0, _backoffExponent - 1) *
                        _deltaBackoff.TotalMilliseconds;
                    backoffInterval += TimeSpan.FromMilliseconds(incrementMsec);
                }

                if (backoffInterval < _maximumInterval)
                {
                    _currentInterval = backoffInterval;
                    _backoffExponent++;
                }
                else
                {
                    _currentInterval = _maximumInterval;
                }
            }

            // else do nothing and keep current interval equal to max
            return _currentInterval;
        }
    }
}

// Copyright (c) CloudNimble, Inc. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;

namespace CloudNimble.WebJobs.Extensions.Tests.Amazon.Integration
{

    /// <summary>
    /// Provides retry functionality for integration tests.
    /// </summary>
    public static class RetryHelper
    {

        /// <summary>
        /// Executes an action with retry logic.
        /// </summary>
        /// <param name="action">The action to execute.</param>
        /// <param name="maxAttempts">The maximum number of attempts.</param>
        /// <param name="delay">The delay between attempts.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        public static async Task ExecuteWithRetryAsync(
            Func<Task> action,
            int maxAttempts = 3,
            TimeSpan? delay = null,
            CancellationToken cancellationToken = default)
        {
            delay ??= TimeSpan.FromSeconds(1);
            
            Exception lastException = null;
            
            for (int attempt = 1; attempt <= maxAttempts; attempt++)
            {
                try
                {
                    await action();
                    return;
                }
                catch (Exception ex)
                {
                    lastException = ex;
                    
                    if (attempt < maxAttempts)
                    {
                        var waitTime = TimeSpan.FromMilliseconds(delay.Value.TotalMilliseconds * attempt);
                        await Task.Delay(waitTime, cancellationToken);
                    }
                }
            }
            
            throw new AggregateException($"Failed after {maxAttempts} attempts", lastException);
        }

        /// <summary>
        /// Executes a function with retry logic and returns the result.
        /// </summary>
        /// <typeparam name="T">The type of the result.</typeparam>
        /// <param name="func">The function to execute.</param>
        /// <param name="maxAttempts">The maximum number of attempts.</param>
        /// <param name="delay">The delay between attempts.</param>
        /// <param name="shouldRetry">A function to determine if a retry should be attempted based on the exception.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The result of the function.</returns>
        public static async Task<T> ExecuteWithRetryAsync<T>(
            Func<Task<T>> func,
            int maxAttempts = 3,
            TimeSpan? delay = null,
            Func<Exception, bool> shouldRetry = null,
            CancellationToken cancellationToken = default)
        {
            delay ??= TimeSpan.FromSeconds(1);
            shouldRetry ??= _ => true;
            
            Exception lastException = null;
            
            for (int attempt = 1; attempt <= maxAttempts; attempt++)
            {
                try
                {
                    return await func();
                }
                catch (Exception ex)
                {
                    lastException = ex;
                    
                    if (attempt < maxAttempts && shouldRetry(ex))
                    {
                        var waitTime = TimeSpan.FromMilliseconds(delay.Value.TotalMilliseconds * attempt);
                        await Task.Delay(waitTime, cancellationToken);
                    }
                    else
                    {
                        throw;
                    }
                }
            }
            
            throw new AggregateException($"Failed after {maxAttempts} attempts", lastException);
        }

        /// <summary>
        /// Waits for a condition to become true with timeout.
        /// </summary>
        /// <param name="condition">The condition to check.</param>
        /// <param name="timeout">The timeout period.</param>
        /// <param name="pollingInterval">The interval between checks.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>True if the condition was met; otherwise, false.</returns>
        public static async Task<bool> WaitForConditionAsync(
            Func<Task<bool>> condition,
            TimeSpan? timeout = null,
            TimeSpan? pollingInterval = null,
            CancellationToken cancellationToken = default)
        {
            timeout ??= TimeSpan.FromSeconds(30);
            pollingInterval ??= TimeSpan.FromMilliseconds(500);
            
            var endTime = DateTime.UtcNow.Add(timeout.Value);
            
            while (DateTime.UtcNow < endTime && !cancellationToken.IsCancellationRequested)
            {
                if (await condition())
                {
                    return true;
                }
                
                await Task.Delay(pollingInterval.Value, cancellationToken);
            }
            
            return false;
        }

    }

}
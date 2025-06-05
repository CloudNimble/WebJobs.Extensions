// Copyright (c) CloudNimble, Inc. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace CloudNimble.WebJobs.Extensions.Tests.Amazon.Integration
{

    /// <summary>
    /// Provides health check functionality for LocalStack services.
    /// </summary>
    public static class LocalStackHealthCheck
    {

        /// <summary>
        /// Checks if LocalStack is healthy and ready to accept requests.
        /// </summary>
        /// <param name="endpoint">The LocalStack endpoint URL.</param>
        /// <param name="timeout">The timeout for the health check.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>True if LocalStack is healthy; otherwise, false.</returns>
        public static async Task<bool> IsHealthyAsync(
            string endpoint = "http://localhost:4566", 
            TimeSpan? timeout = null,
            CancellationToken cancellationToken = default)
        {
            timeout ??= TimeSpan.FromSeconds(5);
            
            using var httpClient = new HttpClient { Timeout = timeout.Value };
            
            try
            {
                var response = await httpClient.GetAsync($"{endpoint}/_localstack/health", cancellationToken);
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Waits for LocalStack to become healthy with retry logic.
        /// </summary>
        /// <param name="endpoint">The LocalStack endpoint URL.</param>
        /// <param name="maxWaitTime">The maximum time to wait.</param>
        /// <param name="retryInterval">The interval between retries.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>True if LocalStack became healthy within the timeout; otherwise, false.</returns>
        public static async Task<bool> WaitForHealthyAsync(
            string endpoint = "http://localhost:4566",
            TimeSpan? maxWaitTime = null,
            TimeSpan? retryInterval = null,
            CancellationToken cancellationToken = default)
        {
            maxWaitTime ??= TimeSpan.FromMinutes(2);
            retryInterval ??= TimeSpan.FromSeconds(2);
            
            var endTime = DateTime.UtcNow.Add(maxWaitTime.Value);
            
            while (DateTime.UtcNow < endTime && !cancellationToken.IsCancellationRequested)
            {
                if (await IsHealthyAsync(endpoint, TimeSpan.FromSeconds(5), cancellationToken))
                {
                    return true;
                }
                
                await Task.Delay(retryInterval.Value, cancellationToken);
            }
            
            return false;
        }

        /// <summary>
        /// Gets detailed health information about LocalStack services.
        /// </summary>
        /// <param name="endpoint">The LocalStack endpoint URL.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The health check response or null if unavailable.</returns>
        public static async Task<string> GetHealthDetailsAsync(
            string endpoint = "http://localhost:4566",
            CancellationToken cancellationToken = default)
        {
            using var httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
            
            try
            {
                return await httpClient.GetStringAsync($"{endpoint}/_localstack/health", cancellationToken);
            }
            catch (Exception ex)
            {
                return $"Failed to get health details: {ex.Message}";
            }
        }

    }

}
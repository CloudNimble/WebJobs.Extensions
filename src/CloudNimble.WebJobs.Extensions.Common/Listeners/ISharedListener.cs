// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Threading;
using System.Threading.Tasks;

namespace CloudNimble.WebJobs.Extensions.Common.Listeners
{

    /// <summary>
    /// Defines a shared listener interface that provides methods to manage the lifecycle of listeners.
    /// </summary>
    public interface ISharedListener : IDisposable
    {

        /// <summary>
        /// Ensures that all listeners are canceled.
        /// </summary>
        void EnsureAllCanceled();

        /// <summary>
        /// Ensures that all listeners are started asynchronously.
        /// </summary>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>A task that represents the asynchronous start operation.</returns>
        Task EnsureAllStartedAsync(CancellationToken cancellationToken);

        /// <summary>
        /// Ensures that all listeners are stopped asynchronously.
        /// </summary>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>A task that represents the asynchronous stop operation.</returns>
        Task EnsureAllStoppedAsync(CancellationToken cancellationToken);

        /// <summary>
        /// Ensures that all listeners are disposed.
        /// </summary>
        void EnsureAllDisposed();

    }

}

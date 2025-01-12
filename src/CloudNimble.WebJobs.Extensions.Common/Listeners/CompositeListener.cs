// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs.Host.Listeners;

namespace CloudNimble.WebJobs.Extensions.Common.Listeners
{
    /// <summary>
    /// Represents a composite listener that manages multiple <see cref="IListener"/> instances.
    /// </summary>
    public sealed class CompositeListener : IListener, IEnumerable<IListener>
    {
        private readonly IEnumerable<IListener> _listeners;
        private bool _disposed;

        /// <summary>
        /// Initializes a new instance of the <see cref="CompositeListener"/> class with the specified listeners.
        /// </summary>
        /// <param name="listeners">The listeners to be managed by this composite listener.</param>
        public CompositeListener(params IListener[] listeners)
            : this((IEnumerable<IListener>)listeners)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CompositeListener"/> class with the specified listeners.
        /// </summary>
        /// <param name="listeners">The listeners to be managed by this composite listener.</param>
        public CompositeListener(IEnumerable<IListener> listeners)
        {
            _listeners = listeners;
        }

        /// <summary>
        /// Starts all listeners asynchronously.
        /// </summary>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>A task that represents the asynchronous start operation.</returns>
        public async Task StartAsync(CancellationToken cancellationToken)
        {
            ThrowIfDisposed();

            // start all listeners in parallel
            var tasks = new List<Task>();
            foreach (var listener in _listeners)
            {
                tasks.Add(listener.StartAsync(cancellationToken));
            }

            await Task.WhenAll(tasks).ConfigureAwait(false);
        }

        /// <summary>
        /// Stops all listeners asynchronously.
        /// </summary>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>A task that represents the asynchronous stop operation.</returns>
        public async Task StopAsync(CancellationToken cancellationToken)
        {
            ThrowIfDisposed();

            // stop all listeners in parallel
            var tasks = new List<Task>();
            foreach (IListener listener in _listeners)
            {
                tasks.Add(listener.StopAsync(cancellationToken));
            }

            await Task.WhenAll(tasks).ConfigureAwait(false);
        }

        /// <summary>
        /// Cancels all listeners.
        /// </summary>
        public void Cancel()
        {
            ThrowIfDisposed();

            foreach (IListener listener in _listeners)
            {
                listener.Cancel();
            }
        }

        /// <summary>
        /// Disposes all listeners.
        /// </summary>
        public void Dispose()
        {
            if (!_disposed)
            {
                foreach (IListener listener in _listeners)
                {
                    listener.Dispose();
                }

                _disposed = true;
            }
        }

        /// <summary>
        /// Throws an <see cref="ObjectDisposedException"/> if the object has been disposed.
        /// </summary>
        private void ThrowIfDisposed()
        {
            ObjectDisposedException.ThrowIf(_disposed, null);
        }

        /// <summary>
        /// Returns an enumerator that iterates through the collection of listeners.
        /// </summary>
        /// <returns>An enumerator for the collection of listeners.</returns>
        public IEnumerator<IListener> GetEnumerator()
        {
            return _listeners.GetEnumerator();
        }

        /// <summary>
        /// Returns an enumerator that iterates through the collection of listeners.
        /// </summary>
        /// <returns>An enumerator for the collection of listeners.</returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

    }
}

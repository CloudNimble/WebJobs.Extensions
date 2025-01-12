// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;
using CloudNimble.EasyAF.Core;
using Microsoft.Azure.WebJobs.Host.Timers;

namespace CloudNimble.WebJobs.Extensions.Common.Timers
{
    /// <summary>
    /// Represents a timer that executes one task after another in a series.
    /// </summary>
    public sealed class TaskSeriesTimer : ITaskSeriesTimer
    {
        private readonly ITaskSeriesCommand _command;
        private readonly IWebJobsExceptionHandler _exceptionHandler;
        private readonly Task _initialWait;
        private readonly CancellationTokenSource _cancellationTokenSource;

        private bool _started;
        private bool _stopped;
        private Task _run;
        private bool _disposed;

        /// <summary>
        /// Initializes a new instance of the <see cref="TaskSeriesTimer"/> class with the specified command, exception handler, and initial wait task.
        /// </summary>
        /// <param name="command">The command to execute in the task series.</param>
        /// <param name="exceptionHandler">The exception handler to use for handling unhandled exceptions.</param>
        /// <param name="initialWait">The initial task to wait for before starting the task series.</param>
        /// <exception cref="ArgumentNullException">Thrown if any of the arguments are null.</exception>
        public TaskSeriesTimer(ITaskSeriesCommand command, IWebJobsExceptionHandler exceptionHandler,
            Task initialWait)
        {
            Ensure.ArgumentNotNull(command, nameof(command));
            Ensure.ArgumentNotNull(exceptionHandler, nameof(exceptionHandler));
            Ensure.ArgumentNotNull(initialWait, nameof(initialWait));

            _command = command;
            _exceptionHandler = exceptionHandler;
            _initialWait = initialWait;
            _cancellationTokenSource = new CancellationTokenSource();
        }

        /// <summary>
        /// Starts the timer. This method initializes the asynchronous execution of tasks in a series.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown if the timer has already been started.</exception>
        /// <exception cref="ObjectDisposedException">Thrown if the timer has been disposed.</exception>
        public void Start()
        {
            ThrowIfDisposed();

            if (_started)
            {
                throw new InvalidOperationException("The timer has already been started; it cannot be restarted.");
            }

            _run = RunAsync(_cancellationTokenSource.Token);
            _started = true;
        }

        /// <summary>
        /// Stops the timer asynchronously.
        /// </summary>
        /// <param name="cancellationToken">A token to cancel the stop operation.</param>
        /// <returns>A task that represents the asynchronous stop operation.</returns>
        /// <exception cref="InvalidOperationException">Thrown if the timer has not yet been started or has already been stopped.</exception>
        /// <exception cref="ObjectDisposedException">Thrown if the timer has been disposed.</exception>
        public Task StopAsync(CancellationToken cancellationToken)
        {
            ThrowIfDisposed();

            if (!_started)
            {
                throw new InvalidOperationException("The timer has not yet been started.");
            }

            if (_stopped)
            {
                throw new InvalidOperationException("The timer has already been stopped.");
            }

            _cancellationTokenSource.Cancel();
            return StopAsyncCore(cancellationToken);
        }

        private async Task StopAsyncCore(CancellationToken cancellationToken)
        {
            await Task.Delay(0, CancellationToken.None).ConfigureAwait(false);
            TaskCompletionSource<object> cancellationTaskSource = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);

            using (cancellationToken.Register(() => cancellationTaskSource.SetCanceled()))
            {
                // Wait for all pending command tasks to complete (or cancellation of the token) before returning.
                await Task.WhenAny(_run, cancellationTaskSource.Task).ConfigureAwait(false);
            }

            _stopped = true;
        }


        /// <summary>
        /// Cancels the timer by signaling the cancellation token.
        /// </summary>
        /// <exception cref="ObjectDisposedException">Thrown if the timer has been disposed.</exception>
        public void Cancel()
        {
            ThrowIfDisposed();
            _cancellationTokenSource.Cancel();
        }

        /// <summary>
        /// Disposes the timer, releasing any resources it holds.
        /// </summary>
        /// <remarks>
        /// This method cancels the timer and disposes of the cancellation token source.
        /// It ensures that the timer is not disposed of while callers are still using the cancellation token,
        /// to avoid ObjectDisposedException when calling token.Register.
        /// </remarks>
        public void Dispose()
        {
            if (!_disposed)
            {
                // Running callers might still be using the cancellation token.
                // Mark it canceled but don't dispose of the source while the callers are running.
                // Otherwise, callers would receive ObjectDisposedException when calling token.Register.
                // For now, rely on finalization to clean up _cancellationTokenSource's wait handle (if allocated).
                _cancellationTokenSource.Cancel();
                _cancellationTokenSource.Dispose();

                _disposed = true;
            }
        }

        private async Task RunAsync(CancellationToken cancellationToken)
        {
            try
            {
                // Allow Start to return immediately without waiting for any initial iteration work to start.
                await Task.Yield();

                Task wait = _initialWait;

                // Execute tasks one at a time (in a series) until stopped.
                while (!cancellationToken.IsCancellationRequested)
                {
                    var cancellationTaskSource = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);

                    using (cancellationToken.Register(() => cancellationTaskSource.SetCanceled()))
                    {
                        try
                        {
                            await Task.WhenAny(wait, cancellationTaskSource.Task).ConfigureAwait(false);
                        }
                        catch (OperationCanceledException)
                        {
                            // When Stop fires, don't make it wait for wait before it can return.
                        }
                    }

                    if (cancellationToken.IsCancellationRequested)
                    {
                        break;
                    }

                    try
                    {
                        var result = await _command.ExecuteAsync(cancellationToken).ConfigureAwait(false);
                        wait = result.Wait;
                    }
                    catch (Exception ex) when (ex.InnerException is OperationCanceledException)
                    {
                        // OperationCanceledExceptions coming from storage are wrapped in a StorageException.
                        // We'll handle them all here so they don't have to be managed for every call.
                    }
                    catch (OperationCanceledException)
                    {
                        // Don't fail the task, throw a background exception, or stop looping when a task cancels.
                    }
                }
            }
            catch (Exception exception)
            {
                // Immediately report any unhandled exception from this background task.
                // (Don't capture the exception as a fault of this Task; that would delay any exception reporting until
                // Stop is called, which might never happen.)
#pragma warning disable AZC0103 // Do not wait synchronously in asynchronous scope.
                _exceptionHandler.OnUnhandledExceptionAsync(ExceptionDispatchInfo.Capture(exception)).GetAwaiter().GetResult();
#pragma warning restore AZC0103 // Do not wait synchronously in asynchronous scope.
            }
        }

        private void ThrowIfDisposed()
        {
            ObjectDisposedException.ThrowIf(_disposed, null);
        }
    }
}

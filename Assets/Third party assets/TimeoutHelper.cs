using System;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;

namespace JurassicTimeoutHelper
{
    /// <summary>
    /// Allows to limit execution time of a Jurassic Script by internally using the
    /// technique of aborting a thread.
    /// </summary>
    public class ScriptTimeoutHelper
    {
        private HandlerState currentState;

        public ScriptTimeoutHelper()
        {
        }

        /// <summary>
        /// Runs the specified <see cref="Action"/> in the current thread,
        /// applying the given timeout.
        /// When the handler times out, a <see cref="ThreadAbortException"/> is
        /// raised in the current thread to break. However, this is managed
        /// internally so it does not affect the caller of this method (i.e. it is
        /// ensured that a <see cref="ThreadAbortException"/> does not flow through
        /// this method or raised after this method returns).
        /// </summary>
        /// <exception cref="TimeoutException">Thrown when the handler times 
        /// out.</exception>
        /// <param name="handler"></param>
        /// <param name="timeout"></param>
        public void RunWithTimeout(Action handler, int timeout)
        {
            if (currentState != null)
                throw new InvalidOperationException(
                    $"Cannot recursively call {nameof(RunWithTimeout)}.");
            if (handler == null)
                throw new ArgumentException(nameof(handler));

            // Throw the TimeoutException immediately when the timeout is 0.
            if (timeout == 0)
                throw new TimeoutException();

            ExceptionDispatchInfo caughtException = null;

            using (var state = currentState =
                new HandlerState(Thread.CurrentThread))
            {
                /* Start a monitoring task that may abort the current thread after
                 * the specified time limit.
                 * Note that the task will start immediately. Therefore we need to
                 * ensure the task does not abort the thread until we entered the
                 * try clause; otherwise the ThreadAbortException might fly through
                 * the caller of this method. To ensure this, the monitoring task
                 * waits until we release the semaphore the first time before
                 * actually waiting for the specified time.
                */
                using (var monitoringTask = Task.Run(async () =>
                    await RunMonitoringTask(state, timeout)))
                {
                    try
                    {
                        bool waitForAbortException;
                        try
                        {
                            // Allow the monitoring task to begin by releasing the
                            // semaphore the first time.
                            // Do this in a finally block to ensure if this thread
                            // is aborted by other code, the semaphore is still
                            // released.
                            try { }
                            finally
                            {
                                state.WaitSemaphore.Release();
                            }

                            // Execute the handler.
                            handler();
                        }
                        catch (Exception ex) when (!(ex is ThreadAbortException))
                        {
                            /* Need to catch all exceptions (except our own
                             * ThreadAbortException) because we may wait for a
                             * ThreadAbortException to be thrown which is not
                             * possible in a finally handler.
                             */
                            caughtException = ExceptionDispatchInfo.Capture(ex);
                        }
                        finally
                        {
                            /* Indicate that the handler is completed, and check
                             * if we need to wait for the ThreadAbortException.
                             * This is done in a finally handler to ensure when
                             * other code wants to abort this thread, the thread
                             * actually will abort as expected but we still can
                             * notify the monitoring task that we already returned.
                             */
                            lock (state)
                            {
                                state.IsExited = true;
                                waitForAbortException =
                                    state.AbortState == AbortState.IsAborting;

                                if (state.AbortState == AbortState.None)
                                {
                                    // If the monitoring task did not do anything
                                    // yet, allow it to complete immediately.
                                    state.WaitSemaphore.Release();
                                }
                            }
                        }

                        if (waitForAbortException)
                        {
                            /* The monitoring task indicated that it will abort our
                             * thread (but the ThreadAbortException did not yet
                             * occur), so we need to wait for the
                             * ThreadAbortException.
                             * This wait is needed because otherwise we may return
                             * too early (and in the finally block we wait for the
                             * monitoring task, causing a deadlock).
                             */
                            Thread.Sleep(Timeout.Infinite);
                        }
                    }
                    catch (ThreadAbortException ex)
                        when (ex.ExceptionState == state)
                    {
                        // Reset the abort.
                        Thread.ResetAbort();

                        // Indicate that the timeout has been exceeded.
                        throw new TimeoutException();
                    }
                    finally
                    {
                        // Wait for the monitoring task to complete.
                        monitoringTask.Wait();
                        currentState = null;
                    }
                }
            }

            // Check if we need to rethrow a caught exception (preserving the
            // original stacktrace).
            if (caughtException != null)
                caughtException.Throw();
        }

        private async Task RunMonitoringTask(HandlerState state, int timeout)
        {
            // Wait until the handler thread entered the try-block.
            // Use a synchronous wait because we expect this to be a very short
            // period of time.
            state.WaitSemaphore.Wait();

            // Now asynchronously wait until the specified time has passed or the
            // semaphore has been released. In the latter case there is no need to
            // call AbortExecution().
            bool completed = await state.WaitSemaphore.WaitAsync(timeout);

            // Abort the handler thread.
            if (!completed)
                AbortExecution(state);
        }

        private void AbortExecution(HandlerState state)
        {
            bool canAbort;
            lock (state)
            {
                if (state.IsExited)
                {
                    // The handler has already exited.
                    return;
                }

                // Check if we can call Thread.Abort() or if the handler thread is
                // currently in a critical section and needs to abort himself when
                // leaving the critical section.
                canAbort = !state.IsCriticalSection;
                state.AbortState = canAbort ? AbortState.IsAborting
                    : AbortState.ShouldAbort;
            }
            if (canAbort)
            {
                /* The handler thread is not in a critical section so we can
                 * directly abort it.
                 * This needs to be done outside of the lock because Abort() could
                 * block if the  thread is currently in a finally handler (and
                 * trying to lock on the state object), which could lead to a
                 * deadlock.
                 */
                state.HandlerThread.Abort(state);
            }
        }

        /// <summary>
        /// Notifies this class that the handler thread is entering a critical
        /// section in which aborting the thread could corrupt the system's state.
        /// This means aborting the thread will be deferred until leaving the
        /// critical section.
        /// Note that you must call <see cref="ExitCriticalSection"/> in a
        /// <c>finally</c> block once the thread left the critical section.
        /// </summary>
        public void EnterCriticalSection()
        {
            if (currentState == null)
                throw new InvalidOperationException();

            bool waitForAbortException;
            lock (currentState)
            {
                if (Thread.CurrentThread != currentState.HandlerThread
                    || currentState.IsCriticalSection)
                    throw new InvalidOperationException();

                currentState.IsCriticalSection = true;
                waitForAbortException =
                    currentState.AbortState == AbortState.IsAborting;
            }
            if (waitForAbortException)
            {
                // The monitoring task indicated that it will abort our thread, so
                // we need to wait for the ThreadAbortException.
                Thread.Sleep(Timeout.Infinite);
            }
        }

        public void ExitCriticalSection()
        {
            if (currentState == null)
                throw new InvalidOperationException();

            bool shouldAbort;
            lock (currentState)
            {
                if (Thread.CurrentThread != currentState.HandlerThread
                    || !currentState.IsCriticalSection)
                    throw new InvalidOperationException();

                currentState.IsCriticalSection = false;
                shouldAbort = currentState.AbortState == AbortState.ShouldAbort;
            }
            if (shouldAbort)
            {
                // The monitoring task indicated that it wanted to abort our
                // thread while we were in a critical section, so we need to abort
                // ourselves.
                Thread.CurrentThread.Abort(currentState);
            }
        }

        private enum AbortState
        {
            /// <summary>
            /// Indicates that the monitoring task has not yet done any action.
            /// </summary>
            None = 0,

            /// <summary>
            /// Indicates that the monitoring task is about to abort the handler
            /// thread.
            /// </summary>
            IsAborting = 1,

            /// <summary>
            /// Indicates that the monitoring task wanted to abort the handler
            /// thread but the handler thread was in a critical section, and needs
            /// to abort itself when leaving the critical section.
            /// </summary>
            ShouldAbort = 2
        }

        private class HandlerState : IDisposable
        {

            public Thread HandlerThread { get; }

            public SemaphoreSlim WaitSemaphore { get; } = new SemaphoreSlim(0);

            public bool IsCriticalSection { get; set; }

            /// <summary>
            /// Indicates if the handler is already completed (so there's no need
            /// to abort the thread).
            /// This flag is set by the handler thread.
            /// </summary>
            public bool IsExited { get; set; }

            /// <summary>
            /// Indicates that the wait task wanted to abort the handler thread but
            /// the handler thread was in a critical section, and needs to abort
            /// itself when leaving the critical section.
            /// This flag is set by the wait task.
            /// </summary>
            public AbortState AbortState { get; set; }


            public HandlerState(Thread handlerThread)
            {
                this.HandlerThread = handlerThread;
            }

            ~HandlerState()
            {
                Dispose(false);
            }

            public void Dispose()
            {
                Dispose(true);
                GC.SuppressFinalize(this);
            }

            protected void Dispose(bool disposing)
            {
                if (disposing)
                {
                    WaitSemaphore.Dispose();
                }
            }
        }
    }
}
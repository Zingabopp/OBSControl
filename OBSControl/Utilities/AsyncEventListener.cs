using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine.Rendering;
#nullable enable

namespace OBSControl.Utilities
{
    /// <summary>
    /// Result of the function given to <see cref="AsyncEventListener{TResult, TEventArgs}"/>.
    /// </summary>
    /// <typeparam name="TResult">Result type returned by <see cref="AsyncEventListener{TResult, TEventArgs}.Task"/>.</typeparam>
#pragma warning disable CA1815 // Override equals and operator equals on value types
    public struct EventListenerResult<TResult>
#pragma warning restore CA1815 // Override equals and operator equals on value types
    {
        /// <summary>
        /// Result from the execution of the function.
        /// </summary>
        public readonly TResult Result;
        /// <summary>
        /// Set to true if the result should be returned in <see cref="AsyncEventListener{TResult, TEventArgs}.Task"/>
        /// </summary>
        public readonly bool IsFinished;

        public EventListenerResult(TResult result, bool isFinished)
        {
            Result = result;
            IsFinished = isFinished;
        }
    }

    /// <summary>
    /// <see cref="AsyncEventListener{TResult, TEventArgs}"/> provides a way to listen to an event in the form of a <see cref="Task{TResult}"/>
    /// </summary>
    /// <typeparam name="TResult">Type of result returned by the <see cref="Task"/>.</typeparam>
    /// <typeparam name="TEventArgs">Type of the event arguments in the <see cref="EventHandler"/></typeparam>
    public class AsyncEventListener<TResult, TEventArgs>
    {
        protected Func<object, TEventArgs, EventListenerResult<TResult>> Function;
        private CancellationToken _cancellationToken;
        private CancellationTokenRegistration _tokenRegistration;

        protected int _timeout;
        private CancellationTokenSource? TimeoutSource;
        private CancellationToken _timeoutToken;
        private CancellationTokenRegistration _timeoutRegistration;

        private object startLock = new object();
        private TaskCompletionSource<TResult> _taskCompletion = null!;

        private bool _listening;
        /// <summary>
        /// Returns true if the <see cref="AsyncEventListener{TResult, TEventArgs}"/> is listening to any events its subscribed to.
        /// </summary>
        public bool Listening
        {
            get => _listening && !_taskCompletion.Task.IsCompleted;
            set => _listening = value;
        }

        /// <summary>
        /// This task will remain incomplete until an event <see cref="OnEvent(object, TEventArgs)"/>
        /// is subscribed to is raised and the function returns a <see cref="EventListenerResult{TResult}"/> 
        /// where <see cref="EventListenerResult{TResult}.IsFinished"/> is true, the <see cref="CancellationToken"/> is cancelled, or the event listener times out.
        /// </summary>
        /// <exception cref="TimeoutException"></exception>
        /// <exception cref="OperationCanceledException"></exception>
        public Task<TResult> Task
        {
            get
            {
                if (_listening == false)
                    StartListening();
                return _taskCompletion.Task;
            }
        }

        /// <summary>
        /// This must be called for the <see cref="AsyncEventListener{TResult, TEventArgs}"/> to start listening to events.
        /// The timeout clock starts when this is called if a timeout was provided in the constructor.
        /// </summary>
        public void StartListening()
        {
            bool wasListening = false;
            lock (startLock)
            {
                wasListening = _listening;
                Listening = true;
            }
            if (wasListening) return; // Don't StartListening twice
            if (_timeout > 0)
            {
                TimeoutSource = new CancellationTokenSource(_timeout);
                _timeoutToken = TimeoutSource.Token;
                _timeoutRegistration = _timeoutToken.Register(Timeout);
            }
        }

        /// <summary>
        /// Resets the <see cref="AsyncEventListener{TResult, TEventArgs}"/> so it can be reused with a new <see cref="CancellationToken"/>.
        /// <see cref="StartListening"/> must be called again, the existing timeout value, if any, is used.
        /// </summary>
        /// <param name="cancellationToken"></param>
        public void Reset(CancellationToken cancellationToken = default)
        {
            _listening = false;
            TaskCompletionSource<TResult>? previous = _taskCompletion;
            if (previous != null)
                previous.TrySetException(new OperationCanceledException("Operation cancelled: AsyncEventListener was reset."));
            Cleanup();
            _taskCompletion = new TaskCompletionSource<TResult>();
            _cancellationToken = cancellationToken;
            if (_cancellationToken.CanBeCanceled)
                _tokenRegistration = _cancellationToken.Register(CancelInternal);
        }

        protected virtual EventListenerResult<TResult> ExecuteFunction(object sender, TEventArgs eventArgs)
        {
            return Function(sender, eventArgs);
        }

        /// <summary>
        /// Subscribe this method to the event it should listen to.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="eventArgs"></param>
        public void OnEvent(object sender, TEventArgs eventArgs)
        {
            if (!Listening) return;
            bool finished = true;
            try
            {
                EventListenerResult<TResult> result = ExecuteFunction(sender, eventArgs);
                if (result.IsFinished)
                    _taskCompletion.TrySetResult(result.Result);
                else
                    finished = false;
            }
#pragma warning disable CA1031 // Do not catch general exception types
            catch (OperationCanceledException)
            {
                _taskCompletion.TrySetCanceled(_cancellationToken);
            }
            catch (Exception ex)
            {
                _taskCompletion.TrySetException(ex);
            }
#pragma warning restore CA1031 // Do not catch general exception types
            finally
            {
                if (finished)
                {
                    _tokenRegistration.Dispose();
                    Listening = false;
                    Cleanup();
                }
            }
        }

        /// <summary>
        /// Creates a new <see cref="AsyncEventListener{TResult, TEventArgs}"/> with an optional timeout (in milliseconds) and <see cref="CancellationToken"/>.
        /// </summary>
        /// <param name="function">Function that will operate on the parameters given by the raised event.</param>
        /// <param name="timeout">Timeout in milliseconds (0 for none). The clock starts when <see cref="StartListening"/> is called.</param>
        /// <param name="cancellationToken"></param>
        public AsyncEventListener(Func<object, TEventArgs, EventListenerResult<TResult>> function, int timeout, CancellationToken cancellationToken = default)
        {
            _timeout = timeout;
            Reset(cancellationToken);
            Function = function;
        }

        /// <summary>
        /// Creates a new <see cref="AsyncEventListener{TResult, TEventArgs}"/> with an optional <see cref="CancellationToken"/>.
        /// </summary>
        /// <param name="function">Function that will operate on the parameters given by the raised event.</param>
        /// <param name="cancellationToken"></param>
        public AsyncEventListener(Func<object, TEventArgs, EventListenerResult<TResult>> function, CancellationToken cancellationToken = default)
            : this(function, 0, cancellationToken)
        { }
        public bool TrySetResult(TResult result)
        {
            bool ret = _taskCompletion.TrySetResult(result);
            Cleanup();
            return ret;
        }
        public bool TrySetCanceled(CancellationToken cancellationToken = default)
        {
            bool ret = _taskCompletion.TrySetCanceled(cancellationToken);
            Cleanup();
            return ret;
        }
        public bool TrySetException(Exception exception)
        {
            bool ret = _taskCompletion.TrySetException(exception);
            Cleanup();
            return ret;
        }

        private void Timeout()
        {
            _taskCompletion.TrySetException(new TimeoutException("EventListener timed out."));
            Cleanup();
        }

        private void CancelInternal()
        {
            _taskCompletion.TrySetCanceled(_cancellationToken);
            Cleanup();
        }

        private void Cleanup()
        {
            Listening = false;
            _tokenRegistration.Dispose();
            _timeoutRegistration.Dispose();
            TimeoutSource?.Dispose();
            TimeoutSource = null;
        }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="TResult">Result returned by the task.</typeparam>
    /// <typeparam name="TEventArgs">Event argument(s) provided with the event.</typeparam>
    /// <typeparam name="TArg"></typeparam>
    public class AsyncEventListenerWithArg<TResult, TEventArgs, TArg> : AsyncEventListener<TResult, TEventArgs>
    {
        public TArg Argument { get; protected set; }

        public void Reset(TArg argument, CancellationToken cancellationToken = default)
        {
            Reset(cancellationToken);
            Argument = argument;
        }

        protected override EventListenerResult<TResult> ExecuteFunction(object sender, TEventArgs eventArgs)
        {

            return FunctionWithArg(sender, eventArgs, Argument);
        }

        Func<object, TEventArgs, TArg, EventListenerResult<TResult>> FunctionWithArg;
        /// <summary>
        /// Creates a new <see cref="AsyncEventListenerWithArg{TResult, TEventArgs, TArg}"/> with an optional timeout (in milliseconds) and <see cref="CancellationToken"/> and will not complete the task until the provided function is satisifed.
        /// </summary>
        /// <param name="function">Function that will operate on the parameters given by the raised event.</param>
        /// <param name="timeout">Timeout in milliseconds (0 for none). The clock starts when <see cref="StartListening"/> is called.</param>
        /// <param name="cancellationToken"></param>
        public AsyncEventListenerWithArg(Func<object, TEventArgs, TArg, EventListenerResult<TResult>> function, TArg arg, int timeout, CancellationToken cancellationToken = default)
            : base((s, e) => function(s, e, arg), timeout, cancellationToken)
        {
            FunctionWithArg = function;
            Argument = arg;
            Reset(cancellationToken);
        }

        /// <summary>
        /// Creates a new <see cref="AsyncEventListener{TResult, TEventArgs}"/> with an optional <see cref="CancellationToken"/>.
        /// </summary>
        /// <param name="function">Function that will operate on the parameters given by the raised event.</param>
        /// <param name="cancellationToken"></param>
        public AsyncEventListenerWithArg(Func<object, TEventArgs, TArg, EventListenerResult<TResult>> function, TArg arg, CancellationToken cancellationToken = default)
            : this(function, arg, 0, cancellationToken)
        { }
    }
}

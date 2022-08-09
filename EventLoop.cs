using OneOf;

namespace SharpEventLoop
{
    public class EventLoop : IEventLoop
    {
        private readonly IUnixTimeProvider _unixTimeProvider;
        private readonly IUiHandler _uiHandler; 

        private readonly PriorityQueue<AsyncEvent, long> _eventQueue = new();
        private readonly LinkedList<AsyncEvent> _runningEvents = new();

        private long _now;
        private LinkedListNode<AsyncEvent>? _currentRunningEventNode;

        public EventLoop(IUnixTimeProvider unixTimeProvider, IUiHandler uiHandler)
        {
            _unixTimeProvider = unixTimeProvider;
            _uiHandler = uiHandler;
        }

        private void HandleEvent()
        {
            if (!_eventQueue.TryPeek(out _, out var timeout)
                || timeout >= _now)
            {
                return;
            }
            
            var asyncEvent = _eventQueue.Dequeue();
            if (!StartRunningAsyncEvent(asyncEvent))
            {
                return;
            }

            _runningEvents.AddLast(asyncEvent);
            _currentRunningEventNode ??= _runningEvents.First;
        }

        private bool StartRunningAsyncEvent(AsyncEvent asyncEvent)
        {
            if (asyncEvent.TaskCancellationToken is {IsCancellationRequested: true})
            {
                HandleCanceledEventException(asyncEvent);
                return false;
            }

            try
            {
                asyncEvent.CurrentTask = asyncEvent.EventHandler!.Value.Match(
                    funcNoParams => funcNoParams(),
                    funcWithParam => funcWithParam(asyncEvent.PrevTaskResult));
                return true;
            }
            catch (Exception exception)
            {
                EnqueueExceptionAsyncEvent(exception, asyncEvent.ExceptionHandler);
                return false;
            }
        }

        private void HandleRunningEvent()
        {
            if (_currentRunningEventNode == null)
            {
                return;
            }

            var currentAsyncEvent = _currentRunningEventNode.Value;
            var nextNode = _currentRunningEventNode.Next;
            if (currentAsyncEvent.CurrentTask!.IsCompleted)
            {
                _runningEvents.Remove(_currentRunningEventNode);
                HandleCanceledEventException(currentAsyncEvent);
                HandleAsyncEventException(currentAsyncEvent);
                EnqueueNextAsyncEvent(currentAsyncEvent);
            }

            _currentRunningEventNode = nextNode ?? _runningEvents.First;
        }

        private void HandleAsyncEventException(AsyncEvent currentAsyncEvent)
        {
            if (currentAsyncEvent.CurrentTask!.IsCanceled
                || !currentAsyncEvent.CurrentTask.IsFaulted)
            {
                return;
            }

            EnqueueExceptionAsyncEvent(
                currentAsyncEvent.CurrentTask.Exception!,
                currentAsyncEvent.ExceptionHandler);
        }

        private void HandleCanceledEventException(AsyncEvent currentAsyncEvent)
        {
            if (currentAsyncEvent.TaskCancellationToken?.IsCancellationRequested == false
                || currentAsyncEvent.CurrentTask?.IsCanceled == false)
            {
                return;
            }

            Exception exception = currentAsyncEvent.CurrentTask?.IsCanceled == true
                ? new TaskCanceledException(currentAsyncEvent.CurrentTask)
                : new OperationCanceledException(currentAsyncEvent.TaskCancellationToken!.Value);
            EnqueueExceptionAsyncEvent(exception, currentAsyncEvent.ExceptionHandler);
        }

        private void EnqueueExceptionAsyncEvent(Exception exception, Func<Exception, Task>? exceptionHandler)
        {
            if (exceptionHandler == null)
            {
                throw exception;
            }

            Task WrappedExceptionHandler(object? param)
            {
                return exceptionHandler((Exception)param!);
            }

            var exceptionAsyncEvent = new AsyncEvent((Func<object?, Task>)WrappedExceptionHandler)
            {
                PrevTaskResult = exception
            };
            EnqueueAsyncEventInternal(exceptionAsyncEvent);
        }

        private void EnqueueNextAsyncEvent(AsyncEvent currentAsyncEvent)
        {
            if (currentAsyncEvent.Next == null || !currentAsyncEvent.CurrentTask!.IsCompletedSuccessfully)
            {
                return;
            }

            var taskType = currentAsyncEvent.CurrentTask.GetType();
            if (taskType.IsGenericType)
            {
                currentAsyncEvent.Next.PrevTaskResult = taskType.GetProperty(nameof(Task<object>.Result))!
                    .GetValue(currentAsyncEvent.CurrentTask);
            }

            EnqueueAsyncEventInternal(currentAsyncEvent.Next);
        }

        public void Run(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                _now = _unixTimeProvider.GetUnixTime();
                _uiHandler.HandleUi();
                HandleEvent();
                HandleRunningEvent();
            }
        }

        public IAsyncEvent EnqueueEvent(Action eventHandler, CancellationToken? cancellationToken = null)
        {
            Task WrappedEventHandler()
            {
                eventHandler();
                return Task.CompletedTask;
            }

            return EnqueueEvent(WrappedEventHandler, cancellationToken);
        }

        public IAsyncEvent EnqueueEvent(Func<Task> eventHandler, CancellationToken? cancellationToken = null)
        {
            return EnqueueEventInternal(eventHandler, 0, cancellationToken);
        }

        public IAsyncEvent EnqueueEvent<TResult>(Func<TResult?> eventHandler, CancellationToken? cancellationToken = null)
        {
            Task WrappedEventHandler()
            {
                return Task.FromResult(eventHandler());
            }

            return EnqueueEvent(WrappedEventHandler, cancellationToken);
        }

        public IAsyncEvent EnqueueEvent<TResult>(Func<Task<TResult?>> eventHandler, CancellationToken? cancellationToken = null)
        {
            return EnqueueEvent((Func<Task>)eventHandler, cancellationToken);
        }

        public IAsyncEvent SetTimeout(Action timeoutEventHandler, long timeout, CancellationToken? cancellationToken = null)
        {
            Task WrappedTimeoutEventHandler()
            {
                timeoutEventHandler();
                return Task.CompletedTask;
            }
            
            return SetTimeout(WrappedTimeoutEventHandler, timeout, cancellationToken);
        }

        public IAsyncEvent SetTimeout(Func<Task> timeoutEventHandler, long timeout, CancellationToken? cancellationToken = null)
        {
            return EnqueueEventInternal(timeoutEventHandler, timeout, cancellationToken);
        }

        public IAsyncEvent SetTimeout<TResult>(Func<TResult?> timeoutEventHandler, long timeout, CancellationToken? cancellationToken = null)
        {
            Task WrappedTimeoutEventHandler()
            {
                return Task.FromResult(timeoutEventHandler());
            }

            return SetTimeout(WrappedTimeoutEventHandler, timeout, cancellationToken);
        }

        public IAsyncEvent SetTimeout<TResult>(Func<Task<TResult?>> timeoutEventHandler, long timeout, CancellationToken? cancellationToken = null)
        {
            return SetTimeout((Func<Task>)timeoutEventHandler, timeout, cancellationToken);
        }

        public IAsyncEvent SetInterval(Action intervalHandler, long interval, CancellationToken? cancellationToken = null)
        {
            var asyncEvent = new AsyncEvent
            {
                TaskCancellationToken = cancellationToken,
                Delay = interval
            };
            Task WrappedIntervalEventHandler()
            {
                EnqueueNextIntervalEvent(asyncEvent);
                intervalHandler();
                return Task.CompletedTask;
            }

            return SetIntervalInternal(asyncEvent, WrappedIntervalEventHandler);
        }

        public IAsyncEvent SetInterval(Func<Task> intervalHandler, long interval, CancellationToken? cancellationToken = null)
        {
            var asyncEvent = new AsyncEvent
            {
                TaskCancellationToken = cancellationToken,
                Delay = interval
            };
            Task WrappedIntervalEventHandler()
            {
                EnqueueNextIntervalEvent(asyncEvent);
                return intervalHandler();
            }

            return SetIntervalInternal(asyncEvent, WrappedIntervalEventHandler);
        }

        public IAsyncEvent SetInterval<TResult>(Func<TResult?> intervalHandler, long interval, CancellationToken? cancellationToken = null)
        {
            var asyncEvent = new AsyncEvent
            {
                TaskCancellationToken = cancellationToken,
                Delay = interval
            };
            Task WrappedIntervalEventHandler()
            {
                EnqueueNextIntervalEvent(asyncEvent);
                return Task.FromResult(intervalHandler());
            }

            return SetIntervalInternal(asyncEvent, WrappedIntervalEventHandler);
        }

        public IAsyncEvent SetInterval<TResult>(Func<Task<TResult?>> intervalHandler, long interval, CancellationToken? cancellationToken = null)
        {
            return SetInterval((Func<Task>)intervalHandler, interval);
        }

        private IAsyncEvent SetIntervalInternal(AsyncEvent asyncEvent, Func<Task> wrappedIntervalEventHandler)
        {
            asyncEvent.EventHandler = wrappedIntervalEventHandler;
            EnqueueAsyncEventInternal(asyncEvent);
            return asyncEvent;
        }

        private void EnqueueNextIntervalEvent(AsyncEvent asyncEvent)
        {
            var nextAsyncEvent = asyncEvent.DeepCopy();
            EnqueueAsyncEventInternal(nextAsyncEvent);
        }

        private AsyncEvent EnqueueEventInternal(OneOf<Func<Task>, Func<object?, Task>> eventHandler, long timeout, CancellationToken? cancellationToken)
        {
            var asyncEvent = new AsyncEvent(eventHandler)
            {
                TaskCancellationToken = cancellationToken,
                Delay = timeout
            };
            EnqueueAsyncEventInternal(asyncEvent);
            return asyncEvent;
        }

        private void EnqueueAsyncEventInternal(AsyncEvent asyncEvent)
        {
            _eventQueue.Enqueue(asyncEvent, _now + asyncEvent.Delay);
        }
    }
}

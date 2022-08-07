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
            StartRunningAsyncEvent(asyncEvent);

            _runningEvents.AddLast(asyncEvent);
            _currentRunningEventNode ??= _runningEvents.First;
        }

        private static void StartRunningAsyncEvent(AsyncEvent asyncEvent)
        {
            asyncEvent.CurrentTask = asyncEvent.EventHandler!.Value.Match(
                funcNoParams => funcNoParams(),
                funcWithParam => funcWithParam(asyncEvent.PrevTaskResult));
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
                HandleAsyncEventException(currentAsyncEvent);
                EnqueueNextAsyncEvent(currentAsyncEvent);
            }

            _currentRunningEventNode = nextNode ?? _runningEvents.First;
        }

        private void HandleAsyncEventException(AsyncEvent currentAsyncEvent)
        {
            if (!currentAsyncEvent.CurrentTask!.IsFaulted || currentAsyncEvent.ExceptionHandler == null)
            {
                return;
            }

            var exceptionAsyncEvent = new AsyncEvent((Func<object?, Task>) currentAsyncEvent.ExceptionHandler)
            {
                PrevTaskResult = currentAsyncEvent.CurrentTask!.Exception
            };
            EnqueueAsyncEventInternal(exceptionAsyncEvent);
        }

        private void EnqueueNextAsyncEvent(AsyncEvent currentAsyncEvent)
        {
            if (currentAsyncEvent.Next == null || !currentAsyncEvent.CurrentTask!.IsCompletedSuccessfully)
            {
                return;
            }

            if (currentAsyncEvent.CurrentTask is Task<object?> taskWithResult)
            {
                currentAsyncEvent.Next.PrevTaskResult = taskWithResult.Result;
            }

            EnqueueAsyncEventInternal(currentAsyncEvent.Next, currentAsyncEvent.Delay);
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

        public IAsyncEvent EnqueueEvent(Action eventHandler)
        {
            Task WrappedEventHandler()
            {
                eventHandler();
                return Task.CompletedTask;
            }

            return EnqueueEvent(WrappedEventHandler);
        }

        public IAsyncEvent EnqueueEvent(Func<Task> eventHandler)
        {
            return EnqueueEventInternal(eventHandler);
        }
        public IAsyncEvent EnqueueEvent<TResult>(Func<Task<TResult>> eventHandler)
        {
            return EnqueueEvent((Func<Task>)eventHandler);
        }

        public IAsyncEvent SetTimeout(Action timeoutEventHandler, long timeout)
        {
            Task WrappedTimeoutEventHandler()
            {
                timeoutEventHandler();
                return Task.CompletedTask;
            }
            
            return SetTimeout(WrappedTimeoutEventHandler, timeout);
        }

        public IAsyncEvent SetTimeout(Func<Task> timeoutEventHandler, long timeout)
        {
            return EnqueueEventInternal(timeoutEventHandler, timeout);
        }

        public IAsyncEvent SetTimeout<TResult>(Func<Task<TResult>> timeoutEventHandler, long timeout)
        {
            return SetTimeout((Func<Task>)timeoutEventHandler, timeout);
        }

        public IAsyncEvent SetInterval(Action intervalHandler, long interval)
        {
            var asyncEvent = new AsyncEvent();
            Task WrappedIntervalEventHandler()
            {
                EnqueueNextIntervalEvent(asyncEvent, interval);
                intervalHandler();
                return Task.CompletedTask;
            }

            return SetIntervalInternal(asyncEvent, WrappedIntervalEventHandler, interval);
        }

        public IAsyncEvent SetInterval(Func<Task> intervalHandler, long interval)
        {
            var asyncEvent = new AsyncEvent();
            Task WrappedIntervalEventHandler()
            {
                EnqueueNextIntervalEvent(asyncEvent, interval);
                return intervalHandler();
            }

            return SetIntervalInternal(asyncEvent, WrappedIntervalEventHandler, interval);
        }

        public IAsyncEvent SetInterval<TResult>(Func<Task<TResult>> intervalHandler, long interval)
        {
            return SetInterval((Func<Task>)intervalHandler, interval);
        }

        private IAsyncEvent SetIntervalInternal(AsyncEvent asyncEvent, Func<Task> wrappedIntervalEventHandler, long interval)
        {
            asyncEvent.EventHandler = wrappedIntervalEventHandler;
            EnqueueAsyncEventInternal(asyncEvent, interval);
            return asyncEvent;
        }

        private void EnqueueNextIntervalEvent(AsyncEvent asyncEvent, long interval)
        {
            var nextAsyncEvent = asyncEvent.DeepCopy();
            EnqueueAsyncEventInternal(nextAsyncEvent, interval);
        }

        private AsyncEvent EnqueueEventInternal(OneOf<Func<Task>, Func<object?, Task>> eventHandler, long timeout = 0)
        {
            var asyncEvent = new AsyncEvent(eventHandler);
            EnqueueAsyncEventInternal(asyncEvent, timeout);
            return asyncEvent;
        }

        private void EnqueueAsyncEventInternal(AsyncEvent asyncEvent, long timeout = 0)
        {
            _eventQueue.Enqueue(asyncEvent, _now + timeout);
        }
    }
}

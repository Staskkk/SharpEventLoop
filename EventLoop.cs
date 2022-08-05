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
            asyncEvent.CurrentTask = asyncEvent.EventHandler!();

            _runningEvents.AddLast(asyncEvent);
            _currentRunningEventNode ??= _runningEvents.First;
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
                if (currentAsyncEvent.Next != null)
                {
                    EnqueueAsyncEventInternal(currentAsyncEvent.Next, currentAsyncEvent.Delay);
                }
            }

            _currentRunningEventNode = nextNode ?? _runningEvents.First;
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

        public IAsyncEvent SetTimeout(Func<Task> timeoutEventHandler, long timeout)
        {
            return EnqueueEventInternal(timeoutEventHandler, timeout);
        }

        public IAsyncEvent SetInterval(Func<Task> intervalHandler, long interval)
        {
            var asyncEvent = new AsyncEvent();
            Task TimeoutEventHandler()
            {
                var nextAsyncEvent = asyncEvent.DeepCopy();
                EnqueueAsyncEventInternal(nextAsyncEvent, interval);
                return intervalHandler();
            }

            asyncEvent.EventHandler = TimeoutEventHandler;
            EnqueueAsyncEventInternal(asyncEvent, interval);
            return asyncEvent;
        }

        public IAsyncEvent EnqueueEvent(Func<Task> eventHandler)
        {
            return EnqueueEventInternal(eventHandler);
        }

        private AsyncEvent EnqueueEventInternal(Func<Task> eventHandler, long timeout = 0)
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

namespace SharpEventLoop
{
    public class EventLoop : IEventLoop
    {
        private readonly IUnixTimeProvider _unixTimeProvider;
        private readonly IUiHandler _uiHandler; 

        private readonly PriorityQueue<AsyncEvent, long> _eventQueue = new();

        private long _now;

        public EventLoop(IUnixTimeProvider unixTimeProvider, IUiHandler uiHandler)
        {
            _unixTimeProvider = unixTimeProvider;
            _uiHandler = uiHandler;
        }

        private void HandleEvent()
        {
            if (_eventQueue.TryPeek(out _, out var timeout)
                && timeout < _now)
            {
                var asyncEvent = _eventQueue.Dequeue();
                asyncEvent.EventHandler();
                if (asyncEvent.Next != null)
                {
                    EnqueueAsyncEventInternal(asyncEvent.Next);
                }
            }
        }

        public void Run(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                _now = _unixTimeProvider.GetUnixTime();
                _uiHandler.HandleUi();
                HandleEvent();
            }
        }

        public IAsyncEvent SetTimeout(Action timeoutEventHandler, long timeout)
        {
            return EnqueueEventInternal(timeoutEventHandler, _now + timeout);
        }

        public IAsyncEvent SetInterval(Action intervalHandler, long interval)
        {
            void TimeoutEventHandler(AsyncEvent asyncEvent)
            {
                asyncEvent.EventHandler();
                if (asyncEvent.Next != null)
                {
                    EnqueueAsyncEventInternal(asyncEvent.Next);
                }

                EnqueueEventInternal(() => TimeoutEventHandler(asyncEvent), _now + interval);
            }

            var asyncEvent = new AsyncEvent(intervalHandler);
            EnqueueEventInternal(() => TimeoutEventHandler(asyncEvent), _now + interval);
            return asyncEvent;
        }

        public IAsyncEvent EnqueueEvent(Action eventHandler)
        {
            return EnqueueEventInternal(eventHandler, _now);
        }

        private AsyncEvent EnqueueEventInternal(Action eventHandler, long unixTime)
        {
            var asyncEvent = new AsyncEvent(eventHandler);
            EnqueueAsyncEventInternal(asyncEvent, unixTime);
            return asyncEvent;
        }

        private void EnqueueAsyncEventInternal(AsyncEvent asyncEvent)
        {
            EnqueueAsyncEventInternal(asyncEvent, _now);
        }

        private void EnqueueAsyncEventInternal(AsyncEvent asyncEvent, long unixTime)
        {
            _eventQueue.Enqueue(asyncEvent, unixTime);
        }
    }
}

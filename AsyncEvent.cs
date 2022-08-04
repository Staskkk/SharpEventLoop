namespace SharpEventLoop
{
    internal class AsyncEvent : IAsyncEvent
    {
        internal Action EventHandler { get; }

        internal AsyncEvent? Next { get; private set; }

        internal AsyncEvent(Action eventHandler)
        {
            EventHandler = eventHandler;
        }

        public IAsyncEvent Then(Action eventHandler)
        {
            Next = new AsyncEvent(eventHandler);
            return Next;
        }
    }
}

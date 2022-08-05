namespace SharpEventLoop
{
    internal class AsyncEvent : IAsyncEvent
    {
        internal Func<Task>? EventHandler { get; set; }

        internal AsyncEvent? Next { get; private set; }

        internal Task? CurrentTask { get; set; }

        internal long Delay { get; set; }

        internal AsyncEvent()
        {
        }

        internal AsyncEvent(Func<Task> eventHandler)
        {
            EventHandler = eventHandler;
        }

        public IAsyncEvent Then(Func<Task> eventHandler)
        {
            return Then(eventHandler, 0);
        }

        public IAsyncEvent Then(Func<Task> eventHandler, long delay)
        {
            Delay = delay;
            Next = new AsyncEvent(eventHandler);
            return Next;
        }

        internal AsyncEvent DeepCopy()
        {
            var asyncEventCopy = new AsyncEvent(EventHandler!)
            {
                Delay = Delay
            };
            if (Next != null)
            {
                asyncEventCopy.Next = Next.DeepCopy();
            }

            return asyncEventCopy;
        }
    }
}

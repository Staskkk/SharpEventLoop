namespace SharpEventLoop
{
    public interface IEventLoop
    {
        void Run(CancellationToken cancellationToken);

        IAsyncEvent SetTimeout(Func<Task> timeoutEventHandler, long timeout);

        IAsyncEvent SetInterval(Func<Task> intervalHandler, long interval);

        IAsyncEvent EnqueueEvent(Func<Task> eventHandler);
    }
}

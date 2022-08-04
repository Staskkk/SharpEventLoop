namespace SharpEventLoop
{
    public interface IEventLoop
    {
        void Run(CancellationToken cancellationToken);

        IAsyncEvent SetTimeout(Action timeoutEventHandler, long timeout);

        IAsyncEvent SetInterval(Action intervalHandler, long interval);

        IAsyncEvent EnqueueEvent(Action eventHandler);
    }
}

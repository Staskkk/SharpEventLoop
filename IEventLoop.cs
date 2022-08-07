namespace SharpEventLoop
{
    public interface IEventLoop
    {
        void Run(CancellationToken cancellationToken);

        IAsyncEvent EnqueueEvent(Action eventHandler);

        IAsyncEvent EnqueueEvent(Func<Task> eventHandler);

        IAsyncEvent EnqueueEvent<TResult>(Func<Task<TResult>> eventHandler);

        IAsyncEvent SetTimeout(Action timeoutEventHandler, long timeout);

        IAsyncEvent SetTimeout(Func<Task> timeoutEventHandler, long timeout);

        IAsyncEvent SetTimeout<TResult>(Func<Task<TResult>> timeoutEventHandler, long timeout);

        IAsyncEvent SetInterval(Action intervalHandler, long interval);

        IAsyncEvent SetInterval(Func<Task> intervalHandler, long interval);

        IAsyncEvent SetInterval<TResult>(Func<Task<TResult>> intervalHandler, long interval);
    }
}

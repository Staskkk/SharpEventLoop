namespace SharpEventLoop
{
    public interface IEventLoop
    {
        void Run(CancellationToken cancellationToken);

        IAsyncEvent EnqueueEvent(Action eventHandler, CancellationToken? cancellationToken = null);

        IAsyncEvent EnqueueEvent(Func<Task> eventHandler, CancellationToken? cancellationToken = null);

        IAsyncEvent EnqueueEvent<TResult>(Func<TResult?> eventHandler, CancellationToken? cancellationToken = null);

        IAsyncEvent EnqueueEvent<TResult>(Func<Task<TResult?>> eventHandler, CancellationToken? cancellationToken = null);

        IAsyncEvent SetTimeout(Action timeoutEventHandler, long timeout, CancellationToken? cancellationToken = null);

        IAsyncEvent SetTimeout(Func<Task> timeoutEventHandler, long timeout, CancellationToken? cancellationToken = null);

        IAsyncEvent SetTimeout<TResult>(Func<TResult?> timeoutEventHandler, long timeout, CancellationToken? cancellationToken = null);

        IAsyncEvent SetTimeout<TResult>(Func<Task<TResult?>> timeoutEventHandler, long timeout, CancellationToken? cancellationToken = null);

        IAsyncEvent SetInterval(Action intervalHandler, long interval, CancellationToken? cancellationToken = null);

        IAsyncEvent SetInterval(Func<Task> intervalHandler, long interval, CancellationToken? cancellationToken = null);

        IAsyncEvent SetInterval<TResult>(Func<TResult?> intervalHandler, long interval, CancellationToken? cancellationToken = null);

        IAsyncEvent SetInterval<TResult>(Func<Task<TResult?>> intervalHandler, long interval, CancellationToken? cancellationToken = null);
    }
}

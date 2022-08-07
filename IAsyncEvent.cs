namespace SharpEventLoop
{
    public interface IAsyncEvent
    {
        IAsyncEvent Then(Action eventHandler, long delay = 0);

        IAsyncEvent Then(Func<Task> eventHandler, long delay = 0);

        IAsyncEvent Then<TResult>(Func<Task<TResult?>> eventHandler, long delay = 0);

        IAsyncEvent Then<TParam>(Action<TParam?> eventHandler, long delay = 0);

        IAsyncEvent Then<TParam>(Func<TParam?, Task> eventHandler, long delay = 0);

        IAsyncEvent Then<TParam, TResult>(Func<TParam?, Task<TResult?>> eventHandler, long delay = 0);

        IAsyncEvent Catch(Action<Exception> exceptionHandler);

        IAsyncEvent Catch(Func<Exception, Task> exceptionHandler);

        IAsyncEvent Catch<TResult>(Func<Exception, Task<TResult?>> exceptionHandler);
    }
}

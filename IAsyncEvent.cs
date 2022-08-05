namespace SharpEventLoop
{
    public interface IAsyncEvent
    {
        IAsyncEvent Then(Func<Task> eventHandler);

        IAsyncEvent Then(Func<Task> eventHandler, long delay);
    }
}

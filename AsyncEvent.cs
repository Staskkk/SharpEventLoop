using OneOf;

namespace SharpEventLoop
{
    internal class AsyncEvent : IAsyncEvent
    {
        internal OneOf<Func<Task>, Func<object?, Task>>? EventHandler { get; set; }

        internal Func<Exception, Task>? ExceptionHandler { get; set; }

        internal AsyncEvent? Next { get; private set; }

        internal Task? CurrentTask { get; set; }

        internal object? PrevTaskResult { get; set; }

        internal CancellationToken? TaskCancellationToken { get; init; }

        internal long Delay { get; init; }

        internal AsyncEvent()
        {
        }

        internal AsyncEvent(OneOf<Func<Task>, Func<object?, Task>> eventHandler)
        {
            EventHandler = eventHandler;
        }

        public IAsyncEvent Then(Action eventHandler, long delay = 0, CancellationToken? cancellationToken = null)
        {
            Task WrappedEventHandler()
            {
                eventHandler();
                return Task.CompletedTask;
            }

            return ThenInternal((Func<Task>)WrappedEventHandler, delay, cancellationToken);
        }

        public IAsyncEvent Then(Func<Task> eventHandler, long delay = 0, CancellationToken? cancellationToken = null)
        {
            return ThenInternal(eventHandler, delay, cancellationToken);
        }

        public IAsyncEvent Then<TResult>(Func<TResult?> eventHandler, long delay = 0, CancellationToken? cancellationToken = null)
        {
            Task WrappedEventHandler()
            {
                return Task.FromResult(eventHandler());
            }

            return ThenInternal((Func<Task>)WrappedEventHandler, delay, cancellationToken);
        }

        public IAsyncEvent Then<TResult>(Func<Task<TResult?>> eventHandler, long delay = 0, CancellationToken? cancellationToken = null)
        {
            return ThenInternal(eventHandler, delay, cancellationToken);
        }

        public IAsyncEvent Then<TParam>(Action<TParam?> eventHandler, long delay = 0, CancellationToken? cancellationToken = null)
        {
            Task WrappedEventHandler(object? param)
            {
                eventHandler((TParam?)param);
                return Task.CompletedTask;
            }

            return ThenInternal((Func<object?, Task>)WrappedEventHandler, delay, cancellationToken);
        }

        public IAsyncEvent Then<TParam>(Func<TParam?, Task> eventHandler, long delay = 0, CancellationToken? cancellationToken = null)
        {
            Task WrappedEventHandler(object? param)
            {
                return eventHandler((TParam?) param);
            }

            return ThenInternal((Func<object?, Task>)WrappedEventHandler, delay, cancellationToken);
        }

        public IAsyncEvent Then<TParam, TResult>(Func<TParam?, TResult?> eventHandler, long delay = 0, CancellationToken? cancellationToken = null)
        {
            Task WrappedEventHandler(object? param)
            {
                return Task.FromResult(eventHandler((TParam?)param));
            }

            return ThenInternal((Func<object?, Task>)WrappedEventHandler, delay, cancellationToken);
        }

        public IAsyncEvent Then<TParam, TResult>(Func<TParam?, Task<TResult?>> eventHandler, long delay = 0, CancellationToken? cancellationToken = null)
        {
            Task WrappedEventHandler(object? param)
            {
                return eventHandler((TParam?) param);
            }

            return ThenInternal((Func<object?, Task>)WrappedEventHandler, delay, cancellationToken);
        }

        private IAsyncEvent ThenInternal(OneOf<Func<Task>, Func<object?, Task>> eventHandler, long delay, CancellationToken? cancellationToken)
        {
            Next = new AsyncEvent(eventHandler)
            {
                Delay = delay,
                TaskCancellationToken = cancellationToken
            };

            return Next;
        }

        public IAsyncEvent Catch(Action<Exception> exceptionHandler)
        {
            Task WrappedExceptionHandler(Exception exception)
            {
                exceptionHandler(exception);
                return Task.CompletedTask;
            }

            return CatchInternal(WrappedExceptionHandler);
        }

        public IAsyncEvent Catch(Func<Exception, Task> exceptionHandler)
        {
            return CatchInternal(exceptionHandler);
        }

        private IAsyncEvent CatchInternal(Func<Exception, Task> exceptionHandler)
        {
            ExceptionHandler = exceptionHandler;
            return this;
        }

        internal AsyncEvent DeepCopy()
        {
            var asyncEventCopy = new AsyncEvent
            {
                EventHandler = EventHandler,
                Delay = Delay,
                PrevTaskResult = PrevTaskResult,
                TaskCancellationToken = TaskCancellationToken,
                ExceptionHandler = ExceptionHandler
            };
            if (Next != null)
            {
                asyncEventCopy.Next = Next.DeepCopy();
            }

            return asyncEventCopy;
        }
    }
}

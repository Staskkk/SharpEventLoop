namespace SharpEventLoop;

public static class Program
{
    private static IEventLoop? _eventLoop;
    private static readonly CancellationTokenSource CancellationTokenSource = new();

    private static void Main()
    {
        try
        {
            var unixTimeProvider = new UnixTimeProvider();
            using var uiHandler = new UiHandler();
            _eventLoop = new EventLoop(unixTimeProvider, uiHandler);
            Console.CancelKeyPress += CancelKeyPressHandler;
            _eventLoop.EnqueueEvent(Initialize);
            _eventLoop.Run(CancellationTokenSource.Token);
        }
        finally
        {
            CancellationTokenSource.Dispose();
        }
    }

    private static void CancelKeyPressHandler(object? sender, ConsoleCancelEventArgs e)
    {
        e.Cancel = true;
        _eventLoop!.EnqueueEvent(() =>
        {
            CancellationTokenSource.Cancel();
            return Task.CompletedTask;
        });
    }

    private static Task Initialize()
    {
        _eventLoop!.SetTimeout(() => ExampleTimeout(34), 500)
            .Then(() =>
            {
                Console.WriteLine("Working");
                return Task.CompletedTask;
            });

        _eventLoop.SetInterval(ExampleInterval, 1000)
            .Then(() =>
            {
                Console.WriteLine("Continue test :)");
                return Task.CompletedTask;
            }, 500)
            .Then(() =>
            {
                Console.WriteLine("One more test");
                return Task.CompletedTask;
            }, 400);

        _eventLoop.EnqueueEvent(StartSendingRequest)
            .Then(() =>
            {
                Console.WriteLine("AFTER FINISHING REQUEST");
                return Task.CompletedTask;
            }, 400);

        _eventLoop.EnqueueEvent(() => Task.FromResult(35))
            .Then((int number) =>
            {
                Console.WriteLine($"AFTER FINISHING RESULT: {number}");
                return Task.CompletedTask;
            }, 400);

        return Task.CompletedTask;
    }

    private static Task ExampleTimeout(int number)
    {
        Console.WriteLine($"My timeout #{number}");
        return Task.CompletedTask;
    }

    private static Task ExampleInterval()
    {
        Console.WriteLine($"My interval");
        return Task.CompletedTask;
    }

    private static TaskCompletionSource? _taskCompletionSource;
    private static int _progress;

    private static Task StartSendingRequest()
    {
        Console.WriteLine("Request is start sending");
        _taskCompletionSource = new TaskCompletionSource();
        _progress = 0;
        var cancellationTokenSource = new CancellationTokenSource();
        _eventLoop!.SetTimeout(CheckRequestStatus, 1000, cancellationTokenSource.Token)
            .Catch(ex =>
            {
                Console.WriteLine(ex.Message);
            });
        _eventLoop.SetTimeout(() => cancellationTokenSource.Cancel(), 500);
        return _taskCompletionSource.Task;
    }

    private static Task CheckRequestStatus()
    {
        _progress += 20;
        Console.WriteLine($"Current progress: {_progress}");
        if (_progress < 100)
        {
            _eventLoop!.SetTimeout(CheckRequestStatus, 1000);
        }
        else
        {
            _eventLoop!.EnqueueEvent(FinishingRequest);
        }

        return _taskCompletionSource!.Task;
    }

    private static Task FinishingRequest()
    {
        Console.WriteLine("Request finished");
        _taskCompletionSource!.SetResult();
        return _taskCompletionSource.Task;
    }
}
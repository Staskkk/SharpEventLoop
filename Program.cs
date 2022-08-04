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
        });
    }

    private static void Initialize()
    {
        _eventLoop!.SetTimeout(() => ExampleTimeout(34), 500)
            .Then(() =>
            {
                Console.WriteLine("Working");
            });

        _eventLoop!.SetInterval(ExampleInterval, 1000)
            .Then(() =>
            {
                Console.WriteLine("Continue test :)");
            })
            .Then(() =>
            {
                Console.WriteLine("One more test");
            });
    }

    private static void ExampleTimeout(int number)
    {
        Console.WriteLine($"My timeout #{number}");
    }

    private static void ExampleInterval()
    {
        Console.WriteLine($"My interval");
    }

    private static void StartSendingRequest()
    {
        Console.WriteLine("Request is start sending");
        _eventLoop!.SetTimeout(CheckRequestStatus, 1000);
    }

    private static int progress = 0;

    private static void CheckRequestStatus()
    {
        progress += 20;
        Console.WriteLine($"Current progress: {progress}");
        if (progress < 100)
        {
            _eventLoop!.SetTimeout(CheckRequestStatus, 1000);
        }
        else
        {
            _eventLoop!.EnqueueEvent(FinishingRequest);
        }
    }

    private static void FinishingRequest()
    {
        Console.WriteLine("Request finished");
    }
}
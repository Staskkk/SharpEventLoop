public class Program
{
    private static readonly PriorityQueue<Action, long> _timeoutsQueue = new();
    private static readonly Queue<Action> _eventQueue = new();

    private static long now;

    private static void Main()
    {
        EnqueueEvent(Initialize);
        do
        {
            now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            HandleEventLoop();
            HandleTimeoutsIntervals();
        }
        while (true);
    }

    private static void HandleEventLoop()
    {
        while (_eventQueue.TryDequeue(out var eventHandler))
        {
            eventHandler();
        }
    }

    private static void HandleTimeoutsIntervals() 
    {
        if (_timeoutsQueue.TryPeek(out _, out var timeout)
            && timeout <= now)
        {
            var timeoutEventHandler = _timeoutsQueue.Dequeue();
            EnqueueEvent(timeoutEventHandler);
        }
    }

    private static Task SetTimeout(Action timeoutEventHandler, long timeout)
    {
        _timeoutsQueue.Enqueue(timeoutEventHandler, now + timeout);
        return Task.CompletedTask;
    }

    private static void SetInterval(Action intervalHandler, long interval)
    {
        void TimeoutEventHandler()
        {
            intervalHandler();
            SetTimeout(TimeoutEventHandler, interval);
        }

        SetTimeout(TimeoutEventHandler, interval);
    }

    private static void Initialize()
    {
        SetInterval(HandleUI, 1000);
        var rand = new Random();
        foreach (var i in Enumerable.Range(1, 10000))
        {
            SetTimeout(ExampleTimeout, rand.Next(3000, 10000));
        }
    }

    private static void ExampleTimeout()
    {
        Console.WriteLine("My timeout");
    }

    private static long counter = 0;

    private static void HandleUI()
    {
        counter++;
        Console.WriteLine($"{counter}!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!");
    }

    private static Task EnqueueEvent(Action eventHandler)
    {
        _eventQueue.Enqueue(eventHandler);
        return Task.CompletedTask;
    }
}

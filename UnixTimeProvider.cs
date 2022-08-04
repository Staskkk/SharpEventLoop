namespace SharpEventLoop
{
    public sealed class UnixTimeProvider : IUnixTimeProvider
    {
        public long GetUnixTime()
        {
            return DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        }
    }
}

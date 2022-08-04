namespace SharpEventLoop
{
    public sealed class UiHandler : IUiHandler, IDisposable
    {
        private readonly Stream _consoleInputStream;
        private readonly StreamReader _consoleInputStreamReader;

        private Task<string>? _readTask;

        public UiHandler()
        {
            _consoleInputStream = Console.OpenStandardInput();
            _consoleInputStreamReader = new StreamReader(_consoleInputStream);
        }

        public void HandleUi()
        {
            _readTask ??= _consoleInputStreamReader.ReadLineAsync()!;

            if (_readTask.IsCompleted)
            {
                Console.WriteLine($"FORMATTED: {_readTask.Result}");
                _readTask = null;
            }
        }

        public void Dispose()
        {
            _consoleInputStreamReader.Dispose();
            _consoleInputStream.Dispose();
        }
    }
}

using Microsoft.Extensions.Logging;

namespace Geowerkstatt.Interlis.Compiler.Test;

public sealed class TestLoggerProvider : ILoggerProvider
{
    private readonly TestLogger logger = new();

    public ILogger CreateLogger(string categoryName) => logger;

    public void Dispose()
    {
    }

    public List<string> GetMessages() => logger.Messages;
}

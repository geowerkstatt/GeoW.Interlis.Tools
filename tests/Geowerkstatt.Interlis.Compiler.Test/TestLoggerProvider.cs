using Microsoft.Extensions.Logging;

namespace Geowerkstatt.Interlis.Compiler.Test;

public sealed class TestLoggerProvider : ILoggerProvider
{
    private readonly TestLogger logger = new();

    public ILogger CreateLogger(string categoryName) => logger;

    public void Dispose()
    {
    }

    /// <summary>All logged messages, in order.</summary>
    public List<string> GetMessages() => logger.Entries.Select(e => e.Message).ToList();

    /// <summary>The logged messages whose level matches <paramref name="predicate"/>, in order.</summary>
    public List<string> GetMessages(Func<LogLevel, bool> predicate) => logger.Entries.Where(e => predicate(e.Level)).Select(e => e.Message).ToList();
}

using Microsoft.Extensions.Logging;

namespace Compiler.Test.CreateAST;

public class TestLoggerProvider : ILoggerProvider
{
    private readonly TestLogger logger = new TestLogger();

    public ILogger CreateLogger(string categoryName) => logger;
    
    public void Dispose()
    {
    }

    public List<string> GetMessages() => logger.Messages;
}

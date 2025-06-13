using Microsoft.Extensions.Logging;
using Moq;

namespace Geowerkstatt.Interlis.RepositoryCrawler.TestHelpers;

public class MockLoggerProvider : ILoggerProvider
{
    public Mock<ILogger> LoggerMock { get; } = new Mock<ILogger>();

    public ILogger CreateLogger(string categoryName) => LoggerMock.Object;

    public void Dispose()
    {
    }
}

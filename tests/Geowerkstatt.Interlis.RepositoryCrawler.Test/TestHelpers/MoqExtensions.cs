#nullable disable warnings
using Microsoft.Extensions.Logging;
using Moq;

namespace Geowerkstatt.Interlis.RepositoryCrawler.TestHelpers;

internal static class MoqExtensions
{
    internal static void Verify(this Mock<ILogger> loggerMock, LogLevel expectedLogLevel, string expectedLogSubstring = "", Times? times = null)
        => loggerMock.Verify(l => l.Log(
                expectedLogLevel,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o.ToString().Contains(expectedLogSubstring, StringComparison.OrdinalIgnoreCase)),
                It.IsAny<Exception>(),
                (Func<It.IsAnyType, Exception, string>)It.IsAny<object>()),
             times ?? Times.AtLeastOnce());
}

using Geowerkstatt.Interlis.Compiler.AST;
using Microsoft.Extensions.Logging;

namespace Geowerkstatt.Interlis.Compiler.Test;

/// <summary>
/// A compiler problem as a consumer of the log output sees it: the entry's level and message plus the
/// <see cref="RangePosition"/> read from the <c>{Range}</c> placeholder of the message, which carries the source URI.
/// </summary>
public sealed record CollectedDiagnostic(LogLevel Level, string Message, RangePosition? Range);

/// <summary>
/// Collects warnings and errors together with their location the way a diagnostics consumer (e.g. a language
/// server) would: as an <see cref="ILoggerProvider"/> that reads the <c>Range</c> value off each entry's state.
/// </summary>
public sealed class DiagnosticCollector : ILoggerProvider
{
    private readonly List<CollectedDiagnostic> diagnostics = [];

    public IReadOnlyList<CollectedDiagnostic> Diagnostics
    {
        get
        {
            lock (diagnostics)
            {
                return diagnostics.ToList();
            }
        }
    }

    public ILogger CreateLogger(string categoryName) => new CollectingLogger(this);

    public void Dispose()
    {
    }

    private sealed class CollectingLogger(DiagnosticCollector owner) : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => logLevel >= LogLevel.Warning;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (!IsEnabled(logLevel))
            {
                return;
            }

            var range = state is IReadOnlyList<KeyValuePair<string, object?>> values
                ? values.FirstOrDefault(pair => pair.Key == "Range").Value as RangePosition
                : null;

            lock (owner.diagnostics)
            {
                owner.diagnostics.Add(new CollectedDiagnostic(logLevel, formatter(state, exception), range));
            }
        }
    }
}

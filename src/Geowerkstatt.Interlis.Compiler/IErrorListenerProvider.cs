using Antlr4.Runtime;

namespace Geowerkstatt.Interlis.Tools;

/// <summary>
/// Supplies a possibly generic <see cref="IAntlrErrorListener{TSymbol}"/>.
/// </summary>
public interface IErrorListenerProvider
{
    IAntlrErrorListener<T> GetErrorListener<T>();
}

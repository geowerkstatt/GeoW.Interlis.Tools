using Geowerkstatt.Interlis.Compiler.AST.Types;

namespace Geowerkstatt.Interlis.Compiler.AST.Expression;

public interface IExpression : ISourceRange
{
    /// <summary>
    /// The type of the result of the expression
    /// </summary>
    TypeDef ReturnType { get; }
}

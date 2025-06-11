using Geowerkstatt.Interlis.Compiler.AST.Types;

namespace Geowerkstatt.Interlis.Compiler.AST.Expression;

public interface IExpression
{
    TypeDef ReturnType { get; }
}

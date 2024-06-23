using Geowerkstatt.Interlis.Tools.AST.Types;

namespace Geowerkstatt.Interlis.Tools.AST.Expression;

public interface IExpression
{
    TypeDef ReturnType { get; }
}

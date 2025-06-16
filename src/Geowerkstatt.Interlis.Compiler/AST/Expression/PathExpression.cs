using Geowerkstatt.Interlis.Compiler.AST.Types;

namespace Geowerkstatt.Interlis.Compiler.AST.Expression;

public class PathExpression : ConstantExpression
{
    public IList<IPathElement> Path { get; } = new List<IPathElement>();

    public PathExpression() : base(new ObjectType())
    {
    }
}

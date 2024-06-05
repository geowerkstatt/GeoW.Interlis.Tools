namespace Geowerkstatt.Interlis.Tools.AST;

public sealed class InterlisFile : IAstElement, IContainer<IInterlisDefinition>
{
    public Dictionary<string, IInterlisDefinition> Content { get; } = new Dictionary<string, IInterlisDefinition>();

    public TResult? Accept<TResult>(IInterlis24AstVisitor<TResult> visitor)
    {
        return visitor.VisitInterlisFile(this);
    }
}

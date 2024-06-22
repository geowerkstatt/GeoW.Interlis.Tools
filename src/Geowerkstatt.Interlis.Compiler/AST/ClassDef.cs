using Geowerkstatt.Interlis.Tools.AST.Types;

namespace Geowerkstatt.Interlis.Tools.AST;

public sealed class ClassDef : IAstElement, IInterlisDefinition, IDocumentation, IContainer<IInterlisDefinition>, IExtending<ClassDef>
{
    public required string Name { get; init; }
    public IInterlisDefinition? Parent { get; set; }

    public IList<string> DocComments { get; } = new List<string>();
    public IDictionary<string, string> MetaAttributes { get; } = new Dictionary<string, string>();

    public ClassDef? Extends { get; set; }

    public Dictionary<string, IInterlisDefinition> Content { get; } = new Dictionary<string, IInterlisDefinition>();

    public bool IsStructure { get; init; }

    public TypeDef? OidType { get; set; }

    public TResult? Accept<TResult>(IInterlis24AstVisitor<TResult> visitor)
    {
        return visitor.VisitClassDef(this);
    }
}

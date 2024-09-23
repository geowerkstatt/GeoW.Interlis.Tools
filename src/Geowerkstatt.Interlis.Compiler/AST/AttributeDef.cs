using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Geowerkstatt.Interlis.Tools.AST.Types;

namespace Geowerkstatt.Interlis.Tools.AST;

public sealed class AttributeDef : IInterlisDefinition, IDocumentation
{
    public required string Name { get; init; }
    public string FullyQualifiedName => Parent != null ? $"{Parent.FullyQualifiedName} -> {Name}" : Name;
    public IInterlisDefinitionContainer? Parent { get; set; } = null;

    public IList<string> DocComments { get; } = new List<string>();
    public IDictionary<string, string> MetaAttributes { get; } = new Dictionary<string, string>();
    public required TypeDef TypeDef { get; init; }

    public TResult? Accept<TResult>(IInterlis24AstVisitor<TResult> visitor)
    {
        return visitor.VisitAttributeDef(this);
    }
}

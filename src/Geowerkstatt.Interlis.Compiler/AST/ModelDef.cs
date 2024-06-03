using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Geowerkstatt.Interlis.Tools.AST;

public sealed class ModelDef : IAstElement, IInterlisDefinition, IDocumentation, IContainer<IInterlisDefinition>
{
    public required Identifier FullyQualifiedName { get; init; }
    public IList<string> DocComments { get; } = new List<string>();
    public IDictionary<string, string> MetaAttributes { get; } = new Dictionary<string, string>();
    public string? Language { get; set; }
    public string? URI { get; set; }
    public string? Version { get; set; }
    public List<IInterlisDefinition> Children { get; } = new List<IInterlisDefinition>();

    public TResult? Accept<TResult>(IInterlis24AstVisitor<TResult> visitor)
    {
        return visitor.VisitModelDef(this);
    }
}

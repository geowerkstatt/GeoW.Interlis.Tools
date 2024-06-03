using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Geowerkstatt.Interlis.Tools.AST;

public sealed class ClassDef : IAstElement, IInterlisDefinition, IDocumentation
{
    public required Identifier FullyQualifiedName { get; init; }
    public IList<string> DocComments { get; } = new List<string>();
    public IDictionary<string, string> MetaAttributes { get; } = new Dictionary<string, string>();

    public TResult? Accept<TResult>(IInterlis24AstVisitor<TResult> visitor)
    {
        return visitor.VisitClassDef(this);
    }
}

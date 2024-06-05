using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Geowerkstatt.Interlis.Tools.AST;

public class AssociationDef : IAstElement, IInterlisDefinition
{
    public required string Name { get; init; }
    public IInterlisDefinition? Parent { get; set; } = null;

    public IList<AttributeDef> RoleDefs { get; } = new List<AttributeDef>();

    public TResult? Accept<TResult>(IInterlis24AstVisitor<TResult> visitor)
    {
        return visitor.VisitAssociationDef(this);
    }
}

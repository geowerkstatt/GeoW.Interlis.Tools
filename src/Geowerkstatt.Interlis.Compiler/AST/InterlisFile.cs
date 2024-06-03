using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Geowerkstatt.Interlis.Tools.AST;

public sealed class InterlisFile : IAstElement, IContainer<ModelDef>
{
    public List<ModelDef> Children { get; } = new List<ModelDef>();

    public TResult? Accept<TResult>(IInterlis24AstVisitor<TResult> visitor)
    {
        return visitor.VisitInterlisFile(this);
    }
}

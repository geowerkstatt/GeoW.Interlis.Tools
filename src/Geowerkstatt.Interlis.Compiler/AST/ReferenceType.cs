using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Geowerkstatt.Interlis.Tools.AST;

public class ReferenceType : ITypeDef
{
    public required Cardinality Cardinality { get; set; }

    /// <summary>
    /// The target of the reference.
    /// </summary>
    public IInterlisDefinition? Target { get; set; }

    /// <summary>
    /// Only these subclasses of <see cref="Target"/> are allowed.
    /// </summary>
    public IList<IInterlisDefinition> Restrictions { get; } = new List<IInterlisDefinition>();
}

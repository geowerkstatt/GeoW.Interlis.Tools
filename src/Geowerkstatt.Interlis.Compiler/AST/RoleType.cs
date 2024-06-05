using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Geowerkstatt.Interlis.Tools.AST;

public class RoleType : ITypeDef
{
    public required Cardinality Cardinality { get; set; }

    /// <summary>
    /// The accepted target classes with their respective restrictions.
    /// </summary>
    public List<RestrictedRef> Targets { get; } = new List<RestrictedRef>();
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Geowerkstatt.Interlis.Tools.AST;

public class RestrictedRef
{
    public Reference<IInterlisDefinition>? Value { get; set; }

    public List<Reference<IInterlisDefinition>> Restrictions { get; } = new List<Reference<IInterlisDefinition>>();
}

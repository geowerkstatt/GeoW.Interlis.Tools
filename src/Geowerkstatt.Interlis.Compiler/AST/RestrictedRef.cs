using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Geowerkstatt.Interlis.Tools.AST;

public class RestrictedRef
{
    public IInterlisDefinition? Target { get; set; }

    public List<IInterlisDefinition> Restrictions { get; } = new List<IInterlisDefinition>();
}

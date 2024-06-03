using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Geowerkstatt.Interlis.Tools.AST;

public interface ITypeDef
{
    Cardinality Cardinality { get; }
}

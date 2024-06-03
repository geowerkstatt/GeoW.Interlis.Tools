using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Geowerkstatt.Interlis.Tools.AST;

public class TypeDef : ITypeDef
{
    public Identifier? FullyQualifiedName { get; init; }

    /// <summary>
    /// Interlis-syntax string for now
    /// </summary>
    public string? Definition { get; init; }

    public Cardinality? Cardinality { get; set; }
}

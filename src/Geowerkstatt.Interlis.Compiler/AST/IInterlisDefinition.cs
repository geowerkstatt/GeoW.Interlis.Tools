using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Geowerkstatt.Interlis.Tools.AST;

/// <summary>
/// An INTERLIS object that can be referenced by its fully qualified name.
/// </summary>
public interface IInterlisDefinition
{
    public Identifier FullyQualifiedName { get; }
}

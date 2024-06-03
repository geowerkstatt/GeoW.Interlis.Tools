using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Geowerkstatt.Interlis.Tools.AST;

/// <summary>
/// An element that acts as a container for other elements.
/// </summary>
public interface IContainer<T>
{
    List<T> Children { get; }
}

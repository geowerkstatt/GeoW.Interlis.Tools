namespace Geowerkstatt.Interlis.Compiler.AST;

/// <summary>
/// An INTERLIS object that can extend another INTERLIS object.
/// </summary>
/// <typeparam name="T">The type of the object that is extended. Usually the same as the implementing type.</typeparam>
public interface IExtending<T> where T : class
{
    Reference<T>? Extends { get; set; }
}

namespace Geowerkstatt.Interlis.Tools.AST;

/// <summary>
/// An INTERLIS object that can extend another INTERLIS object.
/// </summary>
/// <typeparam name="T">The type of the object that is extended. Usually the same as the implementing type.</typeparam>
public interface IExtendable<T> where T : IInterlisDefinition
{
    T? Extends { get; set; }
}

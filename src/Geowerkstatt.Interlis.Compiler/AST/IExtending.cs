namespace Geowerkstatt.Interlis.Compiler.AST;

/// <summary>
/// An INTERLIS object that can extend another INTERLIS object, for code that handles every kind alike (e.g. follows
/// an EXTENDS chain) and does not need the type of the extended object.
/// </summary>
public interface IExtending
{
    IReference? Extends { get; }
}

/// <summary>
/// An INTERLIS object that can extend another INTERLIS object.
/// </summary>
/// <typeparam name="T">The type of the object that is extended. Usually the same as the implementing type.</typeparam>
public interface IExtending<T> : IExtending where T : class, IInterlisDefinition
{
    new Reference<T>? Extends { get; set; }

    IReference? IExtending.Extends => Extends;
}

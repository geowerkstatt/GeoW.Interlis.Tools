using Geowerkstatt.Interlis.Compiler.AST.Types;

namespace Geowerkstatt.Interlis.Compiler.AST;

/// <summary>
/// A class or structure parameter (keyword <c>PARAMETER</c>). Parameters describe properties that
/// concern the use of a (meta-)object in the application rather than the object itself (RefHB 3.10.2).
/// A parameter is either a typed value (<see cref="TypeDef"/>) or a <c>METAOBJECT</c> reference.
/// </summary>
public sealed class ParameterDef : InterlisDefinition
{
    public HashSet<Property> Properties { get; } = new HashSet<Property>();

    /// <summary>
    /// The parameter type when the parameter is defined as a typed value (<c>name : Type</c>);
    /// <see langword="null"/> for a <c>METAOBJECT</c> parameter.
    /// </summary>
    public TypeDef? TypeDef { get; set; }

    /// <summary>
    /// Whether the parameter is a <c>METAOBJECT</c> reference (RefHB 3.10.2.2).
    /// </summary>
    public bool IsMetaObject { get; set; }

    /// <summary>
    /// The target meta-object class for a <c>METAOBJECT OF</c> parameter; <see langword="null"/> for a
    /// plain <c>METAOBJECT</c> (which references the signature class the parameter is defined in).
    /// </summary>
    public Reference<ClassDef>? MetaObject { get; set; }

    public override TResult? Accept<TResult>(IInterlis24AstVisitor<TResult> visitor) where TResult : default
    {
        return visitor.VisitParameterDef(this);
    }
}

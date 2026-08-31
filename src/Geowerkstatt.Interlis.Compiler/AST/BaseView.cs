namespace Geowerkstatt.Interlis.Compiler.AST;

/// <summary>
/// A base view ("Basissicht", RefHB 3.15): a (possibly renamed) reference to a base viewable, carrying the local
/// base name under which the viewable is addressed. It is the single representation of a <c>[ Base '~' ] ViewableRef</c>
/// and is used by view formations (<see cref="ViewFormation"/>), base extensions (<see cref="BaseExtension"/>) and
/// derived associations (<see cref="AssociationDef.DerivedFrom"/>, RefHB 3.7.1).
/// <para>
/// The base <see cref="IInterlisDefinition.Name"/> is the explicit local <c>Base-Name</c> (<c>base ~ ViewableRef</c>)
/// or, when omitted, the unqualified name of the referenced viewable; <see cref="IsRenamed"/> records which form was
/// written, and <see cref="IInterlisDefinition.NameLocations"/> points at the base-name declaration (the alias token,
/// or the viewable reference for an implicit name).
/// </para>
/// <para>
/// Base names are Bestandteilnamen (RefHB 3.5.4) — the same name category as attributes and roles. A view formation's
/// base views are therefore registered in the view's <see cref="IInterlisDefinitionContainer.Content"/> (so they must
/// be uniquely named there and are addressable, e.g. for a language server's go-to-definition). Being a first-class
/// definition also lets the visitor reach it via <see cref="Accept{TResult}"/>.
/// </para>
/// </summary>
public sealed class BaseView : InterlisDefinition
{
    /// <summary>
    /// The referenced base viewable (class, structure, association or view).
    /// </summary>
    public required Reference<IInterlisDefinition>? Viewable { get; init; }

    /// <summary>
    /// Whether an explicit local base name was written (<c>base ~ ViewableRef</c>). When <see langword="false"/>,
    /// the <see cref="IInterlisDefinition.Name"/> was derived from the referenced viewable's own name.
    /// </summary>
    public bool IsRenamed { get; init; }

    public override TResult? Accept<TResult>(IInterlis24AstVisitor<TResult> visitor) where TResult : default
    {
        return visitor.VisitBaseView(this);
    }
}

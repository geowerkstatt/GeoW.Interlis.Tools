namespace Geowerkstatt.Interlis.Compiler.AST;

/// <summary>
/// How a <see cref="Reference{T}"/> finds its target. Every kind denotes names written in the source, so all are
/// registered in the enclosing container's <see cref="IInterlisDefinitionContainer.ContainerReferences"/> and a
/// consumer walking references (navigation, rename, diagnostics) sees all of them. They differ only in the lookup
/// that resolves them.
/// </summary>
public enum ReferenceResolution
{
    /// <summary>
    /// A name looked up in the lexical scopes by the <see cref="CreateAST.Interlis24AstReferenceResolverVisitor"/>:
    /// an unqualified name visible in scope, or a <c>Model[.Topic].Name</c> qualification.
    /// </summary>
    Scoped,

    /// <summary>
    /// A name looked up as a member of a container its context establishes: an attribute of the preceding path
    /// step's structure, a class of a metadata basket's topic, a role of a class's association access. The scoped
    /// resolver skips these — a lexical lookup could bind such a name to an unrelated same-named definition in
    /// scope (RefHB 3.13) — and the pass that owns the context writes the <see cref="Reference{T}.Target"/>
    /// instead.
    /// </summary>
    Member,

    /// <summary>
    /// An object or attribute path (RefHB 3.13, <c>A-&gt;B-&gt;C</c>), navigated step by step from the context
    /// viewable by the <see cref="CreateAST.Interlis24AstPathResolverVisitor"/>: each segment is a member of what
    /// the previous one reached — an attribute, a role, a view base, a substructure — or a keyword such as
    /// <c>THIS</c>. The scoped resolver skips these too; the path resolver writes every segment's target and the
    /// reference's.
    /// </summary>
    ObjectPath,
}

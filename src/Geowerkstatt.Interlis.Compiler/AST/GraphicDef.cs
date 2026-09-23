using Geowerkstatt.Interlis.Compiler.AST.Expression;

namespace Geowerkstatt.Interlis.Compiler.AST;

/// <summary>
/// A graphic description (<c>GRAPHIC</c>, RefHB 3.16). It assigns signature parameters to the objects of a
/// base viewable through a set of (conditional) drawing rules.
/// </summary>
public sealed class GraphicDef : InterlisDefinition, IExtending<GraphicDef>
{
    public HashSet<Property> Properties { get; } = new HashSet<Property>();

    /// <summary>
    /// The graphic this graphic extends (<c>EXTENDS</c>), if any.
    /// </summary>
    public Reference<GraphicDef>? Extends { get; set; }

    /// <summary>
    /// The viewable the graphic is <c>BASED ON</c>, if any.
    /// </summary>
    public Reference<IInterlisDefinition>? BasedOn { get; set; }

    /// <summary>
    /// The selection conditions (<c>WHERE</c>) restricting the objects the graphic applies to.
    /// </summary>
    public List<IExpression> Selections { get; } = new List<IExpression>();

    /// <summary>
    /// The drawing rules of the graphic.
    /// </summary>
    public List<DrawingRule> DrawingRules { get; } = new List<DrawingRule>();

    public override TResult? Accept<TResult>(IInterlis24AstVisitor<TResult> visitor) where TResult : default
    {
        return visitor.VisitGraphicDef(this);
    }
}

/// <summary>
/// A drawing rule of a <see cref="GraphicDef"/> (RefHB 3.16): assigns signature parameters, optionally using a
/// specific sign class (<c>OF</c>), through one or more conditional assignments.
/// </summary>
public sealed class DrawingRule : ISourceRange, IDocumentation
{
    public required string Name { get; init; }
    public RangePosition? SourceRange { get; init; }

    public IList<string> DocComments { get; } = new List<string>();
    public IDictionary<string, string> MetaAttributes { get; } = new Dictionary<string, string>();

    public HashSet<Property> Properties { get; } = new HashSet<Property>();

    /// <summary>
    /// The sign class the rule uses (<c>OF Sign-ClassRef</c>), if any.
    /// </summary>
    public Reference<IInterlisDefinition>? Sign { get; set; }

    public List<CondSignParamAssignment> Assignments { get; } = new List<CondSignParamAssignment>();
}

/// <summary>
/// A conditional block of signature-parameter assignments (<c>[WHERE expr] ( ... )</c>, RefHB 3.16).
/// </summary>
public sealed class CondSignParamAssignment
{
    public IExpression? Where { get; set; }

    public List<SignParamAssignment> Assignments { get; } = new List<SignParamAssignment>();
}

/// <summary>
/// A single signature-parameter assignment (<c>SignParameter-Name := ...</c>, RefHB 3.16). Exactly one of
/// <see cref="MetaObject"/>, <see cref="Value"/> or <see cref="According"/> is set.
/// </summary>
public sealed class SignParamAssignment
{
    public required string ParameterName { get; init; }

    /// <summary>
    /// The assigned meta-object reference (<c>{ MetaObjectRef }</c>). Documentary: it resolves as
    /// <see cref="ReferenceResolution.Member"/> and no pass writes its target yet, so it carries the written name
    /// and its span but no link.
    /// </summary>
    public Reference<IInterlisDefinition>? MetaObject { get; set; }

    /// <summary>The assigned value expression (<c>Factor</c>).</summary>
    public IExpression? Value { get; set; }

    /// <summary>The attribute path of an <c>ACCORDING</c> assignment.</summary>
    public PathExpression? According { get; set; }

    /// <summary>The per-enumeration-value assignments of an <c>ACCORDING</c> assignment.</summary>
    public List<EnumAssignment> EnumAssignments { get; } = new List<EnumAssignment>();
}

/// <summary>
/// A <c>WHEN IN</c> enumeration-range assignment inside an <c>ACCORDING</c> signature-parameter assignment
/// (RefHB 3.16).
/// </summary>
public sealed class EnumAssignment
{
    /// <summary>
    /// The assigned meta-object reference (<c>{ MetaObjectRef }</c>). Documentary: it resolves as
    /// <see cref="ReferenceResolution.Member"/> and no pass writes its target yet, so it carries the written name
    /// and its span but no link.
    /// </summary>
    public Reference<IInterlisDefinition>? MetaObject { get; set; }

    /// <summary>The assigned constant value.</summary>
    public IExpression? Value { get; set; }

    /// <summary>The (inclusive) start of the enumeration range this assignment applies to.</summary>
    public EnumerationConstant? RangeFrom { get; set; }

    /// <summary>The (inclusive) end of the enumeration range (<c>.. EnumerationConst</c>), if a range was given.</summary>
    public EnumerationConstant? RangeTo { get; set; }
}

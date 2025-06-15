namespace Geowerkstatt.Interlis.Compiler.AST;

/// <summary>
/// Represents a collection of models belonging together.
/// For example models from the same Interlis file or models that depend on each other.
/// </summary>
public sealed class InterlisEnvironment : IAstElement, IContainer<ModelDef>
{
    public double? Version { get; init; }

    public Dictionary<string, ModelDef> Content { get; } = new Dictionary<string, ModelDef>();

    public TResult? Accept<TResult>(IInterlis24AstVisitor<TResult> visitor)
    {
        return visitor.VisitInterlisEnvironment(this);
    }
}

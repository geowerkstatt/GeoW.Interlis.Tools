namespace Geowerkstatt.Interlis.Tools.AST;

/// <summary>
/// Implementing classes are part of the visitor pattern.
/// </summary>
public interface IAstElement
{
    TResult? Accept<TResult>(IInterlis24AstVisitor<TResult> visitor);
}

namespace Geowerkstatt.Interlis.Compiler.AST;

/// <summary>
/// Implementing classes are part of the visitor pattern.
/// </summary>
public interface IAstElement
{
    /// <summary>
    /// Double dispatch method for visitor pattern.
    /// </summary>
    TResult? Accept<TResult>(IInterlis24AstVisitor<TResult> visitor);
}

namespace Geowerkstatt.Interlis.Compiler.AST;

/// <summary>
/// An element the <see cref="IInterlis24AstVisitor{TResult}"/> dispatches on. This is a capability, not a category:
/// many AST nodes (every <see cref="Types.TypeDef"/>, <see cref="Types.RefSys"/>, <see cref="MetaObjectDeclaration"/>, …)
/// are reached through their owner instead of being visited in their own right.
/// </summary>
public interface IVisitable
{
    /// <summary>
    /// Double dispatch method for visitor pattern.
    /// </summary>
    TResult? Accept<TResult>(IInterlis24AstVisitor<TResult> visitor);
}

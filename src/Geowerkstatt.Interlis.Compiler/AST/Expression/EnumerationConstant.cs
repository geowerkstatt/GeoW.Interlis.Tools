using Geowerkstatt.Interlis.Compiler.AST.Types;

namespace Geowerkstatt.Interlis.Compiler.AST.Expression;

public class EnumerationConstant : ConstantExpression
{
    public static readonly string Others = Interlis24Parser.DefaultVocabulary.GetSymbolicName(Interlis24Parser.OTHERS);

    public List<string> Path { get; } = new List<string>();

    public EnumerationConstant() : base(new EnumerationType())
    {
    }
}

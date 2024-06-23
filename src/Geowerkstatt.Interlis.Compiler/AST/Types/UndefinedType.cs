namespace Geowerkstatt.Interlis.Tools.AST.Types;

/// <summary>
/// The undefined type represents the absence of a value.
/// </summary>
public class UndefinedType : TypeDef
{
    public static readonly UndefinedType Instance = new UndefinedType();

    private UndefinedType()
    {
    }
}

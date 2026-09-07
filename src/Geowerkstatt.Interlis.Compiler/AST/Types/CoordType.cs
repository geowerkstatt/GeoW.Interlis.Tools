namespace Geowerkstatt.Interlis.Compiler.AST.Types;

public class CoordType : TypeDef
{
    public bool IsMultiGeometry { get; set; }

    public List<NumericType> Axis { get; } = new List<NumericType>();

    /// <summary>
    /// The rotation definition (<c>ROTATION nullAxis -&gt; piHalfAxis</c>), if any (RefHB 3.8.8).
    /// </summary>
    public RotationDef? Rotation { get; set; }

    /// <summary>
    /// The EPSG code of the reference system given after <c>REFSYS</c> (e.g. <c>"EPSG:2056"</c>, pattern
    /// <c>EPSG:PosNumber</c> per RefHB 3.8.8-14), if any. Deliberately a plain string: unlike the axis-level
    /// <see cref="NumericType.RefSystem"/> (a <see cref="RefSys"/>), the clause names no model element — it
    /// carries the external registry code.
    /// </summary>
    public string? RefSysCode { get; set; }
}

namespace Geowerkstatt.Interlis.Compiler.AST.Types;

public class EnumerationType : TypeDef
{
    public Sequencings Sequencing { get; set; }

    public EnumerationValuesList Values { get; } = new EnumerationValuesList();

    public enum Sequencings
    {
        None,
        Ordered = Interlis24Parser.ORDERED,
        Circular = Interlis24Parser.CIRCULAR,
    }

    /// <summary>
    /// An extending enumeration is a delta on the inherited tree (RefHB 3.8.2-18/-19): the merged type carries
    /// the effective base's tree with the delta elements merged in (see <see cref="MergeEnumeration"/>), and the
    /// base's sequencing when this type declares none.
    /// </summary>
    internal override TypeDef MergeWithBase(TypeDef effectiveBase)
    {
        if (effectiveBase is not EnumerationType baseEnum)
        {
            return this;
        }

        var merged = new EnumerationType
        {
            Sequencing = Sequencing == Sequencings.None ? baseEnum.Sequencing : Sequencing,
            Values = { CopyTree(baseEnum.Values) },
        };
        MergeEnumeration(merged.Values, Values, new HashSet<EnumerationTreeNode>(), string.Empty, null);
        return merged;
    }

    /// <summary>
    /// Merges an authored enumeration into the effective (inherited) enumeration it refines, element by element
    /// (see <see cref="MergeElement"/>), and transfers an authored <c>FINAL</c> marker onto the effective
    /// enumeration — after the authored elements, so elements added alongside the marker are themselves legal
    /// (RefHB 3.8.2-19 allows declaring FINAL with or without new elements).
    /// <para>
    /// The merge is total: an illegal delta is reported through <paramref name="report"/> (no-op when
    /// <see langword="null"/>) and still merged best-effort — the type checker owns rejection, the flattening
    /// fold owns totality. Duplicates among the authored elements themselves are not reported here — the type
    /// checker validates the authored tree separately.
    /// </para>
    /// </summary>
    /// <param name="effective">The effective enumeration merged into; mutated.</param>
    /// <param name="authored">The authored delta applied onto <paramref name="effective"/>.</param>
    /// <param name="added">
    /// The elements <paramref name="effective"/> gained from the current authored enumeration, as opposed to
    /// inherited ones — only inherited elements can be identified for refinement (RefHB 3.8.2-17). Pass an empty
    /// set and reuse it across the recursion.
    /// </param>
    /// <param name="path">
    /// The dotted element path of the enumeration level being merged, empty for the root; diagnostics name
    /// elements by their full path in enumeration-constant form (<c>#rot.karmin</c>).
    /// </param>
    /// <param name="report">Receives a message per illegal delta element; <see langword="null"/> merges silently.</param>
    internal static void MergeEnumeration(EnumerationValuesList effective, EnumerationValuesList authored, HashSet<EnumerationTreeNode> added, string path, Action<string>? report)
    {
        foreach (var element in authored)
        {
            MergeElement(effective, element, added, path, report);
        }

        effective.IsFinal |= authored.IsFinal;
    }

    /// <summary>
    /// Merges one authored element into the effective enumeration level it is declared on: an element naming an
    /// inherited element refines it — the sub-enumerations merge recursively, an inherited leaf becomes a node
    /// (RefHB 3.8.2-18; legal even inside a FINAL enumeration, which only forbids additions) — and any other
    /// element is appended, which a FINAL effective enumeration forbids (RefHB 3.8.2-19). Re-listing an
    /// inherited element without a sub-enumeration re-defines it instead of refining it, and the last name of a
    /// dotted element name must identify an inherited element (RefHB 3.8.2-17 — one added by the same authored
    /// enumeration is not a <i>bisheriges</i> element; ili2c accepts unmatched dotted names as new nested
    /// elements, we do not).
    /// </summary>
    private static void MergeElement(EnumerationValuesList effective, EnumerationTreeNode element, HashSet<EnumerationTreeNode> added, string path, Action<string>? report)
    {
        var elementPath = path.Length == 0 ? element.Name : $"{path}.{element.Name}";
        var existing = effective.Find(e => e.Name == element.Name);

        // The node is the last name of a dotted element name when no flagged single sub-element continues the
        // name — its sub-elements, if any, are the defined sub-enumeration.
        var endsDottedName = element.FromDottedName && !(element.SubValues.Count == 1 && element.SubValues[0].FromDottedName);
        if (endsDottedName && (existing == null || added.Contains(existing)))
        {
            report?.Invoke($"the dotted element name '#{elementPath}' must identify an element of the inherited enumeration");
        }

        // RefHB 3.8.2-19 foresees the bare (FINAL) form only for freezing an existing sub-enumeration. On an
        // element without an inherited one it creates an empty FINAL sub-enumeration instead, silently turning
        // the element into a node that is no valid value any more (only leaves are values, RefHB 3.8.2-1) and
        // never can be — ili2c accepts the form and drops the value, we reject it.
        if (element.SubValues.Count == 0 && element.SubValues.IsFinal
            && (existing == null || added.Contains(existing) || (existing.SubValues.Count == 0 && !existing.SubValues.IsFinal)))
        {
            report?.Invoke($"the element '#{elementPath}' has no sub-enumeration to declare FINAL");
        }

        if (existing == null)
        {
            if (effective.IsFinal)
            {
                report?.Invoke(path.Length == 0
                    ? $"can not add the element '#{elementPath}' because the inherited enumeration is FINAL"
                    : $"can not add the element '#{elementPath}' because the inherited sub-enumeration '{path}' is FINAL");
            }

            existing = Copy(element, withSubValues: false);
            added.Add(existing);
            effective.Add(existing);
        }
        else if (!added.Contains(existing) && element.SubValues.Count == 0 && !element.SubValues.IsFinal)
        {
            // Without sub-elements or a FINAL marker the element defines no sub-enumeration to refine the
            // inherited element with.
            report?.Invoke($"the element '#{elementPath}' is already defined by the inherited enumeration");
        }

        MergeEnumeration(existing.SubValues, element.SubValues, added, elementPath, report);
    }

    /// <summary>
    /// A deep copy of an effective enumeration, detached like the merged type it goes into — the inherited
    /// nodes must not be mutated when a delta is merged on top.
    /// </summary>
    internal static EnumerationValuesList CopyTree(EnumerationValuesList values)
    {
        var copy = new EnumerationValuesList(values.IsFinal);
        foreach (var node in values)
        {
            copy.Add(Copy(node, withSubValues: true));
        }

        return copy;
    }

    private static EnumerationTreeNode Copy(EnumerationTreeNode node, bool withSubValues)
    {
        var copy = new EnumerationTreeNode { Name = node.Name, FromDottedName = node.FromDottedName };
        foreach (var docComment in node.DocComments)
        {
            copy.DocComments.Add(docComment);
        }

        foreach (var metaAttribute in node.MetaAttributes)
        {
            copy.MetaAttributes.Add(metaAttribute);
        }

        if (withSubValues)
        {
            copy.SubValues.Add(CopyTree(node.SubValues));
        }

        return copy;
    }
}

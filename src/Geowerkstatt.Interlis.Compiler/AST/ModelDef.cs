namespace Geowerkstatt.Interlis.Compiler.AST;

public sealed class ModelDef : InterlisDefinition, IInterlisDefinitionContainer
{
    public Dictionary<string, IInterlisDefinition> Content { get; } = new Dictionary<string, IInterlisDefinition>();

    public IDictionary<string, (bool IsUnqualifiedAllowed, Reference<ModelDef> ModelDef)> Imports { get; } = new Dictionary<string, (bool, Reference<ModelDef>)>();

    public string? Language { get; set; }
    public string? URI { get; set; }
    public string? Version { get; set; }
    public string? Xmlns { get; set; }
    public ModelType Type { get; set; }

    /// <summary>
    /// The explanation (<c>//...//</c>) following the model version, if any.
    /// Treated as a comment by the standard mechanism (RefHB 3.2.6).
    /// </summary>
    public string? Explanation { get; set; }

    /// <summary>
    /// Whether the model is declared <c>NOINCREMENTALTRANSFER</c>, i.e. incremental transfer
    /// need not be supported (RefHB 3.5.1-6).
    /// </summary>
    public bool NoIncrementalTransfer { get; set; }

    /// <summary>
    /// The IANA charset name declared via <c>CHARSET</c>, if any (RefHB 3.5.1-9).
    /// </summary>
    public string? Charset { get; set; }

    /// <summary>
    /// The version of the <see cref="IInterlisDefinition.TranslationOf"/> model.
    /// </summary>
    public string? TranslationOfVersion { get; set; }

    public ICollection<IReference> ContainerReferences { get; } = new List<IReference>();

    /// <summary>
    /// The path or URL to the source interlis file where this model was defined.
    /// </summary>
    public string? SourceUri { get; set; }

    public override TResult? Accept<TResult>(IInterlis24AstVisitor<TResult> visitor) where TResult : default
    {
        return visitor.VisitModelDef(this);
    }

    public enum ModelType
    {
        /// <summary>
        /// The model has no special type and no constraints on the content
        /// </summary>
        None = 0,

        /// <summary>
        /// A type model that contains only Domains, Units etc.
        /// </summary>
        Type = Interlis24Parser.TYPE,

        /// <summary>
        /// A refsystem model that should only contains classes etc. related to REFSYSTEM.
        /// </summary>
        Refsystem = Interlis24Parser.REFSYSTEM,

        /// <summary>
        /// A symbology model that should only contains classes etc. related to SIGN.
        /// </summary>
        Symbology = Interlis24Parser.SYMBOLOGY,
    }
}

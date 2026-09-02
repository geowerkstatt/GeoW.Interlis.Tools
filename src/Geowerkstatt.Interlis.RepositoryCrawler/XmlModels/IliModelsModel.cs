using System.Xml.Serialization;

namespace Geowerkstatt.Interlis.RepositoryCrawler.XmlModels;

[Serializable]
[XmlType(AnonymousType = true)]
[XmlRoot("DATASECTION")]
public class IliModelsDatasection
{
    [XmlElement("IliRepository09.RepositoryIndex", typeof(RepositoryIndex09))]
    [XmlElement("IliRepository20.RepositoryIndex", typeof(RepositoryIndex20))]
    public RepositoryIndex[]? Items { get; set; } = Array.Empty<RepositoryIndex>();
}

[Serializable]
public class RepositoryIndex
{
    [XmlElement("IliRepository09.RepositoryIndex.ModelMetadata", typeof(ModelMetadata09))]
    [XmlElement("IliRepository20.RepositoryIndex.ModelMetadata", typeof(ModelMetadata20))]
    public ModelMetadata[] ModelMetadata { get; set; } = Array.Empty<ModelMetadata>();
}

public class RepositoryIndex09 : RepositoryIndex
{
}

public class RepositoryIndex20 : RepositoryIndex
{
}

[Serializable]
[XmlType(AnonymousType = true)]
public class ModelMetadata
{
    public string? Name { get; set; }

    public string? SchemaLanguage { get; set; }

    public string? File { get; set; }

    public string? Version { get; set; }

    [XmlElement(DataType = "date")]
    public DateTime? publishingDate { get; set; }

    public string? Title { get; set; }

    [XmlArray("dependsOnModel")]
    [XmlArrayItem("IliRepository09.ModelName_", typeof(DependsOnModel09))]
    [XmlArrayItem("IliRepository20.ModelName_", typeof(DependsOnModel20))]
    public DependsOnModel[] dependsOnModel { get; set; } = Array.Empty<DependsOnModel>();

    public string? Tags { get; set; }

    public string? shortDescription { get; set; }

    public string? Issuer { get; set; }

    public string? technicalContact { get; set; }

    public string? furtherInformation { get; set; }

    public string? md5 { get; set; }
}

public class ModelMetadata09 : ModelMetadata
{
}

public class ModelMetadata20 : ModelMetadata
{
}

[Serializable]
[XmlType(AnonymousType = true)]
public class DependsOnModel
{
    public string? value { get; set; }
}

public class DependsOnModel20 : DependsOnModel
{
}

public class DependsOnModel09 : DependsOnModel
{
}

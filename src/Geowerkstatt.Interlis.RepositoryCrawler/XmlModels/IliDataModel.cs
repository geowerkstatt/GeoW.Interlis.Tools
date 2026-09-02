using System.Xml.Serialization;

namespace Geowerkstatt.Interlis.RepositoryCrawler.XmlModels;

[Serializable]
[XmlType(AnonymousType = true)]
[XmlRoot("DATASECTION")]
public class IliDataDatasection
{
    [XmlElement("DatasetIdx16.DataIndex", IsNullable = false)]
    public DataIndex[]? DatasetIdx16DataIndex { get; set; }
}

[Serializable]
[XmlType(AnonymousType = true)]
public class DataIndex
{
    [XmlElement("DatasetIdx16.DataIndex.DatasetMetadata")]
    public DatasetMetadata[]? DatasetMetadata { get; set; }
}

[Serializable]
[XmlType(AnonymousType = true)]
public class DatasetMetadata
{
    public string? id { get; set; }

    public string? version { get; set; }

    public string? precursorVersion { get; set; }

    public FollowUpData? followupData { get; set; }

    [XmlElement(DataType = "date")]
    public DateTime publishingDate { get; set; }

    public string? owner { get; set; }

    public Title? title { get; set; }

    [XmlArray("categories")]
    [XmlArrayItem("DatasetIdx16.Code_", IsNullable = false)]
    public CategoryCodesCode[]? categories { get; set; }

    public string? technicalContact { get; set; }

    public string? furtherInformation { get; set; }

    [XmlArrayItem("DatasetIdx16.DataFile")]
    public DataFile[]? files { get; set; }

    [XmlArrayItem("DatasetIdx16.DataIndex.BasketMetadata")]
    public BasketMetadata[]? baskets { get; set; }
}

[Serializable]
[XmlType(AnonymousType = true)]
public class BasketMetadata
{
    public BasketModel? model { get; set; }
}

[Serializable]
[XmlType(AnonymousType = true)]
public class BasketModel
{
    [XmlElement("DatasetIdx16.ModelLink")]
    public ModelLink? ModelLink { get; set; }
}

[Serializable]
[XmlType(AnonymousType = true)]
public class ModelLink
{
    public string? name { get; set; }
}

[Serializable]
[XmlType(AnonymousType = true)]
public class FollowUpData
{
    [XmlElement("DatasetIdx16.DataLink")]
    public DataLink? DataLink { get; set; }
}

[Serializable]
[XmlType(AnonymousType = true)]
public class DataLink
{
    public string? datasetId { get; set; }
}

[Serializable]
[XmlType(AnonymousType = true)]
public class Title
{
    [XmlElement("DatasetIdx16.MultilingualText")]
    public MultilingualText? MultilingualText { get; set; }
}

[Serializable]
[XmlType(AnonymousType = true)]
public class MultilingualText
{
    [XmlArray("LocalisedText")]
    [XmlArrayItem("DatasetIdx16.LocalisedText")]
    public LocalisedText[]? LocalisedTexts { get; set; }
}

[Serializable]
[XmlType(AnonymousType = true)]
public class LocalisedText
{
    public string? Language { get; set; }
    public string? Text { get; set; }
}

[Serializable]
[XmlType(AnonymousType = true)]
public class CategoryCodesCode
{
    public string? value { get; set; }
}

[Serializable]
[XmlType(AnonymousType = true)]
public class DataFile
{
    public string? fileFormat { get; set; }

    [XmlArrayItem("DatasetIdx16.File")]
    public DatasetIdx16File[]? file { get; set; }
}

[Serializable]
[XmlType(AnonymousType = true)]
public class DatasetIdx16File
{
    public string? path { get; set; }
    public string? md5 { get; set; }
}

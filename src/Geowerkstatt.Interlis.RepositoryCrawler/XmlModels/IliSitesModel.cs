using System.Xml;
using System.Xml.Serialization;

namespace Geowerkstatt.Interlis.RepositoryCrawler.XmlModels;

[Serializable]
[XmlType(AnonymousType = false)]
[XmlRoot("DATASECTION")]
public class IliSitesDataSection
{
    [XmlElement("IliSite09.SiteMetadata")]
    public SiteMetadata? SiteMetadata { get; set; }
}

[Serializable]
[XmlType(AnonymousType = true)]
public class SiteMetadata
{
    [XmlElement("IliSite09.SiteMetadata.Site")]
    public Site? Site { get; set; }
}

[Serializable]
[XmlType(AnonymousType = true)]
public class Site
{
    public string? Name { get; set; }

    public string? Title { get; set; }

    public string? shortDescription { get; set; }

    public string? Owner { get; set; }

    public string? technicalContact { get; set; }

    [XmlArray("parentSite", IsNullable = true)]
    [XmlArrayItem("IliSite09.RepositoryLocation_")]
    public SiteLocation[]? parentSites { get; set; }

    [XmlArray("subsidiarySite", IsNullable = true)]
    [XmlArrayItem("IliSite09.RepositoryLocation_")]
    public SiteLocation[]? subsidiarySites { get; set; }
}

[Serializable]
public class SiteLocation
{
    public string? value { get; set; }
}

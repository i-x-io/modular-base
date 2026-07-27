using System.Xml;
using System.Xml.Linq;

namespace IX.Modularity.Architecture.Tests;

internal static class ProjectXmlDocumentLoader
{
    private const long MaximumDocumentCharacters = 1_000_000;

    public static XDocument Load(string path)
    {
        var settings = new XmlReaderSettings
        {
            DtdProcessing = DtdProcessing.Prohibit,
            XmlResolver = null,
            MaxCharactersInDocument = MaximumDocumentCharacters,
        };
        using var reader = XmlReader.Create(path, settings);
        return XDocument.Load(reader, LoadOptions.None);
    }
}

using System.Text.Json;
using System.Text.Json.Serialization;
using System.Xml.Linq;

namespace ModularBase.Build.Validation;

internal static class PackageCatalogFile
{
    private const int CurrentSchemaVersion = 1;
    private const string ManifestRelativePath = "eng/package-catalog.json";
    private static readonly JsonSerializerOptions s_serializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    public static void Write(string rootDirectory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(rootDirectory);
        string root = Path.GetFullPath(rootDirectory);
        string path = Path.Combine(root, ManifestRelativePath);
        _ = Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, Render(root));
    }

    public static void Validate(string rootDirectory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(rootDirectory);
        string root = Path.GetFullPath(rootDirectory);
        string path = Path.Combine(root, ManifestRelativePath);
        if (!File.Exists(path))
        {
            throw new InvalidDataException(
                $"Package catalog '{ManifestRelativePath}' is missing; run UpdatePackageCatalog.");
        }

        string expected = Render(root);
        string actual = File.ReadAllText(path).ReplaceLineEndings("\n");
        if (!string.Equals(actual, expected, StringComparison.Ordinal))
        {
            throw new InvalidDataException(
                $"Package catalog '{ManifestRelativePath}' is stale; run UpdatePackageCatalog.");
        }
    }

    private static string Render(string root)
    {
        Dictionary<string, string> guides = ReadGuides(root);
        string packageIndex = File.ReadAllText(Path.Combine(root, "docs", "packages", "README.md"));
        var centralPackages = XDocument.Load(Path.Combine(root, "Directory.Packages.props"));
        CatalogEntry[] entries = [.. centralPackages
            .Descendants()
            .Where(element => string.Equals(element.Name.LocalName, "PackageVersion", StringComparison.Ordinal)
                || string.Equals(element.Name.LocalName, "GlobalPackageReference", StringComparison.Ordinal))
            .Select(element => CreateEntry(root, element, guides, packageIndex))
            .OrderBy(entry => entry.Id, StringComparer.OrdinalIgnoreCase)];
        string[] duplicateIds = [.. entries
            .GroupBy(entry => entry.Id, StringComparer.OrdinalIgnoreCase)
            .Where(group => group.Skip(1).Any())
            .Select(group => group.Key)];
        if (duplicateIds.Length != 0)
        {
            throw new InvalidDataException(
                $"Central package IDs must be unique: {string.Join(", ", duplicateIds)}.");
        }

        string[] undocumentedGuides = [.. guides.Keys
            .Where(id => !entries.Any(entry => string.Equals(entry.Id, id, StringComparison.OrdinalIgnoreCase)))
            .Order(StringComparer.OrdinalIgnoreCase)];
        if (undocumentedGuides.Length != 0)
        {
            throw new InvalidDataException(
                $"Package guides have no central version: {string.Join(", ", undocumentedGuides)}.");
        }

        var manifest = new CatalogManifest(
            CurrentSchemaVersion,
            "Directory.Packages.props",
            entries);
        return JsonSerializer.Serialize(manifest, s_serializerOptions).ReplaceLineEndings("\n") + "\n";
    }

    private static CatalogEntry CreateEntry(
        string root,
        XElement element,
        IReadOnlyDictionary<string, string> guides,
        string packageIndex)
    {
        string id = element.Attribute("Include")?.Value
            ?? throw new InvalidDataException("A central package entry has no Include attribute.");
        string version = element.Attribute("Version")?.Value
            ?? throw new InvalidDataException($"Central package '{id}' has no Version attribute.");
        string? guide = guides.GetValueOrDefault(id);
        if (guide is not null)
        {
            string[] introduction = [.. File.ReadLines(guide).Take(25)];
            if (!introduction.Any(line => line.Contains(version, StringComparison.Ordinal)))
            {
                throw new InvalidDataException(
                    $"Package guide '{Path.GetFileName(guide)}' does not declare central version '{version}'.");
            }

            string expectedIndexEntry =
                $"| [`{id}`]({Path.GetFileName(guide)}) | `{version}` |";
            if (!packageIndex.Contains(expectedIndexEntry, StringComparison.Ordinal))
            {
                throw new InvalidDataException(
                    $"Package index entry for '{id}' is missing or does not use central version '{version}'.");
            }
        }

        return new(
            id,
            version,
            string.Equals(element.Name.LocalName, "GlobalPackageReference", StringComparison.Ordinal)
                ? "global"
                : "central",
            guide is null ? null : Path.GetRelativePath(root, guide).Replace('\\', '/'));
    }

    private static Dictionary<string, string> ReadGuides(string root)
    {
        string directory = Path.Combine(root, "docs", "packages");
        return Directory.GetFiles(directory, "*.md", SearchOption.TopDirectoryOnly)
            .Where(path => !string.Equals(Path.GetFileName(path), "README.md", StringComparison.OrdinalIgnoreCase))
            .ToDictionary(
                ReadHeading,
                path => path,
                StringComparer.OrdinalIgnoreCase);
    }

    private static string ReadHeading(string path)
    {
        string? firstLine = File.ReadLines(path).FirstOrDefault();
        return firstLine?.StartsWith("# ", StringComparison.Ordinal) == true
            ? firstLine[2..].Trim()
            : throw new InvalidDataException(
                $"Package guide '{Path.GetFileName(path)}' must start with an exact package-ID heading.");
    }

    private sealed record CatalogManifest(
        int SchemaVersion,
        string GeneratedFrom,
        IReadOnlyList<CatalogEntry> Packages);

    private sealed record CatalogEntry(
        string Id,
        string Version,
        string ReferenceType,
        string? Guide);
}

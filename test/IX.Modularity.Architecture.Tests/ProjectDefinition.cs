using System.Xml.Linq;

namespace IX.Modularity.Architecture.Tests;

internal sealed class ProjectDefinition
{
    private ProjectDefinition()
    {
    }

    public required string FullPath
    {
        get; init;
    }

    public required string RelativePath
    {
        get; init;
    }

    public required string DirectoryPath
    {
        get; init;
    }

    public required string Name
    {
        get; init;
    }

    public required string Role
    {
        get; init;
    }

    public required IReadOnlyList<string> DeclaredRoles
    {
        get; init;
    }

    public required bool IsTestProject
    {
        get; init;
    }

    public required bool IsPackable
    {
        get; init;
    }

    public required bool HasPackageVersionMetadata
    {
        get; init;
    }

    public required IReadOnlyList<PackageReferenceDefinition> PackageReferences
    {
        get; init;
    }

    public required IReadOnlyList<ProjectReferenceDefinition> ProjectReferences
    {
        get; init;
    }

    public required bool HasCanonicalIdentityMetadata
    {
        get; init;
    }

    public IReadOnlyList<ProjectDefinition> References { get; set; } = [];

    public static ProjectDefinition Load(string repositoryRoot, string path)
    {
        XDocument document = ProjectXmlDocumentLoader.Load(path);
        string[] roles = [.. document.Descendants("IXModularityProjectRole").Select(static element => element.Value.Trim())];
        PackageReferenceDefinition[] packageReferences = [.. document.Descendants("PackageReference")
            .Select(static element => new PackageReferenceDefinition(
                element.Attribute("Include")?.Value ?? element.Attribute("Update")?.Value ?? string.Empty,
                GetMetadata(element, "PrivateAssets")))];

        return new ProjectDefinition
        {
            FullPath = Path.GetFullPath(path),
            RelativePath = Path.GetRelativePath(repositoryRoot, path).Replace('\\', '/'),
            DirectoryPath = Path.GetDirectoryName(path)!,
            Name = Path.GetFileNameWithoutExtension(path),
            Role = roles.SingleOrDefault() ?? string.Empty,
            DeclaredRoles = roles,
            IsTestProject = IsTrue(document, nameof(IsTestProject)),
            IsPackable = IsTrue(document, nameof(IsPackable)),
            HasPackageVersionMetadata = document.Descendants("PackageReference").Any(static element => element.Attribute("Version") is not null || element.Attribute("VersionOverride") is not null),
            HasCanonicalIdentityMetadata = IsCanonicalIdentityMetadata(document, path),
            PackageReferences = packageReferences,
            ProjectReferences = [.. document.Descendants("ProjectReference")
                .Select(static element => new ProjectReferenceDefinition
                {
                    Include = element.Attribute("Include")?.Value ?? string.Empty,
                    OutputItemType = GetMetadata(element, "OutputItemType"),
                    ReferenceOutputAssembly = GetMetadata(element, "ReferenceOutputAssembly"),
                    Kind = GetMetadata(element, "IXModularityProjectReferenceKind"),
                })
                .Where(static reference => reference.Include.Length > 0)],
        };
    }

    private static string GetMetadata(XElement element, string name)
    {
        return element.Elements().SingleOrDefault(child => string.Equals(child.Name.LocalName, name, StringComparison.Ordinal))?.Value.Trim()
            ?? element.Attribute(name)?.Value.Trim()
            ?? string.Empty;
    }

    private static bool IsTrue(XDocument document, string propertyName)
    {
        return string.Equals(document.Descendants(propertyName).SingleOrDefault()?.Value.Trim(), "true", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsCanonicalIdentityMetadata(XDocument document, string path)
    {
        string projectName = Path.GetFileNameWithoutExtension(path);
        return HasCanonicalIdentity(document, "AssemblyName", projectName)
            && HasCanonicalIdentity(document, "RootNamespace", projectName)
            && HasCanonicalIdentity(document, "PackageId", projectName);
    }

    private static bool HasCanonicalIdentity(XDocument document, string propertyName, string projectName)
    {
        string? value = document.Descendants(propertyName).SingleOrDefault()?.Value.Trim();
        return string.IsNullOrEmpty(value) || string.Equals(value, projectName, StringComparison.Ordinal);
    }
}

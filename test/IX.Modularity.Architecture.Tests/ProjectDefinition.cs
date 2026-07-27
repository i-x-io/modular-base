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

    public required IReadOnlyList<string> PackageReferences
    {
        get; init;
    }

    public required IReadOnlyList<string> ProjectReferenceIncludes
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
        string[] packageReferences = [.. document.Descendants("PackageReference").Select(static element => element.Attribute("Include")?.Value ?? element.Attribute("Update")?.Value ?? string.Empty)];

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
            ProjectReferenceIncludes = document.Descendants("ProjectReference").Select(static element => element.Attribute("Include")?.Value ?? string.Empty).Where(static include => include.Length > 0).ToArray(),
        };
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

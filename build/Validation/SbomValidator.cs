using System.Text.Json;

namespace ModularBase.Build.Validation;

internal static class SbomValidator
{
    public static string Validate(string sbomDirectory)
    {
        string[] candidates = Directory.GetFiles(sbomDirectory, "*.json", SearchOption.AllDirectories);
        if (candidates.Length != 1)
        {
            throw new InvalidDataException($"Expected one JSON SBOM but found {candidates.Length}.");
        }

        using var document = JsonDocument.Parse(File.ReadAllText(candidates[0]));
        bool valid = document.RootElement.TryGetProperty("components", out JsonElement components)
            && components.ValueKind == JsonValueKind.Array
            && components.GetArrayLength() > 0;
        return valid
            ? candidates[0]
            : throw new InvalidDataException("The generated SBOM contains no components.");
    }

    public static string Validate(string sbomDirectory, PackageInspection package)
    {
        ArgumentNullException.ThrowIfNull(package);
        string path = Validate(sbomDirectory);
        using var document = JsonDocument.Parse(File.ReadAllText(path));
        JsonElement root = document.RootElement;
        RequireString(root, "bomFormat", "CycloneDX");

        if (!root.TryGetProperty("metadata", out JsonElement metadata)
            || !metadata.TryGetProperty("component", out JsonElement component))
        {
            throw new InvalidDataException("The generated SBOM has no root metadata component.");
        }

        RequireString(component, "type", "library");
        RequireString(component, "name", package.Project.PackageId);
        RequireString(component, "version", package.Version.ToNormalizedString());
        RequireString(
            component,
            "purl",
            $"pkg:nuget/{package.Project.PackageId}@{package.Version.ToNormalizedString()}");

        JsonElement components = root.GetProperty("components");
        var componentNames = components.EnumerateArray()
            .Where(item => item.TryGetProperty("name", out JsonElement name)
                && name.ValueKind == JsonValueKind.String)
            .Select(item => item.GetProperty("name").GetString())
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        string[] missingDependencies = [.. package.Project.RuntimeDependencies
            .Where(dependency => !componentNames.Contains(dependency))
            .Order(StringComparer.Ordinal)];
        return missingDependencies.Length == 0
            ? path
            : throw new InvalidDataException(
                $"The generated SBOM omits runtime dependencies: {string.Join(", ", missingDependencies)}.");
    }

    private static void RequireString(JsonElement element, string propertyName, string expected)
    {
        if (!element.TryGetProperty(propertyName, out JsonElement property)
            || property.ValueKind != JsonValueKind.String
            || !string.Equals(property.GetString(), expected, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidDataException(
                $"The generated SBOM property '{propertyName}' must equal '{expected}'.");
        }
    }
}

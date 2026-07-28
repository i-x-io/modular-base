using System.Text.Json;

namespace ModularBase.Build.Validation;

internal static class DependencyAuditParser
{
    public static int CountFindings(string json)
    {
        using var document = JsonDocument.Parse(json);
        return !document.RootElement.TryGetProperty("projects", out JsonElement projects)
            || projects.ValueKind != JsonValueKind.Array
            ? throw new InvalidDataException("The dependency-audit report does not contain a projects array.")
            : projects.EnumerateArray()
                .SelectMany(GetFrameworks)
                .Sum(framework => CountPackages(framework, "topLevelPackages")
                    + CountPackages(framework, "transitivePackages"));
    }

    private static IEnumerable<JsonElement> GetFrameworks(JsonElement project)
    {
        return !project.TryGetProperty("frameworks", out JsonElement frameworks)
            || frameworks.ValueKind != JsonValueKind.Array
            ? throw new InvalidDataException(
                "A dependency-audit project does not contain a frameworks array.")
            : (IEnumerable<JsonElement>)frameworks.EnumerateArray();
    }

    private static int CountPackages(JsonElement framework, string propertyName)
    {
        return framework.TryGetProperty(propertyName, out JsonElement packages)
            && packages.ValueKind == JsonValueKind.Array
                ? packages.GetArrayLength()
                : 0;
    }
}

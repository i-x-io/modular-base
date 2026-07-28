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
}

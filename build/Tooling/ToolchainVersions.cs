using System.Text.Json;
using System.Xml.Linq;
using Nuke.Common.IO;

namespace ModularBase.Build.Tooling;

internal sealed record ToolchainVersions(string DotNetSdk, string Nuke)
{
    public string? CycloneDx
    {
        get;
        init;
    }

    public static ToolchainVersions Read(AbsolutePath rootDirectory)
    {
        ArgumentNullException.ThrowIfNull(rootDirectory);
        using var document = JsonDocument.Parse(File.ReadAllText(rootDirectory / "global.json"));
        string sdkVersion = document.RootElement
            .GetProperty("sdk")
            .GetProperty("version")
            .GetString()
            ?? throw new InvalidDataException("global.json does not define sdk.version.");

        var packages = XDocument.Load(rootDirectory / "Directory.Packages.props");
        string nukeVersion = packages
            .Descendants("PackageVersion")
            .Single(element => string.Equals(
                (string?)element.Attribute("Include"),
                "Nuke.Common",
                StringComparison.Ordinal))
            .Attribute("Version")?.Value
            ?? throw new InvalidDataException("Directory.Packages.props does not define Nuke.Common.");
        AbsolutePath toolManifestPath = rootDirectory / ".config" / "dotnet-tools.json";
        string? cycloneDxVersion = File.Exists(toolManifestPath)
            ? ReadCycloneDxVersion(toolManifestPath)
            : null;
        return new(sdkVersion, nukeVersion)
        {
            CycloneDx = cycloneDxVersion,
        };
    }

    private static string ReadCycloneDxVersion(string path)
    {
        using var toolManifest = JsonDocument.Parse(File.ReadAllText(path));
        return toolManifest.RootElement
            .GetProperty("tools")
            .GetProperty("cyclonedx")
            .GetProperty("version")
            .GetString()
            ?? throw new InvalidDataException(
                ".config/dotnet-tools.json does not define cyclonedx.version.");
    }
}

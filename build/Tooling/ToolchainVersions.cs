using System.Text.Json;
using System.Xml.Linq;
using Nuke.Common.IO;

namespace ModularBase.Build.Tooling;

internal sealed record ToolchainVersions(string DotNetSdk, string Nuke)
{
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
        return new(sdkVersion, nukeVersion);
    }
}

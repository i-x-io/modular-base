using System.Text.RegularExpressions;
using Nuke.Common.IO;

namespace ModularBase.Build.Repository;

internal sealed partial record BuildUnit
{
    public BuildUnit(string name, string artifactName, AbsolutePath path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(artifactName);
        ArgumentNullException.ThrowIfNull(path);

        if (!ArtifactNamePattern.IsMatch(artifactName))
        {
            throw new ArgumentException(
                "The artifact name must be a lowercase portable path segment.",
                nameof(artifactName));
        }

        Name = name;
        ArtifactName = artifactName;
        Path = path;
    }

    public string Name
    {
        get;
    }

    public string ArtifactName
    {
        get;
    }

    public AbsolutePath Path
    {
        get;
    }

    [GeneratedRegex(
        "^[a-z0-9]+(?:-[a-z0-9]+)*$",
        RegexOptions.CultureInvariant,
        matchTimeoutMilliseconds: 1000)]
    private static partial Regex ArtifactNamePattern
    {
        get;
    }
}

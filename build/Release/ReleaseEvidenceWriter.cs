using System.Text.Json;
using System.Text.Json.Serialization;
using ModularBase.Build.Pipeline;
using ModularBase.Build.Repository;
using ModularBase.Build.Tooling;
using ModularBase.Build.Validation;
using Nuke.Common.IO;

namespace ModularBase.Build.Release;

internal sealed class ReleaseEvidenceWriter(BuildPaths paths, ToolchainVersions toolchain)
{
    private readonly BuildPaths _paths = paths ?? throw new ArgumentNullException(nameof(paths));
    private readonly ToolchainVersions _toolchain = toolchain
        ?? throw new ArgumentNullException(nameof(toolchain));

    private static readonly JsonSerializerOptions s_serializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    public void WritePlan(ReleasePlan plan)
    {
        ArgumentNullException.ThrowIfNull(plan);
        _ = _paths.ReleaseDirectory.CreateOrCleanDirectory();
        WriteJson(_paths.ReleasePlan, plan);
    }

    public ReleaseManifest CreateManifest(
        ReleasePlan plan,
        IReadOnlyCollection<PackageInspection> packages,
        IEnumerable<string> artifactPaths,
        RepositoryIdentity repository,
        BuildConfiguration configuration,
        DateTimeOffset createdAtUtc)
    {
        ArgumentNullException.ThrowIfNull(plan);
        ArgumentNullException.ThrowIfNull(packages);
        ArgumentNullException.ThrowIfNull(artifactPaths);
        ArgumentNullException.ThrowIfNull(repository);

        ReleaseArtifact[] artifacts = [.. artifactPaths
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Order(StringComparer.Ordinal)
            .Select(CreateArtifact)];
        ReleasePackage[] releasePackages = [.. packages
            .OrderBy(package => package.Project.PackageId, StringComparer.Ordinal)
            .Select(package => new ReleasePackage(
                package.Project.PackageId,
                package.Version.ToNormalizedString(),
                RelativePath(package.Project.ProjectFile),
                package.Project.TargetFrameworks))];
        var manifest = new ReleaseManifest(
            ReleaseManifest.CurrentSchemaVersion,
            repository.HttpsUrl,
            plan.Commit,
            plan.Tag,
            configuration.ToString(),
            _toolchain.DotNetSdk,
            _toolchain.Nuke,
            createdAtUtc,
            releasePackages,
            artifacts)
        {
            CycloneDxVersion = _toolchain.CycloneDx,
        };

        _ = _paths.ReleaseEvidenceDirectory.CreateOrCleanDirectory();
        WriteJson(_paths.ReleaseManifest, manifest);
        File.WriteAllLines(
            _paths.Checksums,
            artifacts.Select(artifact => $"{artifact.Sha256}  {artifact.RelativePath}"));
        return manifest;
    }

    private ReleaseArtifact CreateArtifact(string path)
    {
        var file = new FileInfo(path);
        return !file.Exists
            ? throw new FileNotFoundException("A release artifact does not exist.", path)
            : new(RelativePath(path), GetMediaType(path), file.Length, Hashing.Sha256(path));
    }

    private string RelativePath(string path)
    {
        return Path.GetRelativePath(_paths.RootDirectory, path).Replace('\\', '/');
    }

    private static void WriteJson<T>(string path, T value)
    {
        File.WriteAllText(
            path,
            JsonSerializer.Serialize(value, s_serializerOptions) + Environment.NewLine);
    }

    private static string GetMediaType(string path)
    {
        return Path.GetExtension(path).ToUpperInvariant() switch
        {
            ".JSON" => "application/json",
            ".TXT" => "text/plain",
            _ => "application/octet-stream",
        };
    }
}

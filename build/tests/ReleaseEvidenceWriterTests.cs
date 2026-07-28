using System.Text.Json;
using ModularBase.Build.Pipeline;
using ModularBase.Build.Release;
using ModularBase.Build.Repository;
using ModularBase.Build.Tooling;
using ModularBase.Build.Validation;
using NuGet.Versioning;

namespace ModularBase.Build.Tests;

public sealed class ReleaseEvidenceWriterTests
{
    [Fact]
    public void WritesOrderedChecksumsAndAMultiPackageManifest()
    {
        string root = Path.Combine(Path.GetTempPath(), $"modular-base-{Guid.NewGuid():N}");
        Directory.CreateDirectory(root);
        try
        {
            string first = Path.Combine(root, "a.nupkg");
            string second = Path.Combine(root, "b.json");
            File.WriteAllText(first, "package");
            File.WriteAllText(second, "{}");
            var paths = BuildPaths.Create(root, Path.Combine(root, "artifacts"));
            PackageInspection[] packages =
            [
                Package(root, "First", first),
                Package(root, "Second", first),
            ];
            var plan = new ReleasePlan(
                ReleasePlan.CurrentSchemaVersion,
                "abc123",
                "1.0.0",
                "v1.0.0",
                IsPrerelease: false,
                "RELEASE: publish",
                [new("First", "1.0.0"), new("Second", "1.0.0")]);
            var repository = new RepositoryIdentity(
                "github.com",
                "example",
                "repository",
                "example/repository",
                "https://github.com/example/repository",
                "abc123",
                new Uri("https://nuget.pkg.github.com/example/index.json"));
            var writer = new ReleaseEvidenceWriter(paths, new ToolchainVersions("10.0.302", "10.1.0"));

            writer.WritePlan(plan);
            ReleaseManifest manifest = writer.CreateManifest(
                plan,
                packages,
                [second, first],
                repository,
                BuildConfiguration.Release,
                DateTimeOffset.UnixEpoch);

            using var document = JsonDocument.Parse(File.ReadAllText(paths.ReleaseManifest));
            Assert.Equal(2, document.RootElement.GetProperty("schemaVersion").GetInt32());
            Assert.Equal(2, document.RootElement.GetProperty("packages").GetArrayLength());
            Assert.Equal(
                ["a.nupkg", "b.json"],
                manifest.Artifacts.Select(artifact => artifact.RelativePath),
                StringComparer.Ordinal);
            Assert.Equal(2, File.ReadAllLines(paths.Checksums).Length);
            Assert.True(File.Exists(paths.ReleasePlan));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    private static PackageInspection Package(string root, string packageId, string packageFile)
    {
        var project = new PackageProject(
            Path.Combine(root, "src", packageId + ".csproj"),
            packageId,
            packageId,
            "https://github.com/example/repository",
            "v",
            ["net10.0"],
            []);
        return new(project, packageFile, packageFile + ".symbols", NuGetVersion.Parse("1.0.0"));
    }
}

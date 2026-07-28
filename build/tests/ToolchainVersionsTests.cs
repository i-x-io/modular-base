using ModularBase.Build.Tooling;

namespace ModularBase.Build.Tests;

public sealed class ToolchainVersionsTests
{
    [Fact]
    public void ReadsVersionsFromRepositoryConfiguration()
    {
        string root = Path.Combine(Path.GetTempPath(), $"modular-base-toolchain-{Guid.NewGuid():N}");
        Directory.CreateDirectory(root);
        try
        {
            File.WriteAllText(
                Path.Combine(root, "global.json"),
                /*lang=json,strict*/ "{\"sdk\":{\"version\":\"10.0.302\"}}");
            File.WriteAllText(
                Path.Combine(root, "Directory.Packages.props"),
                "<Project><ItemGroup><PackageVersion Include=\"Nuke.Common\" Version=\"10.1.0\" /></ItemGroup></Project>");

            var result = ToolchainVersions.Read(root);

            Assert.Equal("10.0.302", result.DotNetSdk);
            Assert.Equal("10.1.0", result.Nuke);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }
}

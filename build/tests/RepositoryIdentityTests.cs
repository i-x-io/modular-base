using ModularBase.Build.Repository;
using Nuke.Common.Git;

namespace ModularBase.Build.Tests;

public sealed class RepositoryIdentityTests
{
    [Fact]
    public void DerivesGitHubIdentityAndPackageSource()
    {
        var repository = new GitRepository(
            GitProtocol.Https,
            "github.com",
            "example/repository",
            "main",
            localDirectory: null,
            head: null,
            commit: "abc123",
            tags: [],
            remoteName: "origin",
            remoteBranch: "main");

        var result = RepositoryIdentity.From(repository);

        Assert.Equal("example", result.Owner);
        Assert.Equal("repository", result.Name);
        Assert.Equal("example/repository", result.Identifier);
        Assert.Equal(
            "https://nuget.pkg.github.com/example/index.json",
            result.PackageSource.AbsoluteUri);
    }

    [Fact]
    public void ReportsAMissingRemoteIdentityClearly()
    {
        var repository = new GitRepository(
            protocol: null,
            endpoint: null,
            identifier: null,
            branch: "release/14-example",
            localDirectory: null,
            head: null,
            commit: "abc123",
            tags: [],
            remoteName: null,
            remoteBranch: null);

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(
            () => RepositoryIdentity.From(repository));

        Assert.Contains("Git repository identifier", exception.Message, StringComparison.Ordinal);
    }
}

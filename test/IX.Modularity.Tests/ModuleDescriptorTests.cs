namespace IX.Modularity.Tests;

public sealed class ModuleDescriptorTests
{
    [Theory]
    [InlineData("0.1.0")]
    [InlineData("1.0.0-alpha.1")]
    [InlineData("2.3.4-rc.1+build.42")]
    public void Constructor_accepts_semantic_versions(string version)
    {
        ModuleDescriptor descriptor = CreateDescriptor(version: version);

        Assert.Equal(version, descriptor.Version);
    }

    [Theory]
    [InlineData("1")]
    [InlineData("1.0")]
    [InlineData("01.0.0")]
    [InlineData("1.0.0-")]
    [InlineData("1.0.0+bad value")]
    public void Constructor_rejects_invalid_semantic_versions(string version)
    {
        _ = Assert.Throws<ArgumentException>(() => CreateDescriptor(version: version));
    }

    [Fact]
    public void Constructor_copies_dependencies()
    {
        var originalDependency = ModuleId.Parse("foundation");
        List<ModuleId> dependencies = [originalDependency];

        ModuleDescriptor descriptor = CreateDescriptor(dependencies: dependencies);
        dependencies.Clear();

        Assert.Equal([originalDependency], descriptor.Dependencies);
    }

    [Fact]
    public void Constructor_rejects_duplicate_dependencies()
    {
        var dependency = ModuleId.Parse("foundation");

        _ = Assert.Throws<ArgumentException>(() => CreateDescriptor(dependencies: [dependency, dependency]));
    }

    [Fact]
    public void Constructor_rejects_self_dependency()
    {
        var identifier = ModuleId.Parse("payments");

        _ = Assert.Throws<ArgumentException>(() => CreateDescriptor(identifier, dependencies: [identifier]));
    }

    private static ModuleDescriptor CreateDescriptor(
        ModuleId? id = null,
        string version = "1.0.0",
        IEnumerable<ModuleId>? dependencies = null)
    {
        return new(
            id ?? ModuleId.Parse("payments"),
            "Payments",
            version,
            dependencies ?? [],
            "Payment capabilities.");
    }
}

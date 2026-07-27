using ArchUnitNET.Fluent;
using ArchUnitNET.Loader;
using ArchUnitNET.xUnitV3;
using Xunit;
using static ArchUnitNET.Fluent.ArchRuleDefinition;
using ArchUnitArchitecture = ArchUnitNET.Domain.Architecture;

namespace IX.Modularity.Architecture.Tests;

/// <summary>Verifies repository metadata and the ArchUnitNET test harness.</summary>
public sealed class RepositoryArchitectureTests
{
    private static readonly string s_repositoryRoot = FindRepositoryRoot();

    /// <summary>Validates every discovered project and project reference.</summary>
    [Fact]
    public void Project_graph_conforms_to_the_repository_contract()
    {
        var graph = ProjectGraph.Load(s_repositoryRoot);

        _ = Assert.Single(graph.Projects, static project => string.Equals(project.Role, "ArchitectureTest", StringComparison.Ordinal));
        Assert.Equal(graph.Projects.Select(static project => project.RelativePath).Order(StringComparer.Ordinal), graph.SolutionProjectPaths.Order(StringComparer.Ordinal));
        Assert.Equal(graph.SolutionProjectPaths.Count, graph.SolutionProjectPaths.Distinct(StringComparer.Ordinal).Count());

        foreach (ProjectDefinition project in graph.Projects)
        {
            Assert.Contains(project.Role, ProjectGraph.ValidRoles, StringComparer.Ordinal);
            _ = Assert.Single(project.DeclaredRoles);
            Assert.Equal(project.DeclaredRoles[0], project.Role);
            Assert.Matches(ProjectGraph.NamePatternFor(project.Role), project.Name);
            Assert.Equal(ProjectGraph.IsTestRole(project.Role), project.IsTestProject);
            Assert.False(project.HasPackageVersionMetadata, "Central package management owns package versions.");
            Assert.True(ProjectGraph.IsCanonicalLocation(project));
            Assert.True(project.HasCanonicalIdentityMetadata);

            if (ProjectGraph.IsTestRole(project.Role))
            {
                Assert.False(project.IsPackable, "Test and ArchitectureTest projects must not be package assets.");
            }

            Assert.All(project.PackageReferences, packageReference =>
                Assert.True(
                    ProjectGraph.IsPackageReferenceAllowed(project.RelativePath, project.Role, packageReference),
                    $"{project.RelativePath} ({project.Role}) may not reference package '{packageReference.Id}'."));

            if (project.IsPackable)
            {
                Assert.True(File.Exists(Path.Combine(project.DirectoryPath, "PublicAPI.Shipped.txt")));
                Assert.True(File.Exists(Path.Combine(project.DirectoryPath, "PublicAPI.Unshipped.txt")));
            }

            foreach (ProjectReferenceDefinition reference in project.ProjectReferences.Where(ProjectGraph.IsCompilerToolReference))
            {
                Assert.True(ProjectGraph.HasValidCompilerToolMetadata(reference), $"{project.RelativePath} has incomplete compiler-tool reference metadata.");
                Assert.All(reference.ReferencedProjects, static referencedProject => Assert.True(ProjectGraph.IsCompilerToolTargetRole(referencedProject.Role)));
                Assert.DoesNotContain(reference.ReferencedProjects, referencedProject => project.References.Contains(referencedProject));
            }

            foreach (ProjectDefinition referencedProject in project.References)
            {
                Assert.True(ProjectGraph.IsReferenceAllowed(project.Role, referencedProject.Role), $"{project.RelativePath} ({project.Role}) may not reference {referencedProject.RelativePath} ({referencedProject.Role}).");
            }
        }

        Assert.False(graph.HasCycle());
    }

    [Fact]
    public void Compiler_tool_reference_contract_rejects_partial_metadata_and_non_compiler_roles()
    {
        ProjectReferenceDefinition valid = new()
        {
            Include = "../IX.Modularity.Analyzers/IX.Modularity.Analyzers.csproj",
            Kind = "CompilerTool",
            OutputItemType = "Analyzer",
            ReferenceOutputAssembly = "false",
        };
        ProjectReferenceDefinition partial = new()
        {
            Include = valid.Include,
            Kind = valid.Kind,
            OutputItemType = valid.OutputItemType,
            ReferenceOutputAssembly = string.Empty,
        };

        Assert.True(ProjectGraph.IsCompilerToolReference(valid));
        Assert.True(ProjectGraph.HasValidCompilerToolMetadata(valid));
        Assert.False(ProjectGraph.HasValidCompilerToolMetadata(partial));
        Assert.True(ProjectGraph.IsCompilerToolTargetRole("Analyzer"));
        Assert.True(ProjectGraph.IsCompilerToolTargetRole("SourceGenerator"));
        Assert.False(ProjectGraph.IsCompilerToolTargetRole("Library"));
    }

    [Theory]
    [InlineData("Library")]
    [InlineData("Contracts")]
    [InlineData("Abstractions")]
    [InlineData("Adapter")]
    [InlineData("Integration")]
    public void FluentResults_is_allowed_for_the_approved_production_roles(string role)
    {
        Assert.True(ProjectGraph.IsFluentResultsAllowedForProductionRole(role));
    }

    [Theory]
    [InlineData("Testing")]
    [InlineData("Analyzer")]
    [InlineData("SourceGenerator")]
    [InlineData("Test")]
    [InlineData("ArchitectureTest")]
    public void FluentResults_is_rejected_for_non_production_roles(string role)
    {
        Assert.False(ProjectGraph.IsFluentResultsAllowedForProductionRole(role));
    }

    [Fact]
    public void FluentResults_analyzer_test_fixture_is_exact_and_private()
    {
        Assert.True(ProjectGraph.IsFluentResultsAnalyzerTestFixture(
            "test/IX.Modularity.Analyzers.Tests/IX.Modularity.Analyzers.Tests.csproj",
            "Test",
            "all"));
        Assert.False(ProjectGraph.IsFluentResultsAnalyzerTestFixture(
            "test/Other.Tests/Other.Tests.csproj",
            "Test",
            "all"));
        Assert.False(ProjectGraph.IsFluentResultsAnalyzerTestFixture(
            "test/IX.Modularity.Analyzers.Tests/IX.Modularity.Analyzers.Tests.csproj",
            "ArchitectureTest",
            "all"));
        Assert.False(ProjectGraph.IsFluentResultsAnalyzerTestFixture(
            "test/IX.Modularity.Analyzers.Tests/IX.Modularity.Analyzers.Tests.csproj",
            "Test",
            "compile"));
    }

    [Theory]
    [InlineData("Library")]
    [InlineData("Contracts")]
    [InlineData("Abstractions")]
    [InlineData("Adapter")]
    [InlineData("Integration")]
    public void Package_reference_policy_allows_FluentResults_for_each_approved_role(string role)
    {
        Assert.True(ProjectGraph.IsPackageReferenceAllowed(
            "src/IX.Modularity.Sample/IX.Modularity.Sample.csproj",
            role,
            new PackageReferenceDefinition("FluentResults", string.Empty)));
    }

    [Theory]
    [InlineData("Testing")]
    [InlineData("Analyzer")]
    [InlineData("SourceGenerator")]
    [InlineData("Test")]
    [InlineData("ArchitectureTest")]
    public void Package_reference_policy_rejects_FluentResults_for_each_unapproved_role(string role)
    {
        Assert.False(ProjectGraph.IsPackageReferenceAllowed(
            "src/IX.Modularity.Sample/IX.Modularity.Sample.csproj",
            role,
            new PackageReferenceDefinition("FluentResults", string.Empty)));
    }

    [Fact]
    public void Package_reference_policy_allows_only_the_private_analyzer_test_fixture()
    {
        const string analyzerTestProject = "test/IX.Modularity.Analyzers.Tests/IX.Modularity.Analyzers.Tests.csproj";

        Assert.True(ProjectGraph.IsPackageReferenceAllowed(
            analyzerTestProject,
            "Test",
            new PackageReferenceDefinition("FluentResults", "all")));
        Assert.False(ProjectGraph.IsPackageReferenceAllowed(
            analyzerTestProject,
            "Test",
            new PackageReferenceDefinition("FluentResults", string.Empty)));
        Assert.False(ProjectGraph.IsPackageReferenceAllowed(
            analyzerTestProject,
            "Test",
            new PackageReferenceDefinition("FluentResults", "compile")));
    }

    [Theory]
    [InlineData("Contracts")]
    [InlineData("Abstractions")]
    public void Package_reference_policy_retains_neutral_role_package_restrictions(string role)
    {
        Assert.False(ProjectGraph.IsPackageReferenceAllowed(
            "src/IX.Modularity.Sample/IX.Modularity.Sample.csproj",
            role,
            new PackageReferenceDefinition("Example.Package", string.Empty)));
    }

    /// <summary>Proves a permitted fixture dependency satisfies an ArchUnitNET rule.</summary>
    [Fact]
    public void Self_test_architecture_rule_allows_a_permitted_dependency()
    {
        AllowedDependentFixture fixture = new();
        Assert.Equal("allowed", fixture.DependencyValue);

        ArchUnitArchitecture architecture = LoadTestAssemblyArchitecture();
        IArchRule allowed = Classes().That().HaveName(nameof(AllowedDependentFixture)).Should().DependOnAnyTypesThat().HaveName(nameof(AllowedDependencyFixture));

        allowed.Check(architecture);
    }

    /// <summary>Proves a forbidden fixture dependency is reported without failing this self-test.</summary>
    [Fact]
    public void Self_test_architecture_rule_detects_a_forbidden_dependency()
    {
        ViolatingFixture fixture = new();
        Assert.Equal("forbidden", fixture.DependencyValue);

        ArchUnitArchitecture architecture = LoadTestAssemblyArchitecture();
        IArchRule forbidden = Classes().That().HaveName(nameof(ViolatingFixture)).Should().NotDependOnAnyTypesThat().HaveName(nameof(ForbiddenDependencyFixture));

        void Check() => forbidden.Check(architecture);

        _ = Assert.ThrowsAny<Exception>(Check);
    }

    private static ArchUnitArchitecture LoadTestAssemblyArchitecture()
    {
#pragma warning disable RS0030 // ArchUnitNET requires the architecture test to load its own compiled assembly.
        var assembly = System.Reflection.Assembly.GetExecutingAssembly();
        return new ArchLoader().LoadAssemblies(assembly).Build();
#pragma warning restore RS0030
    }

    private static string FindRepositoryRoot()
    {
        for (DirectoryInfo? directory = new(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
        {
            if (File.Exists(Path.Combine(directory.FullName, "IX.Modularity.slnx")))
            {
                return directory.FullName;
            }
        }

        throw new DirectoryNotFoundException("Could not locate the repository root containing IX.Modularity.slnx.");
    }

    private sealed class AllowedDependencyFixture
    {
        public string Value { get; } = "allowed";
    }

    private sealed class AllowedDependentFixture
    {
        private readonly AllowedDependencyFixture _dependency = new();

        public string DependencyValue => _dependency.Value;
    }

    private sealed class ForbiddenDependencyFixture
    {
        public string Value { get; } = "forbidden";
    }

    private sealed class ViolatingFixture
    {
        private readonly ForbiddenDependencyFixture _dependency = new();

        public string DependencyValue => _dependency.Value;
    }
}

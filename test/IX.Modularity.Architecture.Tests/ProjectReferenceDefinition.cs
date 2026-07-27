namespace IX.Modularity.Architecture.Tests;

internal sealed class ProjectReferenceDefinition
{
    public required string Include
    {
        get; init;
    }

    public required string OutputItemType
    {
        get; init;
    }

    public required string ReferenceOutputAssembly
    {
        get; init;
    }

    public required string Kind
    {
        get; init;
    }

    public IReadOnlyList<ProjectDefinition> ReferencedProjects { get; set; } = [];
}

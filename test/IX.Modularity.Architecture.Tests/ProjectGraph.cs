using System.Xml.Linq;

namespace IX.Modularity.Architecture.Tests;

internal sealed class ProjectGraph
{
    public static readonly string[] ValidRoles = ["Library", "Contracts", "Abstractions", "Adapter", "Integration", "Testing", "Analyzer", "SourceGenerator", "Test", "ArchitectureTest"];

    private ProjectGraph(IReadOnlyList<ProjectDefinition> projects, IReadOnlyList<string> solutionProjectPaths)
    {
        Projects = projects;
        SolutionProjectPaths = solutionProjectPaths;
    }

    public IReadOnlyList<ProjectDefinition> Projects
    {
        get;
    }

    public IReadOnlyList<string> SolutionProjectPaths
    {
        get;
    }

    public IEnumerable<ProjectDefinition> SourceProjects => Projects.Where(static project => project.RelativePath.StartsWith("src/", StringComparison.Ordinal));

    public static ProjectGraph Load(string repositoryRoot)
    {
        ProjectDefinition[] projects = [.. Directory.EnumerateFiles(repositoryRoot, "*.csproj", SearchOption.AllDirectories)
            .Where(static path => !IsBuildOutput(path))
            .Select(path => ProjectDefinition.Load(repositoryRoot, path))
            .OrderBy(static project => project.RelativePath, StringComparer.Ordinal)];
        XDocument solution = ProjectXmlDocumentLoader.Load(Path.Combine(repositoryRoot, "IX.Modularity.slnx"));
        string[] solutionProjectPaths = [.. solution.Descendants("Project")
            .Select(static project => project.Attribute("Path")?.Value)
            .OfType<string>()
            .Select(static path => path.Replace('\\', '/'))];

        foreach (ProjectDefinition project in projects)
        {
            project.References = project.ProjectReferenceIncludes
                .SelectMany(include => ResolveProjectReferences(repositoryRoot, project, include, projects))
                .DistinctBy(static referencedProject => referencedProject.FullPath, StringComparer.OrdinalIgnoreCase)
                .ToArray();
        }

        return new ProjectGraph(projects, solutionProjectPaths);
    }

    public static bool IsReferenceAllowed(string role, string referencedRole)
    {
        return role switch
        {
            "Library" or "Adapter" => string.Equals(referencedRole, "Library", StringComparison.Ordinal)
                || string.Equals(referencedRole, "Contracts", StringComparison.Ordinal)
                || string.Equals(referencedRole, "Abstractions", StringComparison.Ordinal),
            "Integration" => string.Equals(referencedRole, "Library", StringComparison.Ordinal)
                || string.Equals(referencedRole, "Contracts", StringComparison.Ordinal)
                || string.Equals(referencedRole, "Abstractions", StringComparison.Ordinal)
                || string.Equals(referencedRole, "Adapter", StringComparison.Ordinal),
            "Contracts" => string.Equals(referencedRole, "Contracts", StringComparison.Ordinal) || string.Equals(referencedRole, "Abstractions", StringComparison.Ordinal),
            "Abstractions" => string.Equals(referencedRole, "Abstractions", StringComparison.Ordinal),
            "Analyzer" or "SourceGenerator" => string.Equals(referencedRole, "Contracts", StringComparison.Ordinal) || string.Equals(referencedRole, "Abstractions", StringComparison.Ordinal) || string.Equals(referencedRole, "Analyzer", StringComparison.Ordinal) || string.Equals(referencedRole, "SourceGenerator", StringComparison.Ordinal),
            "Testing" => !string.Equals(referencedRole, "Test", StringComparison.Ordinal) && !string.Equals(referencedRole, "ArchitectureTest", StringComparison.Ordinal),
            "Test" or "ArchitectureTest" => true,
            _ => false,
        };
    }

    public static bool IsTestRole(string role)
    {
        return string.Equals(role, "Test", StringComparison.Ordinal) || string.Equals(role, "ArchitectureTest", StringComparison.Ordinal);
    }

    public static bool IsNeutralRole(string role)
    {
        return string.Equals(role, "Contracts", StringComparison.Ordinal) || string.Equals(role, "Abstractions", StringComparison.Ordinal);
    }

    public static bool IsCanonicalLocation(ProjectDefinition project)
    {
        return IsTestRole(project.Role)
        ? project.RelativePath.StartsWith("test/", StringComparison.Ordinal)
        : project.RelativePath.StartsWith("src/", StringComparison.Ordinal);
    }

    public static string NamePatternFor(string role)
    {
        return role switch
        {
            "Library" => "^IX\\.Modularity\\.[^.]+$",
            "Contracts" => ".+\\.Contracts$",
            "Abstractions" => ".+\\.Abstractions$",
            "Adapter" => ".+\\.Adapters\\..+",
            "Integration" => ".+\\.Integrations\\..+",
            "Testing" => ".+\\.Testing$",
            "Analyzer" => ".+\\.Analyzers$",
            "SourceGenerator" => ".+\\.Generators$",
            _ => ".+\\.Tests$",
        };
    }

    public bool HasCycle()
    {
        var visited = new HashSet<ProjectDefinition>();
        var visiting = new HashSet<ProjectDefinition>();

        return Projects.Any(HasCycle);

        bool HasCycle(ProjectDefinition project)
        {
            if (visited.Contains(project))
            {
                return false;
            }

            if (!visiting.Add(project))
            {
                return true;
            }

            bool hasCycle = project.References.Any(HasCycle);
            _ = visiting.Remove(project);
            _ = visited.Add(project);
            return hasCycle;
        }
    }

    private static bool IsBuildOutput(string path)
    {
        return path.Contains($"{Path.DirectorySeparatorChar}artifacts{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
        || path.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
        || path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal);
    }

    private static ProjectDefinition[] ResolveProjectReferences(string repositoryRoot, ProjectDefinition project, string include, IEnumerable<ProjectDefinition> candidates)
    {
        string absolutePattern = Path.GetFullPath(Path.Combine(project.DirectoryPath, include)).Replace('\\', '/');
        if (!IsRepositoryPath(repositoryRoot, absolutePattern))
        {
            throw new InvalidOperationException($"Project reference '{include}' from '{project.RelativePath}' resolves outside the repository.");
        }

        ProjectDefinition[] resolvedProjects = [.. candidates.Where(candidate => GlobMatches(absolutePattern, candidate.FullPath.Replace('\\', '/')))];
        return resolvedProjects.Length == 0 && !include.Contains('*', StringComparison.Ordinal)
            ? throw new InvalidOperationException($"Project reference '{include}' from '{project.RelativePath}' does not resolve to a repository project.")
            : resolvedProjects;
    }

    private static bool IsRepositoryPath(string repositoryRoot, string path)
    {
        string relativePath = Path.GetRelativePath(repositoryRoot, path);
        return !Path.IsPathRooted(relativePath)
            && !string.Equals(relativePath, "..", StringComparison.Ordinal)
            && !relativePath.StartsWith($"..{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
            && !relativePath.StartsWith($"..{Path.AltDirectorySeparatorChar}", StringComparison.Ordinal);
    }

    private static bool GlobMatches(string pattern, string path)
    {
        string[] patternSegments = pattern.Split('/');
        string[] pathSegments = path.Split('/');
        return GlobMatches(patternSegments, 0, pathSegments, 0);
    }

    private static bool GlobMatches(IReadOnlyList<string> pattern, int patternIndex, IReadOnlyList<string> path, int pathIndex)
    {
        return patternIndex == pattern.Count
            ? pathIndex == path.Count
            : GlobMatchesRemaining(pattern, patternIndex, path, pathIndex);
    }

    private static bool GlobMatchesRemaining(IReadOnlyList<string> pattern, int patternIndex, IReadOnlyList<string> path, int pathIndex)
    {
        return string.Equals(pattern[patternIndex], "**", StringComparison.Ordinal)
            ? Enumerable.Range(pathIndex, path.Count - pathIndex + 1).Any(nextPathIndex => GlobMatches(pattern, patternIndex + 1, path, nextPathIndex))
            : pathIndex < path.Count && SegmentMatches(pattern[patternIndex], path[pathIndex]) && GlobMatches(pattern, patternIndex + 1, path, pathIndex + 1);
    }

    private static bool SegmentMatches(string pattern, string value)
    {
        int patternIndex = 0;
        int valueIndex = 0;
        while (patternIndex < pattern.Length)
        {
            if (pattern[patternIndex] == '*')
            {
                return Enumerable.Range(valueIndex, value.Length - valueIndex + 1).Any(nextValueIndex => SegmentMatches(pattern[(patternIndex + 1)..], value[nextValueIndex..]));
            }

            if (valueIndex == value.Length || pattern[patternIndex] != value[valueIndex])
            {
                return false;
            }

            patternIndex++;
            valueIndex++;
        }

        return valueIndex == value.Length;
    }
}

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
        EnsureRepositoryPathIsSafe(repositoryRoot, repositoryRoot, "repository root");
        ProjectDefinition[] projects = [.. EnumerateProjectFiles(repositoryRoot)
            .Select(path => ProjectDefinition.Load(repositoryRoot, path))
            .OrderBy(static project => project.RelativePath, StringComparer.Ordinal)];
        string solutionPath = Path.Combine(repositoryRoot, "IX.Modularity.slnx");
        EnsureRepositoryPathIsSafe(repositoryRoot, solutionPath, "solution file");
        XDocument solution = ProjectXmlDocumentLoader.Load(solutionPath);
        string[] solutionProjectPaths = [.. solution.Descendants("Project")
            .Select(static project => project.Attribute("Path")?.Value)
            .OfType<string>()
            .Select(static path => path.Replace('\\', '/'))];

        foreach (ProjectDefinition project in projects)
        {
            foreach (ProjectReferenceDefinition reference in project.ProjectReferences)
            {
                reference.ReferencedProjects = ResolveProjectReferences(repositoryRoot, project, reference.Include, projects);
            }

            project.References = project.ProjectReferences
                .Where(static reference => !IsCompilerToolReference(reference))
                .SelectMany(static reference => reference.ReferencedProjects)
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

    public static bool IsCompilerToolReference(ProjectReferenceDefinition reference)
    {
        return reference.Kind.Length > 0
            || string.Equals(reference.OutputItemType, "Analyzer", StringComparison.Ordinal)
            || string.Equals(reference.ReferenceOutputAssembly, "false", StringComparison.OrdinalIgnoreCase);
    }

    public static bool HasValidCompilerToolMetadata(ProjectReferenceDefinition reference)
    {
        return string.Equals(reference.Kind, "CompilerTool", StringComparison.Ordinal)
            && string.Equals(reference.OutputItemType, "Analyzer", StringComparison.Ordinal)
            && string.Equals(reference.ReferenceOutputAssembly, "false", StringComparison.OrdinalIgnoreCase);
    }

    public static bool IsCompilerToolTargetRole(string role)
    {
        return string.Equals(role, "Analyzer", StringComparison.Ordinal)
            || string.Equals(role, "SourceGenerator", StringComparison.Ordinal);
    }

    public static bool IsTestRole(string role)
    {
        return string.Equals(role, "Test", StringComparison.Ordinal) || string.Equals(role, "ArchitectureTest", StringComparison.Ordinal);
    }

    public static bool IsNeutralRole(string role)
    {
        return string.Equals(role, "Contracts", StringComparison.Ordinal) || string.Equals(role, "Abstractions", StringComparison.Ordinal);
    }

    public static bool IsPackageReferenceAllowed(string relativePath, string role, PackageReferenceDefinition packageReference)
    {
        return !string.Equals(packageReference.Id, "FluentResults", StringComparison.Ordinal)
            ? !IsNeutralRole(role)
            : IsFluentResultsAllowedForProductionRole(role)
                || IsFluentResultsAnalyzerTestFixture(relativePath, role, packageReference.PrivateAssets);
    }

    public static bool IsFluentResultsAllowedForProductionRole(string role)
    {
        return string.Equals(role, "Library", StringComparison.Ordinal)
            || string.Equals(role, "Contracts", StringComparison.Ordinal)
            || string.Equals(role, "Abstractions", StringComparison.Ordinal)
            || string.Equals(role, "Adapter", StringComparison.Ordinal)
            || string.Equals(role, "Integration", StringComparison.Ordinal);
    }

    public static bool IsFluentResultsAnalyzerTestFixture(string relativePath, string role, string privateAssets)
    {
        return string.Equals(relativePath, "test/IX.Modularity.Analyzers.Tests/IX.Modularity.Analyzers.Tests.csproj", StringComparison.Ordinal)
            && string.Equals(role, "Test", StringComparison.Ordinal)
            && string.Equals(privateAssets, "all", StringComparison.OrdinalIgnoreCase);
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

    private static IEnumerable<string> EnumerateProjectFiles(string repositoryRoot)
    {
        Stack<string> directories = new();
        directories.Push(repositoryRoot);

        while (directories.Count > 0)
        {
            string directory = directories.Pop();
            IEnumerable<string> entries;
            try
            {
                entries = Directory.EnumerateFileSystemEntries(directory).ToArray();
            }
            catch (IOException exception)
            {
                throw new InvalidOperationException($"Unable to enumerate repository directory '{directory}'.", exception);
            }
            catch (UnauthorizedAccessException exception)
            {
                throw new InvalidOperationException($"Unable to enumerate repository directory '{directory}'.", exception);
            }

            foreach (string entry in entries)
            {
                FileAttributes attributes = GetFileAttributesOrReject(repositoryRoot, entry, "project discovery");
                if (attributes.HasFlag(FileAttributes.ReparsePoint))
                {
                    if (attributes.HasFlag(FileAttributes.Directory) || entry.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase))
                    {
                        throw new InvalidOperationException($"Project discovery rejects reparse point '{entry}'.");
                    }

                    continue;
                }

                if (attributes.HasFlag(FileAttributes.Directory))
                {
                    if (!IsBuildOutput(entry))
                    {
                        directories.Push(entry);
                    }

                    continue;
                }

                if (entry.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase) && !IsBuildOutput(entry))
                {
                    yield return entry;
                }
            }
        }
    }

    private static ProjectDefinition[] ResolveProjectReferences(string repositoryRoot, ProjectDefinition project, string include, IEnumerable<ProjectDefinition> candidates)
    {
        string absolutePattern = Path.GetFullPath(Path.Combine(project.DirectoryPath, include)).Replace('\\', '/');
        if (!IsRepositoryPath(repositoryRoot, absolutePattern))
        {
            throw new InvalidOperationException($"Project reference '{include}' from '{project.RelativePath}' resolves outside the repository.");
        }

        EnsureRepositoryPatternPrefixIsSafe(repositoryRoot, absolutePattern, $"project reference '{include}' from '{project.RelativePath}'");
        ProjectDefinition[] resolvedProjects = [.. candidates.Where(candidate => GlobMatches(absolutePattern, candidate.FullPath.Replace('\\', '/')))];
        foreach (ProjectDefinition resolvedProject in resolvedProjects)
        {
            EnsureRepositoryPathIsSafe(repositoryRoot, resolvedProject.FullPath, $"project reference '{include}' from '{project.RelativePath}'");
        }

        if (!include.Contains('*', StringComparison.Ordinal))
        {
            EnsureRepositoryPathIsSafe(repositoryRoot, absolutePattern, $"project reference '{include}' from '{project.RelativePath}'");
        }

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

    private static void EnsureRepositoryPathIsSafe(string repositoryRoot, string path, string description)
    {
        string normalizedRoot = Path.GetFullPath(repositoryRoot);
        string normalizedPath = Path.GetFullPath(path);
        if (!IsRepositoryPath(normalizedRoot, normalizedPath) && !string.Equals(normalizedRoot, normalizedPath, StringComparison.Ordinal))
        {
            throw new InvalidOperationException($"{description} resolves outside the repository.");
        }

        FileAttributes rootAttributes = GetFileAttributesOrReject(normalizedRoot, normalizedRoot, description);
        if (rootAttributes.HasFlag(FileAttributes.ReparsePoint))
        {
            throw new InvalidOperationException($"{description} repository root is a reparse point.");
        }
        string relativePath = Path.GetRelativePath(normalizedRoot, normalizedPath);
        if (string.Equals(relativePath, ".", StringComparison.Ordinal))
        {
            return;
        }

        string currentPath = normalizedRoot;
        foreach (string segment in relativePath.Split([Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar], StringSplitOptions.RemoveEmptyEntries))
        {
            currentPath = Path.Combine(currentPath, segment);
            FileAttributes attributes = GetFileAttributesOrReject(normalizedRoot, currentPath, description);
            if (attributes.HasFlag(FileAttributes.ReparsePoint))
            {
                throw new InvalidOperationException($"{description} traverses reparse point '{currentPath}'.");
            }
        }
    }

    private static void EnsureRepositoryPatternPrefixIsSafe(string repositoryRoot, string pattern, string description)
    {
        string normalizedRoot = Path.GetFullPath(repositoryRoot);
        string relativePattern = Path.GetRelativePath(normalizedRoot, Path.GetFullPath(pattern));
        string currentPath = normalizedRoot;
        foreach (string segment in relativePattern.Split([Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar], StringSplitOptions.RemoveEmptyEntries))
        {
            if (segment.Contains('*', StringComparison.Ordinal))
            {
                return;
            }

            currentPath = Path.Combine(currentPath, segment);
            FileAttributes attributes = GetFileAttributesOrReject(normalizedRoot, currentPath, description);
            if (attributes.HasFlag(FileAttributes.ReparsePoint))
            {
                throw new InvalidOperationException($"{description} traverses reparse point '{currentPath}'.");
            }
        }
    }

    private static FileAttributes GetFileAttributesOrReject(string repositoryRoot, string path, string description)
    {
        try
        {
            return File.GetAttributes(path);
        }
        catch (IOException exception)
        {
            throw new InvalidOperationException($"Unable to inspect {description} path '{path}' within repository '{repositoryRoot}'.", exception);
        }
        catch (UnauthorizedAccessException exception)
        {
            throw new InvalidOperationException($"Unable to inspect {description} path '{path}' within repository '{repositoryRoot}'.", exception);
        }
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

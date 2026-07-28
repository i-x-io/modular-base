using Nuke.Common.ProjectModel;

namespace ModularBase.Build.Repository;

internal static class ProjectDiscovery
{
    public static IReadOnlyList<PackageProject> FindPackageProjects(Solution solution)
    {
        ArgumentNullException.ThrowIfNull(solution);
        ProjectMetadata[] projects = [.. solution.AllProjects.Select(ReadMetadata)];
        return SelectPackageProjects(projects);
    }

    internal static IReadOnlyList<PackageProject> SelectPackageProjects(
        IEnumerable<ProjectMetadata> projects)
    {
        ArgumentNullException.ThrowIfNull(projects);
        ProjectMetadata[] packable = [.. projects.Where(project => project.IsPackable)];
        if (packable.Length == 0)
        {
            throw new InvalidOperationException("The solution must contain at least one packable project.");
        }

        string[] duplicateIds = [.. packable
            .GroupBy(project => project.PackageId, StringComparer.OrdinalIgnoreCase)
            .Where(group => group.Skip(1).Any())
            .Select(group => group.Key)];
        if (duplicateIds.Length != 0)
        {
            throw new InvalidOperationException(
                $"Package IDs must be unique: {string.Join(", ", duplicateIds)}.");
        }

        string[] duplicatePaths = [.. packable
            .GroupBy(project => Path.GetFullPath(project.ProjectFile), StringComparer.OrdinalIgnoreCase)
            .Where(group => group.Skip(1).Any())
            .Select(group => group.Key)];
        return duplicatePaths.Length != 0
            ? throw new InvalidOperationException(
                $"Package project paths must be unique: {string.Join(", ", duplicatePaths)}.")
            : [.. packable
            .Select(CreatePackageProject)
            .OrderBy(project => project.PackageId, StringComparer.Ordinal)];
    }

    private static PackageProject CreatePackageProject(ProjectMetadata project)
    {
        Require(project.PackageId, nameof(project.PackageId), project.ProjectFile);
        Require(project.AssemblyName, nameof(project.AssemblyName), project.ProjectFile);
        Require(project.RepositoryUrl, nameof(project.RepositoryUrl), project.ProjectFile);
        Require(project.TagPrefix, nameof(project.TagPrefix), project.ProjectFile);
        string frameworks = !string.IsNullOrWhiteSpace(project.TargetFrameworks)
            ? project.TargetFrameworks
            : project.TargetFramework;
        string[] targetFrameworks = [.. frameworks.Split(
            ';',
            StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)];
        return targetFrameworks.Length == 0
            ? throw new InvalidOperationException(
                string.Create(global::System.Globalization.CultureInfo.InvariantCulture, $"Packable project '{project.ProjectFile}' must define TargetFramework or TargetFrameworks."))
            : new(
            project.ProjectFile,
            project.PackageId,
            project.AssemblyName,
            RepositoryIdentity.NormalizeRepositoryUrl(project.RepositoryUrl),
            project.TagPrefix,
            targetFrameworks,
            [.. project.RuntimeDependencies.Order(StringComparer.OrdinalIgnoreCase)]);
    }

    private static ProjectMetadata ReadMetadata(Project project)
    {
        string[] packageReferences = [.. project.GetItems("PackageReference")];
        string[] privateAssets = [.. project.GetItemMetadata("PackageReference", "PrivateAssets")];
        string[] excludeAssets = [.. project.GetItemMetadata("PackageReference", "ExcludeAssets")];
        string[] runtimeDependencies = [.. packageReferences
            .Where((_, index) => !ContainsAll(privateAssets.ElementAtOrDefault(index))
                && !ContainsAll(excludeAssets.ElementAtOrDefault(index)))];

        return new(
            project.Path,
            project.GetProperty<bool>("IsPackable"),
            project.GetProperty("PackageId"),
            project.GetProperty("AssemblyName"),
            project.GetProperty("RepositoryUrl"),
            project.GetProperty("MinVerTagPrefix"),
            project.GetProperty("TargetFrameworks"),
            project.GetProperty("TargetFramework"),
            runtimeDependencies);
    }

    private static bool ContainsAll(string? assets)
    {
        return assets?.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Contains("all", StringComparer.OrdinalIgnoreCase) == true;
    }

    private static void Require(string value, string propertyName, string projectFile)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException(
                $"Packable project '{projectFile}' must define the evaluated MSBuild property '{propertyName}'.");
        }
    }
}

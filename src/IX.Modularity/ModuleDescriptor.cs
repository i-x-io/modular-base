using System.Collections.ObjectModel;
using System.Text.RegularExpressions;

namespace IX.Modularity;

/// <summary>
/// Describes a module and the modules that must be registered before it.
/// </summary>
public sealed partial class ModuleDescriptor
{
    /// <summary>
    /// Initializes a module descriptor.
    /// </summary>
    /// <param name="id">The stable module identifier.</param>
    /// <param name="displayName">The human-readable module name.</param>
    /// <param name="version">A Semantic Versioning 2.0.0 version.</param>
    /// <param name="dependencies">The modules that must be registered first.</param>
    /// <param name="description">An optional human-readable description.</param>
    public ModuleDescriptor(
        ModuleId id,
        string displayName,
        string version,
        IEnumerable<ModuleId> dependencies,
        string? description = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(displayName);
        ArgumentException.ThrowIfNullOrWhiteSpace(version);
        ArgumentNullException.ThrowIfNull(dependencies);
        _ = id.Value;

        if (!SemanticVersionPattern.IsMatch(version))
        {
            throw new ArgumentException("The module version must be a valid Semantic Versioning 2.0.0 value.", nameof(version));
        }

        List<ModuleId> dependencyList = [];
        HashSet<ModuleId> uniqueDependencies = [];

        foreach (ModuleId dependency in dependencies)
        {
            _ = dependency.Value;

            if (dependency == id)
            {
                throw new ArgumentException("A module cannot depend on itself.", nameof(dependencies));
            }

            if (!uniqueDependencies.Add(dependency))
            {
                throw new ArgumentException($"The dependency '{dependency}' is declared more than once.", nameof(dependencies));
            }

            dependencyList.Add(dependency);
        }

        Id = id;
        DisplayName = displayName.Trim();
        Version = version;
        Dependencies = new ReadOnlyCollection<ModuleId>(dependencyList);
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
    }

    /// <summary>
    /// Gets the stable module identifier.
    /// </summary>
    public ModuleId Id
    {
        get;
    }

    /// <summary>
    /// Gets the human-readable module name.
    /// </summary>
    public string DisplayName
    {
        get;
    }

    /// <summary>
    /// Gets the Semantic Versioning 2.0.0 version.
    /// </summary>
    public string Version
    {
        get;
    }

    /// <summary>
    /// Gets the modules that must be registered before this module.
    /// </summary>
    public IReadOnlyList<ModuleId> Dependencies
    {
        get;
    }

    /// <summary>
    /// Gets the optional human-readable description.
    /// </summary>
    public string? Description
    {
        get;
    }

    [GeneratedRegex(
        "^(0|[1-9][0-9]*)\\.(0|[1-9][0-9]*)\\.(0|[1-9][0-9]*)(?:-((?:0|[1-9][0-9]*|[0-9]*[A-Za-z-][0-9A-Za-z-]*)(?:\\.(?:0|[1-9][0-9]*|[0-9]*[A-Za-z-][0-9A-Za-z-]*))*))?(?:\\+([0-9A-Za-z-]+(?:\\.[0-9A-Za-z-]+)*))?$",
        RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture,
        matchTimeoutMilliseconds: 1000)]
    private static partial Regex SemanticVersionPattern
    {
        get;
    }
}

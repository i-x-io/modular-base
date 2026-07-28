using Microsoft.Extensions.DependencyInjection;

namespace IX.Modularity;

/// <summary>
/// Defines a module that is composed explicitly at compile time.
/// </summary>
public interface IModule
{
    /// <summary>
    /// Gets the immutable module descriptor.
    /// </summary>
    static abstract ModuleDescriptor Descriptor
    {
        get;
    }

    /// <summary>
    /// Adds the module's services to the application service collection.
    /// </summary>
    /// <param name="services">The application service collection.</param>
    static abstract void ConfigureServices(IServiceCollection services);
}

using System.Runtime.CompilerServices;
using Microsoft.Extensions.DependencyInjection;

namespace IX.Modularity;

/// <summary>
/// Provides explicit, reflection-free module registration.
/// </summary>
public static class ModularityServiceCollectionExtensions
{
    private static readonly ConditionalWeakTable<IServiceCollection, ModuleRegistrationState> s_registrationStates = [];

    /// <summary>
    /// Adds a module after verifying its declared dependencies.
    /// </summary>
    /// <typeparam name="TModule">The statically known module type.</typeparam>
    /// <param name="services">The application service collection.</param>
    /// <returns>The original service collection.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="services"/> is null.</exception>
    /// <exception cref="InvalidOperationException">
    /// The module is already registered, is recursively registering, or has a dependency that has not been registered.
    /// </exception>
    public static IServiceCollection AddModule<TModule>(this IServiceCollection services)
        where TModule : IModule
    {
        ArgumentNullException.ThrowIfNull(services);

        ModuleDescriptor descriptor = TModule.Descriptor
            ?? throw new InvalidOperationException("A module descriptor cannot be null.");
        ModuleRegistrationState state = s_registrationStates.GetValue(services, static _ => new ModuleRegistrationState());

        lock (state.SyncRoot)
        {
            if (state.Registered.Contains(descriptor.Id))
            {
                throw new InvalidOperationException($"Module '{descriptor.Id}' is already registered.");
            }

            if (!state.InProgress.Add(descriptor.Id))
            {
                throw new InvalidOperationException($"Module '{descriptor.Id}' is already being registered.");
            }

            try
            {
                ModuleId missingDependency = descriptor.Dependencies.FirstOrDefault(
                    dependency => !state.Registered.Contains(dependency));
                if (missingDependency != default)
                {
                    throw new InvalidOperationException(
                        $"Module '{descriptor.Id}' requires module '{missingDependency}' to be registered first.");
                }

                TModule.ConfigureServices(services);
                _ = services.AddSingleton(descriptor);
                _ = state.Registered.Add(descriptor.Id);
            }
            finally
            {
                _ = state.InProgress.Remove(descriptor.Id);
            }
        }

        return services;
    }

    private sealed class ModuleRegistrationState
    {
        public object SyncRoot { get; } = new();

        public HashSet<ModuleId> Registered { get; } = [];

        public HashSet<ModuleId> InProgress { get; } = [];
    }
}

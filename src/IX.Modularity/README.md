# IX.Modularity

`IX.Modularity` provides small, reflection-free contracts for composing modular .NET applications explicitly.

Modules expose immutable metadata and a static service-registration method. The composition root registers modules in dependency order:

```csharp
IServiceCollection services = new ServiceCollection();

services
    .AddModule<FoundationModule>()
    .AddModule<PaymentsModule>();
```

The package deliberately does not scan assemblies, activate module objects, dispatch messages, or own application lifecycle. Hosts retain control of composition and runtime behavior.

## GitHub Packages source

GitHub's NuGet registry requires authentication even for public packages. Create a classic personal access token with `read:packages`, keep it outside the repository, and configure the `i-x-io` source for the consuming environment before installing this package.

See the repository's development and package documentation for the reviewed setup and security guidance.

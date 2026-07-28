using Microsoft.Extensions.DependencyInjection;

namespace IX.Modularity.Tests;

public sealed class ModuleRegistrationTests
{
    [Fact]
    public void AddModule_registers_services_and_descriptor()
    {
        ServiceCollection services = [];

        IServiceCollection returnedServices = services.AddModule<FoundationModule>();

        Assert.Same(services, returnedServices);

        using ServiceProvider provider = services.BuildServiceProvider();
        Assert.Equal("foundation", provider.GetRequiredService<FoundationService>().Name);
        Assert.Equal(FoundationModule.Descriptor, provider.GetRequiredService<ModuleDescriptor>());
    }

    [Fact]
    public void AddModule_accepts_dependencies_in_registration_order()
    {
        ServiceCollection services = [];

        _ = services.AddModule<FoundationModule>().AddModule<PaymentsModule>();

        using ServiceProvider provider = services.BuildServiceProvider();
        Assert.Equal(
            [FoundationModule.Descriptor, PaymentsModule.Descriptor],
            provider.GetServices<ModuleDescriptor>());
        Assert.Equal("payments", provider.GetRequiredService<PaymentsService>().Name);
    }

    [Fact]
    public void AddModule_rejects_a_missing_dependency()
    {
        ServiceCollection services = [];

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(
            services.AddModule<PaymentsModule>);

        Assert.Contains("foundation", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void AddModule_rejects_duplicate_registration()
    {
        ServiceCollection services = [];
        _ = services.AddModule<FoundationModule>();

        _ = Assert.Throws<InvalidOperationException>(services.AddModule<FoundationModule>);
    }

    private sealed class FoundationService(string name)
    {
        public string Name { get; } = name;
    }

    private sealed class PaymentsService(string name)
    {
        public string Name { get; } = name;
    }

    private readonly struct FoundationModule : IModule
    {
        public static ModuleDescriptor Descriptor
        {
            get;
        } = new(
            ModuleId.Parse("foundation"),
            "Foundation",
            "1.0.0",
            []);

        public static void ConfigureServices(IServiceCollection services)
        {
            _ = services.AddSingleton(new FoundationService("foundation"));
        }
    }

    private readonly struct PaymentsModule : IModule
    {
        public static ModuleDescriptor Descriptor
        {
            get;
        } = new(
            ModuleId.Parse("payments"),
            "Payments",
            "1.0.0",
            [ModuleId.Parse("foundation")]);

        public static void ConfigureServices(IServiceCollection services)
        {
            _ = services.AddSingleton(new PaymentsService("payments"));
        }
    }
}

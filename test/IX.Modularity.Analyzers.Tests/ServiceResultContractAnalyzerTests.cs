using System.Collections.Immutable;
using System.Globalization;
using AwesomeAssertions;
using Microsoft.CodeAnalysis;

namespace IX.Modularity.Analyzers.Tests;

#pragma warning disable IDE0022, MA0136, MA0040, MA0006, xUnit1051 // Source snippets deliberately preserve analyzer-facing shape; cancellation is explicitly tested below.
public sealed class ServiceResultContractAnalyzerTests
{
    [Xunit.Fact]
    public async Task Public_service_methods_accept_only_the_six_result_shapes()
    {
        const string source = """
            using System.Threading.Tasks;
            using FluentResults;
            public sealed class OrdersService
            {
                public Result One() => Result.Ok();
                public Result<int> Two() => Result.Ok(1);
                public Task<Result> Three() => Task.FromResult(Result.Ok());
                public Task<Result<int>> Four() => Task.FromResult(Result.Ok(1));
                public ValueTask<Result> Five() => ValueTask.FromResult(Result.Ok());
                public ValueTask<Result<int>> Six() => ValueTask.FromResult(Result.Ok(1));
                public string BadString() => string.Empty;
                public Task BadTask() => Task.CompletedTask;
                public ValueTask<int> BadValueTask() => ValueTask.FromResult(1);
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzeAsync(source).ConfigureAwait(true);

        AssertRuleLocations(diagnostics, "IXM3001", "BadString", "BadTask", "BadValueTask");
    }

    [Xunit.Fact]
    public async Task Aliases_inherited_services_and_interface_contracts_are_resolved_by_symbol_identity()
    {
        const string source = """
            using FluentResults;
            using Outcome = FluentResults.Result;
            public interface IBaseService { Outcome Save(); string Broken(); }
            public interface IOrdersService : IBaseService { Result<int> Create(); }
            public sealed class OrdersService : IOrdersService
            {
                public Outcome Save() => Outcome.Ok();
                public string Broken() => string.Empty;
                public Result<int> Create() => Result.Ok(1);
                public int ConcreteOnly() => 0;
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzeAsync(source).ConfigureAwait(true);

        AssertRuleLocations(diagnostics, "IXM3001", "Broken", "ConcreteOnly");
        _ = RuleDiagnostics(diagnostics, "IXM3001").Count(diagnostic => LocationText(diagnostic) == "Broken").Should().Be(1);
    }

    [Xunit.Fact]
    public async Task Unsupported_shapes_and_interface_ownership_variants_are_reported_once_at_method_identifiers()
    {
        const string source = """
            using System;
            using System.Collections.Generic;
            using System.Threading;
            using System.Threading.Tasks;
            using FluentResults;
            public interface IRootService { string Inherited(); }
            public interface IChildService : IRootService { string Owned(); }
            public sealed class ChildService : IChildService
            {
                public string Inherited() => "";
                public string Owned() => "";
                public bool Flag() => false;
                public int? Nullable() => null;
                public (int, int) Tuple() => default;
                public Task<string> RawTask() => Task.FromResult("");
                public ValueTask<string> RawValueTask() => ValueTask.FromResult("");
                public IAsyncEnumerable<Result> Stream() => default!;
                public void Callback(Action callback) => callback();
                protected string Protected() => "";
            }
            public sealed class ExplicitService : IChildService
            {
                string IRootService.Inherited() => "";
                string IChildService.Owned() => "";
            }
            public sealed class GenericService<T> { public string Generic(T value) => ""; }
            public sealed class Ordinary { public string Excluded() => ""; }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzeAsync(source).ConfigureAwait(true);

        AssertRuleLocations(diagnostics, "IXM3001", "Inherited", "Owned", "Flag", "Nullable", "Tuple", "RawTask", "RawValueTask", "Stream", "Callback", "Protected", "Generic");
    }

    [Xunit.Fact]
    public void Result_policy_descriptors_have_complete_public_metadata()
    {
        ImmutableArray<DiagnosticDescriptor> descriptors = new DocumentationAndRecordAnalyzer().SupportedDiagnostics;

        _ = descriptors.Where(descriptor => descriptor.Id.StartsWith("IXM300", StringComparison.Ordinal)).Should().HaveCount(3).And.AllSatisfy(descriptor =>
        {
            _ = descriptor.Title.ToString(CultureInfo.InvariantCulture).Should().NotBeNullOrWhiteSpace();
            _ = descriptor.MessageFormat.ToString(CultureInfo.InvariantCulture).Should().NotBeNullOrWhiteSpace();
            _ = descriptor.Description.ToString(CultureInfo.InvariantCulture).Should().NotBeNullOrWhiteSpace();
            _ = descriptor.HelpLinkUri.Should().NotBeNullOrWhiteSpace();
            _ = descriptor.DefaultSeverity.Should().Be(DiagnosticSeverity.Warning);
            _ = descriptor.IsEnabledByDefault.Should().BeTrue();
        });
    }

    [Xunit.Fact]
    public async Task Private_service_interface_helpers_are_excluded_but_public_default_contract_methods_are_checked()
    {
        const string source = """
            public interface IOrdersService
            {
                private static string StaticHelper() => "";
                private string InstanceHelper() => "";
                public string PublicBad() => "";
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzeAsync(source).ConfigureAwait(true);

        AssertRuleLocations(diagnostics, "IXM3001", "PublicBad");
    }

    [Xunit.Fact]
    public async Task Non_public_generated_and_malformed_service_members_do_not_produce_spurious_contract_diagnostics()
    {
        const string source = """
            using FluentResults;
            public partial class OrdersService { public string UserBad() => ""; private string PrivateBad() => ""; }
            public partial class OrdersService { public Result Good() => Result.Ok(); }
            public interface IBrokenService { Result Missing( ; }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzeAsync(
            [(source, "User.cs"), ("using FluentResults; public partial class GeneratedService { public string GeneratedBad() => \"\"; }", "Generated.g.cs")]).ConfigureAwait(true);

        AssertRuleLocations(diagnostics, "IXM3001", "UserBad");
    }

    [Xunit.Fact]
    public async Task Missing_fluentresults_metadata_is_ignored_and_analysis_is_safe_under_concurrency_and_cancellation()
    {
        const string source = "public sealed class OrdersService { public FluentResults.Result Save() => null!; }";

        ImmutableArray<Diagnostic> missingReference = await AnalyzeAsync(source, includeFluentResults: false).ConfigureAwait(true);
        _ = RuleDiagnostics(missingReference, "IXM3001").Should().BeEmpty();

        ImmutableArray<Diagnostic>[] concurrent = await Task.WhenAll(Enumerable.Range(0, 8).Select(_ => AnalyzeAsync(source))).ConfigureAwait(true);
        _ = concurrent.Should().AllSatisfy(result => RuleDiagnostics(result, "IXM3001").Should().BeEmpty());

        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync().ConfigureAwait(true);
        Func<Task<ImmutableArray<Diagnostic>>> action = () => AnalyzeAsync(source, cancellationToken: cancellation.Token);
        _ = await action.Should().ThrowAsync<OperationCanceledException>().ConfigureAwait(true);
    }

    private static async Task<ImmutableArray<Diagnostic>> AnalyzeAsync(
        string source,
        bool includeFluentResults = true,
        CancellationToken cancellationToken = default)
    {
        return await AnalyzerTestHarness.AnalyzeAsync(new DocumentationAndRecordAnalyzer(), source, includeFluentResults: includeFluentResults, cancellationToken: cancellationToken).ConfigureAwait(false);
    }

    private static async Task<ImmutableArray<Diagnostic>> AnalyzeAsync(IEnumerable<(string Source, string Path)> sources)
    {
        return await AnalyzerTestHarness.AnalyzeAsync(new DocumentationAndRecordAnalyzer(), sources, includeFluentResults: true, cancellationToken: Xunit.TestContext.Current.CancellationToken).ConfigureAwait(false);
    }

    private static ImmutableArray<Diagnostic> RuleDiagnostics(ImmutableArray<Diagnostic> diagnostics, string id) => [.. diagnostics.Where(diagnostic => diagnostic.Id == id && !diagnostic.IsSuppressed)];

    private static void AssertRuleLocations(ImmutableArray<Diagnostic> diagnostics, string id, params string[] expected)
    {
        _ = RuleDiagnostics(diagnostics, id).Select(LocationText).Should().BeEquivalentTo(expected);
    }

    private static string LocationText(Diagnostic diagnostic)
    {
        return diagnostic.Location.SourceTree!.GetText().ToString(diagnostic.Location.SourceSpan);
    }
}
#pragma warning restore IDE0022, MA0136, MA0040, MA0006, xUnit1051

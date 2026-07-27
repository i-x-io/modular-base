using System.Collections.Immutable;
using System.Globalization;
using AwesomeAssertions;
using Microsoft.CodeAnalysis;

namespace IX.Modularity.Analyzers.Tests;

public sealed class ModularityDocumentationAnalyzerTests
{
    [Xunit.Fact]
    public async Task Data_suffixes_and_contracts_role_require_data_documentation()
    {
        string source = JoinLines(
            "public sealed class CustomerDto { }",
            "public sealed class CustomerModel { }",
            "public sealed class CustomerRequest { }",
            "public sealed class CustomerResponse { }",
            "public sealed class CustomerCommand { }",
            "public sealed class CustomerQuery { }",
            "public sealed class CustomerEvent { }",
            "public sealed class CustomerMessage { }",
            "public sealed class CustomerPayload { }",
            "public sealed class CustomerContract { }",
            "public sealed class CustomerConfiguration { }",
            "public sealed class CustomerError { }",
            "public sealed class CustomerNotification { }",
            "public sealed class CustomerOptions { }",
            "public sealed class CustomerResult { }",
            "public sealed class CustomerData { }",
            "public sealed class Customer { }");

        ImmutableArray<Diagnostic> diagnostics = await AnalyzeAsync(source, cancellationToken: Xunit.TestContext.Current.CancellationToken).ConfigureAwait(true);

        _ = diagnostics.Count(diagnostic => string.Equals(diagnostic.Id, "IXM1001", StringComparison.Ordinal)).Should().Be(12);
        _ = diagnostics.Select(diagnostic => diagnostic.Location.SourceTree?.GetText(Xunit.TestContext.Current.CancellationToken).ToString().Substring(diagnostic.Location.SourceSpan.Start, diagnostic.Location.SourceSpan.Length))
            .Should().NotContain("Customer");

        ImmutableArray<Diagnostic> contractsDiagnostics = await AnalyzeAsync("public sealed class Customer { }", projectRole: "Contracts", cancellationToken: Xunit.TestContext.Current.CancellationToken).ConfigureAwait(true);
        _ = contractsDiagnostics.Should().Contain(diagnostic => diagnostic.Id == "IXM1001");
    }

    [Xunit.Fact]
    public async Task Interface_types_and_members_have_separate_documentation_diagnostics()
    {
        string source = JoinLines(
            "public interface IOrders<T> : IDisposable",
            "{",
            "    T Create<TRequest>(TRequest request);",
            "    string Name { get; }",
            "    string this[int index] { get; }",
            "    event EventHandler Changed;",
            "}");

        ImmutableArray<Diagnostic> diagnostics = await AnalyzeAsync(source, cancellationToken: Xunit.TestContext.Current.CancellationToken).ConfigureAwait(true);

        _ = diagnostics.Should().Contain(diagnostic => diagnostic.Id == "IXM1002");
        _ = diagnostics.Count(diagnostic => string.Equals(diagnostic.Id, "IXM1003", StringComparison.Ordinal)).Should().BeGreaterThanOrEqualTo(4);
    }

    [Xunit.Fact]
    public async Task Service_types_and_members_require_documentation_but_ordinary_interface_implementations_are_exempt()
    {
        string source = JoinLines(
            "public interface IWorker { void Run(); }",
            "public sealed class Worker : IWorker { public void Run() { } }",
            "public interface IPaymentsService { void Pay(); }",
            "public sealed class PaymentsService : IPaymentsService { public void Pay() { } }");

        ImmutableArray<Diagnostic> diagnostics = await AnalyzeAsync(source, cancellationToken: Xunit.TestContext.Current.CancellationToken).ConfigureAwait(true);

        _ = diagnostics.Should().Contain(diagnostic => diagnostic.Id == "IXM1004");
        _ = diagnostics.Should().Contain(diagnostic => diagnostic.Id == "IXM1005");
        _ = diagnostics.Should().NotContain(diagnostic =>
            diagnostic.Id == "IXM1002" && diagnostic.GetMessage(CultureInfo.InvariantCulture).Contains("IPaymentsService", StringComparison.Ordinal));
        _ = diagnostics.Where(diagnostic => string.Equals(diagnostic.Id, "IXM1004", StringComparison.Ordinal) || string.Equals(diagnostic.Id, "IXM1005", StringComparison.Ordinal))
            .Should().NotContain(diagnostic => diagnostic.GetMessage(CultureInfo.InvariantCulture).Contains("Worker", StringComparison.Ordinal));
    }

    [Xunit.Fact]
    public async Task Complete_documentation_and_valid_inheritdoc_are_accepted()
    {
        string source = JoinLines(
            "/// <summary>Creates orders.</summary>",
            "/// <typeparam name=\"T\">The result type.</typeparam>",
            "public interface IOrdersService<T>", "{",
            "    /// <summary>Creates an order.</summary>",
            "    /// <typeparam name=\"TRequest\">The request type.</typeparam>",
            "    /// <param name=\"request\">The request.</param>",
            "    /// <returns>The created order.</returns>",
            "    T Create<TRequest>(TRequest request);", "}",
            "/// <summary>Creates orders.</summary>",
            "public sealed class OrdersService : IOrdersService<string>", "{",
            "    /// <inheritdoc/>",
            "    public string Create<TRequest>(TRequest request) => string.Empty;", "}",
            "/// <summary>A customer.</summary>",
            "/// <param name=\"Name\">The customer name.</param>",
            "public record CustomerDto(string Name);");

        ImmutableArray<Diagnostic> diagnostics = await AnalyzeAsync(source, cancellationToken: Xunit.TestContext.Current.CancellationToken).ConfigureAwait(true);

        _ = diagnostics.Should().NotContain(diagnostic =>
            string.Equals(diagnostic.Id, "IXM1001", StringComparison.Ordinal)
            || string.Equals(diagnostic.Id, "IXM1002", StringComparison.Ordinal)
            || string.Equals(diagnostic.Id, "IXM1003", StringComparison.Ordinal)
            || string.Equals(diagnostic.Id, "IXM1004", StringComparison.Ordinal)
            || string.Equals(diagnostic.Id, "IXM1005", StringComparison.Ordinal));
    }

    [Xunit.Theory]
    [Xunit.InlineData("/// <inheritdoc/>\npublic interface IOrphan { }")]
    [Xunit.InlineData("/// <summary></summary>\npublic interface IEmpty { }")]
    [Xunit.InlineData("/// <summary>One.</summary>\n/// <summary>Two.</summary>\npublic interface IDuplicate { }")]
    [Xunit.InlineData("/// <summery>Misspelled.</summery>\npublic interface IMisspelled { }")]
    public async Task Invalid_or_incomplete_documentation_is_reported(string source)
    {
        ImmutableArray<Diagnostic> diagnostics = await AnalyzeAsync(source, cancellationToken: Xunit.TestContext.Current.CancellationToken).ConfigureAwait(true);

        _ = diagnostics.Should().Contain(diagnostic => diagnostic.Id == "IXM1002");
    }

    [Xunit.Fact]
    public async Task Inheritdoc_requires_a_complete_recursive_contract_and_supports_named_type_sources()
    {
        string completeSource = JoinLines(
            "/// <summary>A documented base.</summary>", "public interface IBase { }",
            "/// <inheritdoc cref=\"T:IBase\"/>", "public interface INamedDerived : IBase { }",
            "/// <inheritdoc/>", "public interface IMiddle : IBase { }",
            "/// <inheritdoc/>", "public interface ITop : IMiddle { }");

        ImmutableArray<Diagnostic> completeDiagnostics = await AnalyzeAsync(completeSource, cancellationToken: Xunit.TestContext.Current.CancellationToken).ConfigureAwait(true);

        _ = completeDiagnostics.Should().NotContain(diagnostic => diagnostic.Id == "IXM1002");

        string incompleteSource = JoinLines(
            "/// <summary></summary>", "public interface IBase { }",
            "/// <inheritdoc/>", "public interface IDerived : IBase { }");

        ImmutableArray<Diagnostic> incompleteDiagnostics = await AnalyzeAsync(incompleteSource, cancellationToken: Xunit.TestContext.Current.CancellationToken).ConfigureAwait(true);

        _ = incompleteDiagnostics.Should().Contain(diagnostic =>
            diagnostic.Id == "IXM1002" && diagnostic.GetMessage(CultureInfo.InvariantCulture).Contains("IDerived", StringComparison.Ordinal));
    }

    [Xunit.Fact]
    public async Task Inheritdoc_resolves_methods_properties_and_events_but_rejects_fake_elements()
    {
        string source = JoinLines(
            "/// <summary>Base service.</summary>", "public interface IBaseService", "{",
            "    /// <summary>Runs.</summary>", "    /// <param name=\"value\">Value.</param>",
            "    /// <returns>Result.</returns>", "    int Run(int value);",
            "    /// <summary>Name.</summary>", "    /// <value>Value.</value>", "    string Name { get; }",
            "    /// <summary>Changed.</summary>", "    event EventHandler Changed;", "}",
            "/// <summary>Derived service.</summary>", "public interface IDerivedService : IBaseService", "{",
            "    /// <inheritdoc/>", "    int Run(int value);", "    /// <inheritdoc/>",
            "    string Name { get; }", "    /// <inheritdoc/>", "    event EventHandler Changed;", "}",
            "/// <remarks>inheritdoc</remarks>", "public interface IFake { }");

        ImmutableArray<Diagnostic> diagnostics = await AnalyzeAsync(source, cancellationToken: Xunit.TestContext.Current.CancellationToken).ConfigureAwait(true);

        _ = diagnostics.Should().NotContain(diagnostic => diagnostic.Id == "IXM1005" && diagnostic.GetMessage(CultureInfo.InvariantCulture).Contains("Run", StringComparison.Ordinal));
        _ = diagnostics.Should().NotContain(diagnostic => diagnostic.Id == "IXM1005" && diagnostic.GetMessage(CultureInfo.InvariantCulture).Contains("Name", StringComparison.Ordinal));
        _ = diagnostics.Should().NotContain(diagnostic => diagnostic.Id == "IXM1005" && diagnostic.GetMessage(CultureInfo.InvariantCulture).Contains("Changed", StringComparison.Ordinal));
        _ = diagnostics.Should().Contain(diagnostic => diagnostic.Id == "IXM1002" && diagnostic.GetMessage(CultureInfo.InvariantCulture).Contains("IFake", StringComparison.Ordinal));
    }

    [Xunit.Fact]
    public async Task Private_interface_helpers_are_excluded_from_documentation_requirements()
    {
        string source = JoinLines(
            "/// <summary>A service.</summary>", "public interface IWorkerService", "{",
            "    private void Helper() { }", "    void Run();", "}");

        ImmutableArray<Diagnostic> diagnostics = await AnalyzeAsync(source, cancellationToken: Xunit.TestContext.Current.CancellationToken).ConfigureAwait(true);

        _ = diagnostics.Should().Contain(diagnostic => diagnostic.Id == "IXM1005" && diagnostic.GetMessage(CultureInfo.InvariantCulture).Contains("Run", StringComparison.Ordinal));
        _ = diagnostics.Should().NotContain(diagnostic => diagnostic.Id == "IXM1005" && diagnostic.GetMessage(CultureInfo.InvariantCulture).Contains("Helper", StringComparison.Ordinal));
    }

    [Xunit.Theory]
    [Xunit.InlineData("Generated.g.cs", "User.cs")]
    [Xunit.InlineData("User.cs", "Generated.g.cs")]
    public async Task Mixed_generated_and_user_partials_are_analyzed_at_the_deterministic_user_location(string firstPath, string secondPath)
    {
        ArgumentNullException.ThrowIfNull(firstPath);
        ArgumentNullException.ThrowIfNull(secondPath);

        const string firstSource = "public partial class CustomerDto { }";
        const string secondSource = "public partial class CustomerDto { }";

        ImmutableArray<Diagnostic> diagnostics = await AnalyzeAsync(
            [(firstSource, firstPath), (secondSource, secondPath)]).ConfigureAwait(true);

        Diagnostic diagnostic = diagnostics.Single(candidate => string.Equals(candidate.Id, "IXM1001", StringComparison.Ordinal));
        _ = diagnostic.Location.SourceTree!.FilePath.Should().Be("User.cs");
    }

    [Xunit.Fact]
    public async Task Unsafe_or_oversized_documentation_xml_is_incomplete()
    {
        string dtdSource = JoinLines(
            "/// <!DOCTYPE member [<!ENTITY x \"text\">]>",
            "/// <summary>&x;</summary>", "public interface IDtd { }");
        string oversizedSource = "/// <summary>" + new string('x', 1_000_001) + "</summary>\npublic interface ILarge { }";

        ImmutableArray<Diagnostic> dtdDiagnostics = await AnalyzeAsync(dtdSource, cancellationToken: Xunit.TestContext.Current.CancellationToken).ConfigureAwait(true);
        ImmutableArray<Diagnostic> oversizedDiagnostics = await AnalyzeAsync(oversizedSource, cancellationToken: Xunit.TestContext.Current.CancellationToken).ConfigureAwait(true);

        _ = dtdDiagnostics.Should().Contain(diagnostic => diagnostic.Id == "IXM1002" && diagnostic.GetMessage(CultureInfo.InvariantCulture).Contains("IDtd", StringComparison.Ordinal));
        _ = oversizedDiagnostics.Should().Contain(diagnostic => diagnostic.Id == "IXM1002" && diagnostic.GetMessage(CultureInfo.InvariantCulture).Contains("ILarge", StringComparison.Ordinal));
    }

    [Xunit.Fact]
    public async Task Record_suggestion_targets_immutable_class_but_not_record_struct_enum_or_delegate()
    {
        string source = JoinLines(
            "public sealed class CustomerDto { public string Name { get; init; } = string.Empty; }",
            "public record CustomerRecord(string Name);", "public readonly struct CustomerStruct { }",
            "public enum CustomerKind { One }", "public delegate void CustomerHandler();");

        ImmutableArray<Diagnostic> diagnostics = await AnalyzeAsync(source, cancellationToken: Xunit.TestContext.Current.CancellationToken).ConfigureAwait(true);

        _ = diagnostics.Count(diagnostic => string.Equals(diagnostic.Id, "IXM2001", StringComparison.Ordinal)).Should().Be(1);
        _ = diagnostics.Single(diagnostic => string.Equals(diagnostic.Id, "IXM2001", StringComparison.Ordinal)).DefaultSeverity.Should().Be(DiagnosticSeverity.Info);
    }

    [Xunit.Fact]
    public async Task Visibility_generated_partial_malformed_and_suppressed_sources_are_handled_deterministically()
    {
        string source = JoinLines(
            "internal sealed class InternalDto { }", "public class Outer { internal sealed class HiddenDto { } }",
            "public partial class PublicDto { }", "public partial class PublicDto { }",
            "#pragma warning disable IXM1001", "public sealed class SuppressedDto { }",
            "#pragma warning restore IXM1001", "public interface IBroken { void M( ; }");

        ImmutableArray<Diagnostic> diagnostics = await AnalyzeAsync(source, cancellationToken: Xunit.TestContext.Current.CancellationToken).ConfigureAwait(true);

        _ = diagnostics.Count(diagnostic => string.Equals(diagnostic.Id, "IXM1001", StringComparison.Ordinal) && !diagnostic.IsSuppressed).Should().Be(1);
        _ = diagnostics.Should().Contain(diagnostic => diagnostic.Id == "IXM1001" && diagnostic.IsSuppressed);

        ImmutableArray<Diagnostic> generatedDiagnostics = await AnalyzeAsync("public sealed class GeneratedDto { }", path: "Generated.g.cs", cancellationToken: Xunit.TestContext.Current.CancellationToken).ConfigureAwait(true);
        _ = generatedDiagnostics.Should().NotContain(diagnostic => diagnostic.Id == "IXM1001");
    }

    [Xunit.Fact]
    public async Task Analysis_is_concurrent_and_observes_cancellation()
    {
        const string source = "public sealed class ConcurrentDto { }";
        IEnumerable<Task<ImmutableArray<Diagnostic>>> analyses = Enumerable.Range(0, 8).Select(_ => AnalyzeAsync(source, cancellationToken: Xunit.TestContext.Current.CancellationToken));

        ImmutableArray<Diagnostic>[] resultSets = await Task.WhenAll(analyses).ConfigureAwait(true);
        _ = resultSets.Should().AllSatisfy(result => result.Should().Contain(diagnostic => diagnostic.Id == "IXM1001"));

        using var cancellationSource = new CancellationTokenSource();
        await cancellationSource.CancelAsync().ConfigureAwait(true);
        Func<Task<ImmutableArray<Diagnostic>>> action = async () => await AnalyzeAsync(source, cancellationToken: cancellationSource.Token).ConfigureAwait(true);
        _ = await action.Should().ThrowAsync<OperationCanceledException>().ConfigureAwait(true);
    }

    private static async Task<ImmutableArray<Diagnostic>> AnalyzeAsync(
        string source,
        string projectRole = "Library",
        string path = "Test.cs",
        CancellationToken cancellationToken = default)
    {
        return await AnalyzerTestHarness.AnalyzeAsync(CreateAnalyzer(), source, projectRole, path, cancellationToken).ConfigureAwait(false);
    }

    private static async Task<ImmutableArray<Diagnostic>> AnalyzeAsync(IEnumerable<(string Source, string Path)> sources)
    {
        return await AnalyzerTestHarness.AnalyzeAsync(CreateAnalyzer(), sources, cancellationToken: Xunit.TestContext.Current.CancellationToken).ConfigureAwait(false);
    }

    private static DocumentationAndRecordAnalyzer CreateAnalyzer()
    {
        return new DocumentationAndRecordAnalyzer();
    }

    private static string JoinLines(params string[] lines)
    {
        return string.Join('\n', lines);
    }
}

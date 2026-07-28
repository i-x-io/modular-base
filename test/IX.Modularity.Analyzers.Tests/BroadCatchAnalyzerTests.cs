using System.Collections.Immutable;
using AwesomeAssertions;
using Microsoft.CodeAnalysis;

namespace IX.Modularity.Analyzers.Tests;

#pragma warning disable IDE0022, MA0136, MA0040, MA0006, xUnit1051 // Source snippets deliberately preserve analyzer-facing shape; cancellation is explicitly tested below.
public sealed class BroadCatchAnalyzerTests
{
    [Xunit.Fact]
    public async Task Only_reachable_terminal_bare_rethrows_are_allowed()
    {
        const string source = """
            public sealed class Handler
            {
                public void Allowed() { try { } catch (System.Exception) { throw; } }
                public void Branch(bool value) { try { } catch (System.Exception) { if (value) throw; } }
                public void Return() { try { } catch (System.Exception) { return; } }
                public void Replace() { try { } catch (System.Exception) { throw new InvalidOperationException(); } }
                public void ThrowVariable() { try { } catch (System.Exception exception) { throw exception; } }
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzeAsync(source).ConfigureAwait(true);

        AssertCatchLocations(diagnostics, "catch", "catch", "catch", "catch");
        _ = RuleDiagnostics(diagnostics).Length.Should().Be(4);
    }

    [Xunit.Fact]
    public async Task Filters_finally_loops_and_nested_functions_are_evaluated_as_control_flow()
    {
        const string source = """
            public sealed class Handler
            {
                public void Filter(bool enabled) { try { } catch (System.Exception) when (enabled) { throw; } }
                public void Finally() { try { } catch (System.Exception) { throw; } finally { Cleanup(); } }
                public void Loop(bool value) { try { } catch { while (value) { throw; } } }
                public void Logged() { try { } catch (System.Exception) { Cleanup(); throw; } }
                public void Nested() { try { } catch (System.Exception) { System.Action action = () => { throw new System.Exception(); }; throw; } }
                private static void Cleanup() { }
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzeAsync(source).ConfigureAwait(true);

        AssertCatchLocations(diagnostics, "catch");
    }

    [Xunit.Fact]
    public async Task Conditional_switch_loop_finally_and_nested_boundaries_follow_reachable_bare_rethrow_contract()
    {
        const string source = """
            public sealed class Handler
            {
                public void Conditional(bool value) { try { } catch (System.Exception) { if (value) { Cleanup(); } else { Cleanup(); } throw; } }
                public void Switch(int value) { try { } catch (System.Exception) { switch (value) { case 0: throw; default: throw; } } }
                public void NonTerminating() { try { } catch (System.Exception) { while (true) { Cleanup(); } } }
                public void NestedCleanup() { try { } catch (System.Exception) { try { Cleanup(); } finally { Cleanup(); } throw; } }
                public void NestedFinallyReplacement() { try { } catch (System.Exception) { try { throw; } finally { throw new System.Exception(); } } }
                public void FilteredInvalid(bool enabled) { try { } catch (System.Exception) when (enabled) { return; } }
                public void LocalFunction() { try { } catch (System.Exception) { void Local() { throw new System.Exception(); } Cleanup(); throw; } }
                public void Lambda() { try { } catch (System.Exception) { System.Action action = () => throw new System.Exception(); Cleanup(); throw; } }
                private static void Cleanup() { }
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzeAsync(source).ConfigureAwait(true);

        AssertCatchLocations(diagnostics, "catch", "catch", "catch");
    }

    [Xunit.Fact]
    public async Task Nested_lambda_and_local_function_catches_are_analyzed_independently_from_the_outer_catch_region()
    {
        const string source = """
            public sealed class Handler
            {
                public void Outer()
                {
                    try { }
                    catch (System.Exception)
                    {
                        System.Action action = () => { try { } catch (System.Exception) { throw; } };
                        void Local() { try { } catch (System.Exception) { throw; } }
                        throw new System.InvalidOperationException();
                    }
                }
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzeAsync(source).ConfigureAwait(true);
        ImmutableArray<Diagnostic> ruleDiagnostics = RuleDiagnostics(diagnostics);

        _ = ruleDiagnostics.Should().ContainSingle();
        _ = LocationText(ruleDiagnostics[0]).Should().Be("catch");
        _ = ruleDiagnostics[0].Location.GetLineSpan().StartLinePosition.Line.Should().Be(5);
    }

    [Xunit.Fact]
    public async Task Adjacent_and_nested_catches_report_only_the_region_that_throws_a_replacement_exception()
    {
        const string source = """
            public sealed class Handler
            {
                public void Adjacent()
                {
                    try { } catch (System.Exception) { throw; }
                    try { } catch (System.Exception) { throw new System.InvalidOperationException(); }
                }
                public void Nested()
                {
                    try { try { } catch (System.Exception) { throw new System.InvalidOperationException(); } } catch (System.Exception) { throw; }
                }
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzeAsync(source).ConfigureAwait(true);
        ImmutableArray<Diagnostic> ruleDiagnostics = RuleDiagnostics(diagnostics);

        _ = ruleDiagnostics.Should().HaveCount(2);
        _ = ruleDiagnostics.Select(LocationText).Should().AllSatisfy(location => location.Should().Be("catch"));
        _ = ruleDiagnostics.Select(diagnostic => diagnostic.Location.GetLineSpan().StartLinePosition.Line).Should().BeEquivalentTo([5, 9]);
    }

    [Xunit.Fact]
    public async Task Large_mixed_broad_catch_method_has_an_exact_deterministic_invalid_count()
    {
        const int catchCount = 96;
        string statements = string.Join(
            '\n',
            Enumerable.Range(0, catchCount).Select(static index => (index % 3) switch
            {
                0 => "try { } catch (System.Exception) { try { Cleanup(); } finally { Cleanup(); } throw; }",
                1 => "try { } catch (System.Exception) { try { throw; } finally { throw new System.InvalidOperationException(); } }",
                _ => "try { } catch (System.Exception) { Cleanup(); }",
            }));
        string source = "public sealed class Handler { public void Stress() { " + statements + " } private static void Cleanup() { } }";

        ImmutableArray<Diagnostic> diagnostics = await AnalyzeAsync(source, cancellationToken: Xunit.TestContext.Current.CancellationToken).ConfigureAwait(true);
        ImmutableArray<Diagnostic> ruleDiagnostics = RuleDiagnostics(diagnostics);

        _ = ruleDiagnostics.Should().HaveCount(catchCount - (catchCount / 3));
        _ = ruleDiagnostics.Select(LocationText).Should().AllSatisfy(location => location.Should().Be("catch"));
    }

    [Xunit.Fact]
    public async Task Typed_catches_generated_sources_suppression_malformed_input_concurrency_and_cancellation_are_safe()
    {
        const string source = """
            public sealed class Handler
            {
                public void Typed() { try { } catch (InvalidOperationException) { return; } }
                #pragma warning disable IXM3003
                public void Suppressed() { try { } catch (System.Exception) { return; } }
                #pragma warning restore IXM3003
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzeAsync(source).ConfigureAwait(true);
        ImmutableArray<Diagnostic> generated = await AnalyzeAsync("public class Generated { void M() { try { } catch (System.Exception) { return; } } }", path: "Generated.g.cs").ConfigureAwait(true);
        ImmutableArray<Diagnostic> malformed = await AnalyzeAsync("public class Broken { void M() { try { } catch (System.Exception) { throw; }").ConfigureAwait(true);

        _ = RuleDiagnostics(diagnostics).Should().BeEmpty();
        _ = diagnostics.Should().Contain(diagnostic => diagnostic.Id == "IXM3003" && diagnostic.IsSuppressed);
        _ = RuleDiagnostics(generated).Should().BeEmpty();
        _ = RuleDiagnostics(malformed).Should().NotContain(diagnostic => diagnostic.Location == Location.None);

        const string concurrentSource = "public class Handler { void M() { try { } catch (System.Exception) { return; } } }";
        ImmutableArray<Diagnostic>[] concurrent = await Task.WhenAll(Enumerable.Range(0, 8).Select(_ => AnalyzeAsync(concurrentSource))).ConfigureAwait(true);
        _ = concurrent.Should().AllSatisfy(result => RuleDiagnostics(result).Should().ContainSingle());

        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync().ConfigureAwait(true);
        Func<Task<ImmutableArray<Diagnostic>>> action = () => AnalyzeAsync(concurrentSource, cancellationToken: cancellation.Token);
        _ = await action.Should().ThrowAsync<OperationCanceledException>().ConfigureAwait(true);
    }

    private static Task<ImmutableArray<Diagnostic>> AnalyzeAsync(string source, string path = "Test.cs", CancellationToken cancellationToken = default) =>
        AnalyzerTestHarness.AnalyzeAsync(new DocumentationAndRecordAnalyzer(), source, path: path, cancellationToken: cancellationToken);

    private static ImmutableArray<Diagnostic> RuleDiagnostics(ImmutableArray<Diagnostic> diagnostics) => [.. diagnostics.Where(diagnostic => diagnostic.Id == "IXM3003" && !diagnostic.IsSuppressed)];

    private static void AssertCatchLocations(ImmutableArray<Diagnostic> diagnostics, params string[] expected) =>
        _ = RuleDiagnostics(diagnostics).Select(LocationText).Should().BeEquivalentTo(expected);

    private static string LocationText(Diagnostic diagnostic) => diagnostic.Location.SourceTree!.GetText().ToString(diagnostic.Location.SourceSpan);
}
#pragma warning restore IDE0022, MA0136, MA0040, MA0006, xUnit1051

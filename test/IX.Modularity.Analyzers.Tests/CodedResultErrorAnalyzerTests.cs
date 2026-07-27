using System.Collections.Immutable;
using AwesomeAssertions;
using Microsoft.CodeAnalysis;

namespace IX.Modularity.Analyzers.Tests;

#pragma warning disable IDE0022, MA0136, MA0040, MA0006, xUnit1051 // Source snippets deliberately preserve analyzer-facing shape; cancellation is explicitly tested below.
public sealed class CodedResultErrorAnalyzerTests
{
    [Xunit.Fact]
    public async Task Typed_errors_and_recursively_visible_collections_are_accepted_at_all_supported_boundaries()
    {
        const string source = """
            using System;
            using System.Collections.Generic;
            using FluentResults;
            using FluentResults.Extensions;
            public sealed class DomainError : Error { public const string Code = "domain_error"; }
            public static class Uses
            {
                public static Result Run() => Result.Fail(new DomainError());
                public static Result<int> Generic() => Result.Fail<int>(new DomainError());
                public static Result Conditions() => Result.OkIf(false, () => new DomainError()).WithErrors(new IError[] { new DomainError() });
                public static Result More() => Result.FailIf(true, new[] { new DomainError() }).WithError(new DomainError());
                public static Result NotEmpty() => Result.FailIfNotEmpty(new[] { 1 }, _ => new DomainError());
                public static Result Converted() { Result result = new DomainError(); return result; }
                public static Result Extension(Result result) => result.OrFailIf(true, "not_checked");
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzeAsync(source).ConfigureAwait(true);

        AssertArgumentLocations(diagnostics, "\"not_checked\"");
    }

    [Xunit.Fact]
    public async Task String_base_and_nonconforming_error_failures_report_the_exact_failure_argument()
    {
        const string source = """
            using FluentResults;
            public sealed class NoCode : Error { }
            public sealed class BadCase : Error { public const string Code = "Bad_Code"; }
            public sealed class StaticReadonly : Error { public static readonly string Code = "static_readonly"; }
            public sealed class Inherited : ParentError { }
            public class ParentError : Error { public const string Code = "parent_error"; }
            public static class Uses
            {
                public static Result One() => Result.Fail("string_failure");
                public static Result Two() => Result.Fail(new Error("uncoded"));
                public static Result Three() => Result.Fail(new NoCode());
                public static Result Four() => Result.Fail(new BadCase());
                public static Result Five() => Result.Fail(new StaticReadonly());
                public static Result Six() => Result.Fail(new Inherited());
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzeAsync(source).ConfigureAwait(true);

        AssertArgumentLocations(diagnostics, "\"string_failure\"", "new Error(\"uncoded\")", "new NoCode()", "new BadCase()", "new StaticReadonly()", "new Inherited()");
    }

    [Xunit.Fact]
    public async Task Unknown_factories_collections_conversions_and_result_try_are_reported_but_suppressions_are_preserved()
    {
        const string source = """
            using System;
            using System.Collections.Generic;
            using FluentResults;
            public sealed class DomainError : Error { public const string Code = "domain_error"; }
            public static class Uses
            {
                static IError Unknown() => new DomainError();
                public static Result Factory() => Result.Fail(Unknown());
                public static Result Collection(IEnumerable<IError> errors) => Result.Fail(errors);
                public static Result Try() => Result.Try((Action)(() => { }), _ => new DomainError());
                #pragma warning disable IXM3002
                public static Result Suppressed() => Result.Fail("suppressed");
                #pragma warning restore IXM3002
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzeAsync(source).ConfigureAwait(true);

        AssertArgumentLocations(diagnostics, "Unknown()", "errors", "Result.Try((Action)(() => { }), _ => new DomainError())");
        _ = diagnostics.Should().Contain(diagnostic => diagnostic.Id == "IXM3002" && diagnostic.IsSuppressed && LocationText(diagnostic) == "\"suppressed\"");
    }

    [Xunit.Fact]
    public async Task Nested_lambda_returns_and_implicit_generic_and_non_generic_conversions_are_checked_recursively()
    {
        const string source = """
            using System.Collections.Generic;
            using FluentResults;
            public sealed class DomainError : Error { public const string Code = "domain_error"; }
            public static class Uses
            {
                public static Result IfFactory(bool value) => Result.OkIf(value, () => { if (value) { return new DomainError(); } return new Error("uncoded"); });
                public static Result SwitchFactory(int value) => Result.FailIf(true, () => { switch (value) { case 0: return new DomainError(); default: { return new Error("uncoded"); } } });
                public static Result BlockFactory() => Result.FailIfNotEmpty(new[] { 1 }, _ => { { return new DomainError(); } });
                public static Result Converted() { Result result = new Error("uncoded"); return result; }
                public static Result<int> ConvertedGeneric() { Result<int> result = new Error("uncoded"); return result; }
                public static Result ListConverted() { Result result = new List<Error> { new DomainError(), new Error("uncoded") }; return result; }
                public static Result<int> ListConvertedGeneric() { Result<int> result = new List<Error> { new DomainError(), new Error("uncoded") }; return result; }
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzeAsync(source).ConfigureAwait(true);

        AssertArgumentLocations(diagnostics, "new Error(\"uncoded\")", "new Error(\"uncoded\")", "new Error(\"uncoded\")", "new Error(\"uncoded\")", "new Error(\"uncoded\")", "new Error(\"uncoded\")");
    }

    [Xunit.Fact]
    public async Task Every_recognized_overload_and_invalid_code_shape_reports_the_failure_value()
    {
        const string source = """
            using System;
            using System.Collections.Generic;
            using FluentResults;
            using FluentResults.Extensions;
            public sealed class Empty : Error { public const string Code = ""; }
            public sealed class Leading : Error { public const string Code = "_leading"; }
            public sealed class Trailing : Error { public const string Code = "trailing_"; }
            public sealed class Double : Error { public const string Code = "double__underscore"; }
            public sealed class Digit : Error { public const string Code = "1digit"; }
            public sealed class Upper : Error { public const string Code = "Upper"; }
            public sealed class Character : Error { public const string Code = "invalid-char"; }
            public sealed class Property : Error { public static string Code => "property"; }
            public sealed class Readonly : Error { public static readonly string Code = "readonly"; }
            public sealed class Child : Parent { }
            public class Parent : Error { public const string Code = "parent"; }
            public static class Uses
            {
                public static Result FailString() => Result.Fail("fail");
                public static Result<int> FailGenericStrings() => Result.Fail<int>(new[] { "one", "two" });
                public static Result WithString() => Result.Ok().WithError("with").WithErrors(new[] { "many" });
                public static Result OkIf() => Result.OkIf(false, "ok_if");
                public static Result FailIf() => Result.FailIf(true, "fail_if");
                public static Result OrFailIf(Result result) => result.OrFailIf(true, "or_fail");
                public static Result NotEmpty() => Result.FailIfNotEmpty(new IError[] { new Empty(), new Leading(), new Trailing(), new Double(), new Digit(), new Upper(), new Character(), new Property(), new Readonly(), new Child() });
                public static Result Try() => Result.Try((Action)(() => { }), _ => new Error("uncoded"));
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzeAsync(source).ConfigureAwait(true);

        AssertArgumentLocations(diagnostics, "\"fail\"", "\"one\"", "\"two\"", "\"with\"", "\"many\"", "\"ok_if\"", "\"fail_if\"", "\"or_fail\"", "new Empty()", "new Leading()", "new Trailing()", "new Double()", "new Digit()", "new Upper()", "new Character()", "new Property()", "new Readonly()", "new Child()", "Result.Try((Action)(() => { }), _ => new Error(\"uncoded\"))");
    }

    [Xunit.Fact]
    public async Task Role_gating_metadata_absence_malformed_input_concurrency_and_cancellation_are_safe()
    {
        const string source = "using FluentResults; public static class Uses { public static Result Fail() => Result.Fail(\"bad\"); }";

        ImmutableArray<Diagnostic> library = await AnalyzeAsync(source, projectRole: "Library").ConfigureAwait(true);
        ImmutableArray<Diagnostic> missingRole = await AnalyzeAsync(source, projectRole: null).ConfigureAwait(true);
        ImmutableArray<Diagnostic> test = await AnalyzeAsync(source, projectRole: "Test").ConfigureAwait(true);
        ImmutableArray<Diagnostic> architecture = await AnalyzeAsync(source, projectRole: "ArchitectureTest").ConfigureAwait(true);
        ImmutableArray<Diagnostic> analyzer = await AnalyzeAsync(source, projectRole: "Analyzer").ConfigureAwait(true);
        ImmutableArray<Diagnostic> generator = await AnalyzeAsync(source, projectRole: "SourceGenerator").ConfigureAwait(true);
        ImmutableArray<Diagnostic> missingMetadata = await AnalyzeAsync(source, includeFluentResults: false).ConfigureAwait(true);
        ImmutableArray<Diagnostic> malformed = await AnalyzeAsync("using FluentResults; public class Broken { Result M( => Result.Fail(\"bad\"); }").ConfigureAwait(true);

        _ = RuleDiagnostics(library).Should().ContainSingle();
        _ = RuleDiagnostics(missingRole).Should().ContainSingle();
        _ = RuleDiagnostics(test).Should().BeEmpty();
        _ = RuleDiagnostics(architecture).Should().BeEmpty();
        _ = RuleDiagnostics(analyzer).Should().BeEmpty();
        _ = RuleDiagnostics(generator).Should().BeEmpty();
        _ = RuleDiagnostics(missingMetadata).Should().BeEmpty();
        _ = RuleDiagnostics(malformed).Should().NotContain(diagnostic => diagnostic.Location == Location.None);

        ImmutableArray<Diagnostic>[] concurrent = await Task.WhenAll(Enumerable.Range(0, 8).Select(_ => AnalyzeAsync(source))).ConfigureAwait(true);
        _ = concurrent.Should().AllSatisfy(result => RuleDiagnostics(result).Should().ContainSingle());

        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync().ConfigureAwait(true);
        Func<Task<ImmutableArray<Diagnostic>>> action = () => AnalyzeAsync(source, cancellationToken: cancellation.Token);
        _ = await action.Should().ThrowAsync<OperationCanceledException>().ConfigureAwait(true);
    }

    private static Task<ImmutableArray<Diagnostic>> AnalyzeAsync(string source, string? projectRole = "Library", bool includeFluentResults = true, CancellationToken cancellationToken = default) =>
        AnalyzerTestHarness.AnalyzeAsync(new DocumentationAndRecordAnalyzer(), source, projectRole, includeFluentResults: includeFluentResults, cancellationToken: cancellationToken);

    private static ImmutableArray<Diagnostic> RuleDiagnostics(ImmutableArray<Diagnostic> diagnostics) => [.. diagnostics.Where(diagnostic => diagnostic.Id == "IXM3002" && !diagnostic.IsSuppressed)];

    private static void AssertArgumentLocations(ImmutableArray<Diagnostic> diagnostics, params string[] expected) =>
        _ = RuleDiagnostics(diagnostics).Select(LocationText).Should().BeEquivalentTo(expected);

    private static string LocationText(Diagnostic diagnostic) => diagnostic.Location.SourceTree!.GetText().ToString(diagnostic.Location.SourceSpan);
}
#pragma warning restore IDE0022, MA0136, MA0040, MA0006, xUnit1051

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;

namespace IX.Modularity.Analyzers.Tests;

internal static class AnalyzerTestHarness
{
    private static readonly ImmutableArray<MetadataReference> s_references = CreateReferences();
    private static readonly AnalyzerConfigOptions s_emptyOptions = new TestAnalyzerConfigOptions(projectRole: null);

    public static Task<ImmutableArray<Diagnostic>> AnalyzeAsync(
        DiagnosticAnalyzer analyzer,
        string source,
        string projectRole = "Library",
        string path = "Test.cs",
        CancellationToken cancellationToken = default)
    {
        return AnalyzeAsync(analyzer, [(source, path)], projectRole, cancellationToken);
    }

    public static async Task<ImmutableArray<Diagnostic>> AnalyzeAsync(
        DiagnosticAnalyzer analyzer,
        IEnumerable<(string Source, string Path)> sources,
        string projectRole = "Library",
        CancellationToken cancellationToken = default)
    {
        SyntaxTree[] trees = [.. sources.Select(source => CSharpSyntaxTree.ParseText(
                source.Source,
                CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.Preview),
                path: source.Path,
                cancellationToken: cancellationToken))];
        var compilation = CSharpCompilation.Create(
            assemblyName: "AnalyzerTests",
            syntaxTrees: trees,
            references: s_references,
            options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        AnalyzerOptions options = new(
            [],
            new TestAnalyzerConfigOptionsProvider(projectRole));
        ImmutableArray<Diagnostic> diagnostics = await compilation
            .WithAnalyzers(
                [analyzer],
                new CompilationWithAnalyzersOptions(
                    options,
                    onAnalyzerException: null,
                    concurrentAnalysis: true,
                    logAnalyzerExecutionTime: false,
                    reportSuppressedDiagnostics: true))
            .GetAnalyzerDiagnosticsAsync(cancellationToken)
            .ConfigureAwait(false);

        return [.. diagnostics.OrderBy(diagnostic => diagnostic.Location.SourceSpan.Start)];
    }

    private static ImmutableArray<MetadataReference> CreateReferences()
    {
        string[] trustedAssemblies = ((string?)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES"))
            ?.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries)
            ?? throw new InvalidOperationException("Trusted platform assemblies are unavailable.");

        return [.. trustedAssemblies.Select(static path => (MetadataReference)MetadataReference.CreateFromFile(path))];
    }

    private sealed class TestAnalyzerConfigOptionsProvider(string projectRole) : AnalyzerConfigOptionsProvider
    {

        public override AnalyzerConfigOptions GlobalOptions { get; } = new TestAnalyzerConfigOptions(projectRole);

        public override AnalyzerConfigOptions GetOptions(SyntaxTree tree)
        {
            return s_emptyOptions;
        }

        public override AnalyzerConfigOptions GetOptions(AdditionalText textFile)
        {
            return s_emptyOptions;
        }
    }

    private sealed class TestAnalyzerConfigOptions(string? projectRole) : AnalyzerConfigOptions
    {
        public override bool TryGetValue(string key, out string value)
        {
            if (string.Equals(key, "build_property.IXModularityProjectRole", StringComparison.Ordinal)
                && projectRole is not null)
            {
                value = projectRole;
                return true;
            }

            value = string.Empty;
            return false;
        }
    }
}

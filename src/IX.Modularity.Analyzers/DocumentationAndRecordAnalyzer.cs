using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Threading;
using System.Xml;
using System.Xml.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.FlowAnalysis;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.Text;

namespace IX.Modularity.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class DocumentationAndRecordAnalyzer : DiagnosticAnalyzer
{
    private const string DocumentationCategory = "Documentation";
    private const string DesignCategory = "Design";
    private const string HelpLinkBase = "https://github.com/modular-base/modular-base/blob/main/src/IX.Modularity.Analyzers/docs/analyzers/diagnostics/";

    private static readonly ImmutableHashSet<string> s_dataSuffixes = ImmutableHashSet.Create(
        StringComparer.Ordinal,
        "Command",
        "Configuration",
        "Dto",
        "Error",
        "Event",
        "Message",
        "Notification",
        "Options",
        "Query",
        "Request",
        "Response",
        "Result");

    private static readonly DiagnosticDescriptor s_publicDataObjectDocumentation = CreateDescriptor(
        "IXM1001",
        "Public data objects require complete XML documentation",
        "Public data object '{0}' must have complete XML documentation",
        DocumentationCategory,
        DiagnosticSeverity.Warning);

    private static readonly DiagnosticDescriptor s_publicInterfaceDocumentation = CreateDescriptor(
        "IXM1002",
        "Public interfaces require complete XML documentation",
        "Public interface '{0}' must have complete XML documentation",
        DocumentationCategory,
        DiagnosticSeverity.Warning);

    private static readonly DiagnosticDescriptor s_interfaceMemberDocumentation = CreateDescriptor(
        "IXM1003",
        "Interface members require complete XML documentation",
        "Interface member '{0}' must have complete XML documentation",
        DocumentationCategory,
        DiagnosticSeverity.Warning);

    private static readonly DiagnosticDescriptor s_serviceTypeDocumentation = CreateDescriptor(
        "IXM1004",
        "Public services require complete XML documentation",
        "Public service '{0}' must have complete XML documentation",
        DocumentationCategory,
        DiagnosticSeverity.Warning);

    private static readonly DiagnosticDescriptor s_serviceMemberDocumentation = CreateDescriptor(
        "IXM1005",
        "Service members require complete XML documentation",
        "Service member '{0}' must have complete XML documentation",
        DocumentationCategory,
        DiagnosticSeverity.Warning);

    private static readonly DiagnosticDescriptor s_preferRecord = CreateDescriptor(
        "IXM2001",
        "Data objects should be records",
        "Data object '{0}' should be declared as a record",
        DesignCategory,
        DiagnosticSeverity.Info);

    private static readonly DiagnosticDescriptor s_serviceOperationMustReturnResult = CreateDescriptor(
        "IXM3001",
        "Service operation must return FluentResults",
        "Service operation '{0}' must return FluentResults.Result or an approved Task/ValueTask result shape",
        DesignCategory,
        DiagnosticSeverity.Warning,
        "Externally visible service operations must expose FluentResults Result contracts.");

    private static readonly DiagnosticDescriptor s_businessFailureMustUseCodedError = CreateDescriptor(
        "IXM3002",
        "Business failure must use a coded error",
        "Business failure must use a concrete FluentResults.Error with a public const string Code in lowercase snake case",
        DesignCategory,
        DiagnosticSeverity.Warning,
        "Direct business failure creation must use a concrete coded FluentResults error.");

    private static readonly DiagnosticDescriptor s_broadCatchMustRethrow = CreateDescriptor(
        "IXM3003",
        "Broad exception catch must rethrow",
        "Broad catch must propagate the original exception with a bare throw on every reachable path",
        DesignCategory,
        DiagnosticSeverity.Warning,
        "Broad exception catches must preserve the original exception with a bare rethrow on every reachable path.");

    /// <inheritdoc />
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
    [
        s_publicDataObjectDocumentation,
        s_publicInterfaceDocumentation,
        s_interfaceMemberDocumentation,
        s_serviceTypeDocumentation,
        s_serviceMemberDocumentation,
        s_preferRecord,
        s_serviceOperationMustReturnResult,
        s_businessFailureMustUseCodedError,
        s_broadCatchMustRethrow,
    ];

    /// <inheritdoc />
    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSymbolAction(AnalyzeNamedType, SymbolKind.NamedType);
        context.RegisterCompilationStartAction(static compilationContext =>
        {
            var symbols = new ResultSymbols(compilationContext.Compilation);
            var reportedBroadCatches = new ConcurrentDictionary<(SyntaxTree Tree, TextSpan Span), byte>();
            compilationContext.RegisterSymbolAction(symbolContext => AnalyzeServiceOperation(symbolContext, symbols), SymbolKind.Method);

            if (!ShouldSkipCodedFailureAnalysis(compilationContext.Options.AnalyzerConfigOptionsProvider))
            {
                compilationContext.RegisterOperationAction(operationContext => AnalyzeFailureInvocation(operationContext, symbols), OperationKind.Invocation);
                compilationContext.RegisterOperationAction(operationContext => AnalyzeFailureConversion(operationContext, symbols), OperationKind.Conversion);
            }

            compilationContext.RegisterOperationBlockAction(operationContext => AnalyzeBroadCatches(operationContext, symbols, reportedBroadCatches));
            compilationContext.RegisterSyntaxNodeAction(syntaxContext => AnalyzeBroadCatchNormalExit(syntaxContext, symbols, reportedBroadCatches), Microsoft.CodeAnalysis.CSharp.SyntaxKind.CatchClause);
        });
    }

    private static bool ShouldSkipCodedFailureAnalysis(AnalyzerConfigOptionsProvider options)
    {
        return options.GlobalOptions.TryGetValue("build_property.IXModularityProjectRole", out string? role)
            && (string.Equals(role, "Test", StringComparison.OrdinalIgnoreCase)
                || string.Equals(role, "ArchitectureTest", StringComparison.OrdinalIgnoreCase)
                || string.Equals(role, "Analyzer", StringComparison.OrdinalIgnoreCase)
                || string.Equals(role, "SourceGenerator", StringComparison.OrdinalIgnoreCase));
    }

    private static void AnalyzeServiceOperation(SymbolAnalysisContext context, ResultSymbols symbols)
    {
        var method = (IMethodSymbol)context.Symbol;
        if (method.MethodKind != MethodKind.Ordinary
            || method.IsImplicitlyDeclared
            || method.AssociatedSymbol is not null
            || !IsExternalMethod(method)
            || !IsService(method.ContainingType)
            || IsGenerated(method, context.CancellationToken)
            || IsInterfaceContractImplementation(method, context.CancellationToken))
        {
            return;
        }

        if (!symbols.HasServiceReturnSymbols || symbols.IsApprovedServiceReturn(method.ReturnType))
        {
            return;
        }

        context.ReportDiagnostic(Diagnostic.Create(s_serviceOperationMustReturnResult, GetLocation(method, context.CancellationToken), method.Name));
    }

    private static bool IsExternalMethod(IMethodSymbol method)
    {
        return method.ContainingType is not null
            && IsExternallyVisible(method.ContainingType)
            && method.DeclaredAccessibility is Accessibility.Public or Accessibility.Protected or Accessibility.ProtectedOrInternal;
    }

    private static bool IsInterfaceContractImplementation(IMethodSymbol method, CancellationToken cancellationToken)
    {
        if (method.ContainingType?.TypeKind == TypeKind.Interface)
        {
            return false;
        }

        if (method.ExplicitInterfaceImplementations.Any(contract => IsServiceInterface(contract.ContainingType)))
        {
            return true;
        }

        INamedTypeSymbol containingType = method.ContainingType!;
        foreach (INamedTypeSymbol @interface in containingType.AllInterfaces.Where(IsServiceInterface))
        {
            foreach (ISymbol member in @interface.GetMembers())
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (member is IMethodSymbol contract
                    && string.Equals(contract.Name, method.Name, StringComparison.Ordinal)
                    && contract.Arity == method.Arity
                    && HaveMatchingParameters(contract, method)
                    && SymbolEqualityComparer.Default.Equals(containingType.FindImplementationForInterfaceMember(contract), method))
                {
                    return true;
                }
            }
        }

        return false;
    }

    private static bool HaveMatchingParameters(IMethodSymbol left, IMethodSymbol right)
    {
        return left.Parameters.Length == right.Parameters.Length
            && left.Parameters.Zip(right.Parameters, static (leftParameter, rightParameter) => SymbolEqualityComparer.Default.Equals(leftParameter.Type, rightParameter.Type)).All(static matches => matches);
    }

    private static void AnalyzeFailureInvocation(OperationAnalysisContext context, ResultSymbols symbols)
    {
        var invocation = (IInvocationOperation)context.Operation;
        IMethodSymbol method = invocation.TargetMethod.ReducedFrom ?? invocation.TargetMethod;
        if (!symbols.IsFluentResultsMethod(method))
        {
            return;
        }

        if (string.Equals(method.Name, "Try", StringComparison.Ordinal) && SymbolEqualityComparer.Default.Equals(method.ContainingType, symbols.Result))
        {
            ReportCodedFailure(context, invocation.Syntax.GetLocation());
            return;
        }

        if (!symbols.IsFailureBoundary(method))
        {
            return;
        }

        foreach (IArgumentOperation argument in invocation.Arguments)
        {
            context.CancellationToken.ThrowIfCancellationRequested();
            if (symbols.IsFailureArgument(argument.Parameter?.Type))
            {
                ValidateFailureArgument(argument.Value, context, symbols);
            }
        }
    }

    private static void AnalyzeFailureConversion(OperationAnalysisContext context, ResultSymbols symbols)
    {
        var conversion = (IConversionOperation)context.Operation;
        if (!conversion.Conversion.IsImplicit || !symbols.IsResult(conversion.Type) || !symbols.IsFailureValue(conversion.Operand.Type))
        {
            return;
        }

        ValidateFailureArgument(conversion.Operand, context, symbols);
    }

    private static void ValidateFailureArgument(IOperation operation, OperationAnalysisContext context, ResultSymbols symbols)
    {
        operation = Unwrap(operation);
        if (operation.Type?.SpecialType == SpecialType.System_String)
        {
            ReportCodedFailure(context, operation.Syntax.GetLocation());
            return;
        }

        if (operation is IAnonymousFunctionOperation lambda)
        {
            if (lambda.Body is not null)
            {
                ValidateLambdaBody(lambda.Body, context, symbols);
            }
            else
            {
                ReportCodedFailure(context, operation.Syntax.GetLocation());
            }

            return;
        }

        if (operation is IArrayCreationOperation array && array.Initializer is not null)
        {
            foreach (IOperation element in array.Initializer.ElementValues)
            {
                context.CancellationToken.ThrowIfCancellationRequested();
                ValidateFailureArgument(element, context, symbols);
            }

            return;
        }

        if (operation is IObjectCreationOperation { Initializer: { } initializer } collection && symbols.IsErrorCollection(collection.Type))
        {
            ValidateCollectionInitializer(initializer, context, symbols);
            return;
        }

        if (operation is IObjectCreationOperation creation)
        {
            ValidateCreatedOrTypedError(creation.Type, creation.Syntax.GetLocation(), context, symbols);
            return;
        }

        if (symbols.IsError(operation.Type))
        {
            ValidateCreatedOrTypedError(operation.Type, operation.Syntax.GetLocation(), context, symbols);
            return;
        }

        ReportCodedFailure(context, operation.Syntax.GetLocation());
    }

    private static void ValidateCollectionInitializer(IObjectOrCollectionInitializerOperation initializer, OperationAnalysisContext context, ResultSymbols symbols)
    {
        foreach (IOperation element in initializer.Initializers)
        {
            context.CancellationToken.ThrowIfCancellationRequested();
            if (element is IInvocationOperation add && string.Equals(add.TargetMethod.Name, "Add", StringComparison.Ordinal))
            {
                foreach (IArgumentOperation argument in add.Arguments)
                {
                    context.CancellationToken.ThrowIfCancellationRequested();
                    ValidateFailureArgument(argument.Value, context, symbols);
                }
            }
            else
            {
                ValidateFailureArgument(element, context, symbols);
            }
        }
    }

    private static void ValidateLambdaBody(IOperation body, OperationAnalysisContext context, ResultSymbols symbols)
    {
        if (body is not IBlockOperation)
        {
            ValidateFailureArgument(body, context, symbols);
            return;
        }

        ValidateLambdaReturns(body, context, symbols);
    }

    private static void ValidateLambdaReturns(IOperation operation, OperationAnalysisContext context, ResultSymbols symbols)
    {
        context.CancellationToken.ThrowIfCancellationRequested();
        if (operation is IAnonymousFunctionOperation or ILocalFunctionOperation)
        {
            return;
        }

        if (operation is IReturnOperation returnOperation)
        {
            if (returnOperation.ReturnedValue is null)
            {
                ReportCodedFailure(context, returnOperation.Syntax.GetLocation());
            }
            else
            {
                ValidateFailureArgument(returnOperation.ReturnedValue, context, symbols);
            }

            return;
        }

        foreach (IOperation child in operation.ChildOperations)
        {
            ValidateLambdaReturns(child, context, symbols);
        }
    }

    private static IOperation Unwrap(IOperation operation)
    {
        while (operation is IConversionOperation conversion)
        {
            operation = conversion.Operand;
        }

        return operation is IDelegateCreationOperation { Target: { } target } ? Unwrap(target) : operation;
    }

    private static void ValidateCreatedOrTypedError(ITypeSymbol? type, Location location, OperationAnalysisContext context, ResultSymbols symbols)
    {
        if (type is not INamedTypeSymbol named || !symbols.IsError(named) || SymbolEqualityComparer.Default.Equals(named, symbols.Error))
        {
            ReportCodedFailure(context, location);
            return;
        }

        IFieldSymbol? code = named.GetMembers("Code").OfType<IFieldSymbol>().FirstOrDefault(field => SymbolEqualityComparer.Default.Equals(field.ContainingType, named));
        if (code is null
            || code.DeclaredAccessibility != Accessibility.Public
            || !code.IsConst
            || code.Type.SpecialType != SpecialType.System_String
            || code.ConstantValue is not string value
            || !IsLowerSnakeCase(value))
        {
            ReportCodedFailure(context, location);
        }
    }

    private static bool IsLowerSnakeCase(string value)
    {
        if (value.Length == 0 || value[0] is < 'a' or > 'z')
        {
            return false;
        }

        bool previousUnderscore = false;
        foreach (char character in value)
        {
            bool valid = (character is >= 'a' and <= 'z') || (character is >= '0' and <= '9') || character == '_';
            if (!valid || (character == '_' && previousUnderscore))
            {
                return false;
            }

            previousUnderscore = character == '_';
        }

        return !previousUnderscore;
    }

    private static void ReportCodedFailure(OperationAnalysisContext context, Location location)
    {
        context.ReportDiagnostic(Diagnostic.Create(s_businessFailureMustUseCodedError, location));
    }

    private static void AnalyzeBroadCatches(OperationBlockAnalysisContext context, ResultSymbols symbols, ConcurrentDictionary<(SyntaxTree Tree, TextSpan Span), byte> reportedBroadCatches)
    {
        foreach (IOperation operationBlock in context.OperationBlocks)
        {
            context.CancellationToken.ThrowIfCancellationRequested();
            ControlFlowGraph? graph = context.GetControlFlowGraph(operationBlock);
            if (graph is null)
            {
                continue;
            }

            ICatchClauseOperation[] catchOperations = [.. GetCatchClauses(operationBlock, isRoot: true, context.CancellationToken)];
            ControlFlowRegion[] catchRegions = [.. GetRegions(graph.Root, ControlFlowRegionKind.Catch, context.CancellationToken)
                .OrderBy(region => region.FirstBlockOrdinal)
                .ThenBy(region => region.LastBlockOrdinal)];
            ImmutableArray<ControlFlowRegion> finallyRegions = GetRegions(graph.Root, ControlFlowRegionKind.Finally, context.CancellationToken);
            (Dictionary<TextSpan, ControlFlowRegion> catchRegionIndex, HashSet<TextSpan> ambiguousHandlerSpans) = BuildCatchRegionIndex(graph, catchOperations, catchRegions, context.CancellationToken);
            Dictionary<ControlFlowRegion, ImmutableArray<ControlFlowRegion>> finallyRegionIndex = BuildFinallyRegionIndex(catchRegions, finallyRegions, context.CancellationToken);
            for (int index = 0; index < catchOperations.Length; index++)
            {
                ICatchClauseOperation catchOperation = catchOperations[index];
                if (!IsBroadCatch(catchOperation, symbols)
                    || catchOperation.Syntax is not CatchClauseSyntax catchSyntax)
                {
                    continue;
                }

                if (!catchRegionIndex.TryGetValue(catchOperation.Handler.Syntax.Span, out ControlFlowRegion? catchRegion)
                    || ambiguousHandlerSpans.Contains(catchOperation.Handler.Syntax.Span))
                {
                    TryReportBroadCatch(context.ReportDiagnostic, catchSyntax.CatchKeyword.GetLocation(), reportedBroadCatches);
                    continue;
                }

                if (!HasOnlyRethrowTransfers(graph, catchRegion, catchOperation.Handler.Syntax.Span, context.CancellationToken)
                    || FinallyCanOverrideRethrow(graph, catchRegion, finallyRegionIndex[catchRegion], context.CancellationToken))
                {
                    TryReportBroadCatch(context.ReportDiagnostic, catchSyntax.CatchKeyword.GetLocation(), reportedBroadCatches);
                }
            }
        }
    }

    private static IEnumerable<ICatchClauseOperation> GetCatchClauses(IOperation operation, bool isRoot, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (operation is ICatchClauseOperation catchClause)
        {
            yield return catchClause;
        }

        if (!isRoot && operation is IAnonymousFunctionOperation or ILocalFunctionOperation)
        {
            yield break;
        }

        foreach (IOperation child in operation.ChildOperations)
        {
            foreach (ICatchClauseOperation nestedCatchClause in GetCatchClauses(child, isRoot: false, cancellationToken))
            {
                yield return nestedCatchClause;
            }
        }
    }

    private static bool IsBroadCatch(ICatchClauseOperation catchOperation, ResultSymbols symbols)
    {
        return catchOperation.ExceptionType is null || SymbolEqualityComparer.Default.Equals(catchOperation.ExceptionType, symbols.Exception);
    }

    private static void AnalyzeBroadCatchNormalExit(SyntaxNodeAnalysisContext context, ResultSymbols symbols, ConcurrentDictionary<(SyntaxTree Tree, TextSpan Span), byte> reportedBroadCatches)
    {
        var catchSyntax = (CatchClauseSyntax)context.Node;
        if (catchSyntax.Block is null)
        {
            return;
        }

        bool isUntyped = catchSyntax.Declaration is null;
        ITypeSymbol? caughtType = catchSyntax.Declaration is null ? null : context.SemanticModel.GetTypeInfo(catchSyntax.Declaration.Type, context.CancellationToken).Type;
        if ((isUntyped || SymbolEqualityComparer.Default.Equals(caughtType, symbols.Exception))
            && context.SemanticModel.AnalyzeControlFlow(catchSyntax.Block).EndPointIsReachable)
        {
            TryReportBroadCatch(context.ReportDiagnostic, catchSyntax.CatchKeyword.GetLocation(), reportedBroadCatches);
        }
    }

    private static void TryReportBroadCatch(Action<Diagnostic> reportDiagnostic, Location location, ConcurrentDictionary<(SyntaxTree Tree, TextSpan Span), byte> reportedBroadCatches)
    {
        if (location.SourceTree is not null && reportedBroadCatches.TryAdd((location.SourceTree, location.SourceSpan), 0))
        {
            reportDiagnostic(Diagnostic.Create(s_broadCatchMustRethrow, location));
        }
    }

    private static ImmutableArray<ControlFlowRegion> GetRegions(ControlFlowRegion root, ControlFlowRegionKind kind, CancellationToken cancellationToken)
    {
        ImmutableArray<ControlFlowRegion>.Builder builder = ImmutableArray.CreateBuilder<ControlFlowRegion>();
        var pending = new Stack<ControlFlowRegion>();
        pending.Push(root);
        while (pending.Count > 0)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ControlFlowRegion region = pending.Pop();
            if (region.Kind == kind)
            {
                builder.Add(region);
            }

            for (int index = region.NestedRegions.Length - 1; index >= 0; index--)
            {
                pending.Push(region.NestedRegions[index]);
            }
        }

        return builder.ToImmutable();
    }

    private static bool IsOperationlessBareRethrow(CatchClauseSyntax catchSyntax)
    {
        return catchSyntax.Block is { Statements.Count: 1 }
            && catchSyntax.Block.Statements[0] is ThrowStatementSyntax { Expression: null };
    }

    private static (Dictionary<TextSpan, ControlFlowRegion> Unique, HashSet<TextSpan> Ambiguous) BuildCatchRegionIndex(ControlFlowGraph graph, ICatchClauseOperation[] catchOperations, ControlFlowRegion[] catchRegions, CancellationToken cancellationToken)
    {
        var index = new Dictionary<TextSpan, ControlFlowRegion>();
        var ambiguous = new HashSet<TextSpan>();

        for (int regionIndex = 0; regionIndex < catchRegions.Length; regionIndex++)
        {
            ControlFlowRegion region = catchRegions[regionIndex];
            for (int ordinal = region.FirstBlockOrdinal; ordinal <= region.LastBlockOrdinal; ordinal++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                BasicBlock block = graph.Blocks[ordinal];
                foreach (IOperation operation in block.Operations)
                {
                    AssociateRegion(operation.Syntax, region, index, ambiguous, cancellationToken);
                }

                if (block.BranchValue is { Syntax: { } syntax })
                {
                    AssociateRegion(syntax, region, index, ambiguous, cancellationToken);
                }
            }
        }

        for (int operationIndex = 0; operationIndex < catchOperations.Length && operationIndex < catchRegions.Length; operationIndex++)
        {
            if (catchOperations[operationIndex].Syntax is CatchClauseSyntax catchSyntax
                && IsOperationlessBareRethrow(catchSyntax)
                && !index.ContainsKey(catchOperations[operationIndex].Handler.Syntax.Span)
                && !ambiguous.Contains(catchOperations[operationIndex].Handler.Syntax.Span))
            {
                index.Add(catchOperations[operationIndex].Handler.Syntax.Span, catchRegions[operationIndex]);
            }
        }

        return (index, ambiguous);
    }

    private static void AssociateRegion(SyntaxNode syntax, ControlFlowRegion region, Dictionary<TextSpan, ControlFlowRegion> index, HashSet<TextSpan> ambiguous, CancellationToken cancellationToken)
    {
        for (SyntaxNode? current = syntax; current is not null; current = current.Parent)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (current is CatchClauseSyntax { Block: { } block })
            {
                if (!ambiguous.Contains(block.Span))
                {
                    if (index.TryGetValue(block.Span, out ControlFlowRegion? existing) && !ReferenceEquals(existing, region))
                    {
                        _ = index.Remove(block.Span);
                        _ = ambiguous.Add(block.Span);
                    }
                    else if (existing is null)
                    {
                        index.Add(block.Span, region);
                    }
                }

                return;
            }
        }
    }

    private static Dictionary<ControlFlowRegion, ImmutableArray<ControlFlowRegion>> BuildFinallyRegionIndex(ControlFlowRegion[] catchRegions, ImmutableArray<ControlFlowRegion> finallyRegions, CancellationToken cancellationToken)
    {
        Dictionary<ControlFlowRegion, HashSet<ControlFlowRegion>> builders = catchRegions.ToDictionary(region => region, _ => new HashSet<ControlFlowRegion>());
        var finallyByOwner = new Dictionary<ControlFlowRegion, List<ControlFlowRegion>>();
        foreach (ControlFlowRegion finallyRegion in finallyRegions)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ControlFlowRegion? owner = GetAncestor(finallyRegion, ControlFlowRegionKind.TryAndFinally);
            if (owner is not null)
            {
                if (!finallyByOwner.TryGetValue(owner, out List<ControlFlowRegion>? groupedFinals))
                {
                    groupedFinals = [];
                    finallyByOwner.Add(owner, groupedFinals);
                }

                groupedFinals.Add(finallyRegion);
            }

            for (ControlFlowRegion? current = finallyRegion.EnclosingRegion; current is not null; current = current.EnclosingRegion)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (builders.TryGetValue(current, out HashSet<ControlFlowRegion>? associatedFinals))
                {
                    _ = associatedFinals.Add(finallyRegion);
                }
            }
        }

        foreach (ControlFlowRegion catchRegion in catchRegions)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ControlFlowRegion? owner = GetAncestor(catchRegion, ControlFlowRegionKind.TryAndFinally);
            if (owner is not null && finallyByOwner.TryGetValue(owner, out List<ControlFlowRegion>? groupedFinals))
            {
                foreach (ControlFlowRegion finallyRegion in groupedFinals)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    _ = builders[catchRegion].Add(finallyRegion);
                }
            }
        }

        return builders.ToDictionary(pair => pair.Key, pair => pair.Value.ToImmutableArray());
    }

    private static bool HasOnlyRethrowTransfers(ControlFlowGraph graph, ControlFlowRegion catchRegion, TextSpan handlerSpan, CancellationToken cancellationToken)
    {
        var pending = new Queue<BasicBlock>();
        var visited = new HashSet<BasicBlock>();
        BasicBlock entryBlock = GetHandlerEntryBlock(graph, catchRegion, handlerSpan, cancellationToken);
        pending.Enqueue(entryBlock);
        bool hasRethrow = false;
        while (pending.Count > 0)
        {
            cancellationToken.ThrowIfCancellationRequested();
            BasicBlock block = pending.Dequeue();
            if (!visited.Add(block))
            {
                continue;
            }

            if (!ValidateTransfer(block.FallThroughSuccessor, catchRegion, pending, ref hasRethrow)
                || !ValidateTransfer(block.ConditionalSuccessor, catchRegion, pending, ref hasRethrow))
            {
                return false;
            }
        }

        return hasRethrow;
    }

    private static BasicBlock GetHandlerEntryBlock(ControlFlowGraph graph, ControlFlowRegion catchRegion, TextSpan handlerSpan, CancellationToken cancellationToken)
    {
        for (int ordinal = catchRegion.FirstBlockOrdinal; ordinal <= catchRegion.LastBlockOrdinal; ordinal++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            BasicBlock block = graph.Blocks[ordinal];
            if (block.Operations.Any(operation => handlerSpan.Contains(operation.Syntax.Span))
                || (block.BranchValue is { Syntax: { } syntax } && handlerSpan.Contains(syntax.Span)))
            {
                return block;
            }
        }

        // A handler consisting solely of `throw;` has no operation or branch value.
        return graph.Blocks[catchRegion.FirstBlockOrdinal];
    }

    private static bool ValidateTransfer(ControlFlowBranch? branch, ControlFlowRegion catchRegion, Queue<BasicBlock> pending, ref bool hasRethrow)
    {
        if (branch is null)
        {
            return true;
        }

        if (branch.Semantics == ControlFlowBranchSemantics.Rethrow)
        {
            hasRethrow = true;
            return true;
        }

        if (branch.Destination is not null && IsInRegion(branch.Destination.EnclosingRegion, catchRegion))
        {
            pending.Enqueue(branch.Destination);
            return true;
        }

        return false;
    }

    private static bool FinallyCanOverrideRethrow(ControlFlowGraph graph, ControlFlowRegion catchRegion, ImmutableArray<ControlFlowRegion> finallyRegions, CancellationToken cancellationToken)
    {
        foreach (ControlFlowRegion finallyRegion in finallyRegions)
        {
            for (int ordinal = finallyRegion.FirstBlockOrdinal; ordinal <= finallyRegion.LastBlockOrdinal; ordinal++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                BasicBlock block = graph.Blocks[ordinal];
                if (!IsAllowedFinallyTransfer(block.FallThroughSuccessor)
                    || !IsAllowedFinallyTransfer(block.ConditionalSuccessor))
                {
                    return true;
                }
            }
        }

        return false;
    }

    private static bool IsAllowedFinallyTransfer(ControlFlowBranch? branch)
    {
        return branch is null || branch.Semantics != ControlFlowBranchSemantics.Throw;
    }

    private static ControlFlowRegion? GetAncestor(ControlFlowRegion region, ControlFlowRegionKind kind)
    {
        for (ControlFlowRegion? current = region; current is not null; current = current.EnclosingRegion)
        {
            if (current.Kind == kind)
            {
                return current;
            }
        }

        return null;
    }

    private static bool IsInRegion(ControlFlowRegion? region, ControlFlowRegion target)
    {
        for (ControlFlowRegion? current = region; current is not null; current = current.EnclosingRegion)
        {
            if (ReferenceEquals(current, target))
            {
                return true;
            }
        }

        return false;
    }

    private static void AnalyzeNamedType(SymbolAnalysisContext context)
    {
        INamedTypeSymbol type = (INamedTypeSymbol)context.Symbol;
        CancellationToken cancellationToken = context.CancellationToken;

        if (!IsExternallyVisible(type) || IsGenerated(type, cancellationToken))
        {
            return;
        }

        bool isService = IsService(type);
        bool isDataObject = IsDataObject(type, context.Options.AnalyzerConfigOptionsProvider, cancellationToken);

        if (isDataObject && !HasCompleteDocumentation(type, GetPositionalRecordParameterNames(type, cancellationToken), context.Compilation, cancellationToken))
        {
            context.ReportDiagnostic(Diagnostic.Create(s_publicDataObjectDocumentation, GetLocation(type, cancellationToken), type.Name));
        }

        if (isDataObject)
        {
            AnalyzePublicMembers(type, s_publicDataObjectDocumentation, context);
        }

        if (type.TypeKind == TypeKind.Interface)
        {
            if (!HasCompleteDocumentation(type, Array.Empty<string>(), context.Compilation, cancellationToken))
            {
                DiagnosticDescriptor descriptor = isService ? s_serviceTypeDocumentation : s_publicInterfaceDocumentation;
                context.ReportDiagnostic(Diagnostic.Create(descriptor, GetLocation(type, cancellationToken), type.Name));
            }

            AnalyzeInterfaceMembers(type, isService, context);
        }

        if (isService && type.TypeKind != TypeKind.Interface && !HasCompleteDocumentation(type, Array.Empty<string>(), context.Compilation, cancellationToken))
        {
            context.ReportDiagnostic(Diagnostic.Create(s_serviceTypeDocumentation, GetLocation(type, cancellationToken), type.Name));
        }

        if (isService && type.TypeKind != TypeKind.Interface)
        {
            AnalyzePublicMembers(type, s_serviceMemberDocumentation, context);
        }

        if (isDataObject && IsEligibleClass(type))
        {
            context.ReportDiagnostic(Diagnostic.Create(s_preferRecord, GetLocation(type, cancellationToken), type.Name));
        }
    }

    private static void AnalyzeInterfaceMembers(INamedTypeSymbol type, bool isService, SymbolAnalysisContext context)
    {
        foreach (ISymbol member in type.GetMembers())
        {
            context.CancellationToken.ThrowIfCancellationRequested();
            if (member.IsImplicitlyDeclared
                || member is IMethodSymbol { AssociatedSymbol: not null }
                || !IsExternallyVisible(member)
                || IsGenerated(member, context.CancellationToken))
            {
                continue;
            }

            if (!HasCompleteDocumentation(member, Array.Empty<string>(), context.Compilation, context.CancellationToken))
            {
                DiagnosticDescriptor descriptor = isService ? s_serviceMemberDocumentation : s_interfaceMemberDocumentation;
                context.ReportDiagnostic(Diagnostic.Create(descriptor, GetLocation(member, context.CancellationToken), member.Name));
            }
        }
    }

    private static void AnalyzePublicMembers(INamedTypeSymbol type, DiagnosticDescriptor descriptor, SymbolAnalysisContext context)
    {
        foreach (ISymbol member in type.GetMembers())
        {
            context.CancellationToken.ThrowIfCancellationRequested();
            if (member.IsImplicitlyDeclared
                || member is IMethodSymbol { AssociatedSymbol: not null }
                || IsPositionalRecordMember(type, member, context.CancellationToken)
                || !IsExternallyVisible(member)
                || IsGenerated(member, context.CancellationToken))
            {
                continue;
            }

            if (!HasCompleteDocumentation(member, Array.Empty<string>(), context.Compilation, context.CancellationToken))
            {
                context.ReportDiagnostic(Diagnostic.Create(descriptor, GetLocation(member, context.CancellationToken), member.Name));
            }
        }
    }

    private static bool IsPositionalRecordMember(INamedTypeSymbol type, ISymbol member, CancellationToken cancellationToken)
    {
        if (!type.IsRecord)
        {
            return false;
        }

        string[] parameterNames = GetPositionalRecordParameterNames(type, cancellationToken);
        return member switch
        {
            IPropertySymbol property => parameterNames.Contains(property.Name, StringComparer.OrdinalIgnoreCase),
            IMethodSymbol { MethodKind: MethodKind.Constructor } constructor => constructor.Parameters.Select(static parameter => parameter.Name).SequenceEqual(parameterNames, StringComparer.OrdinalIgnoreCase),
            _ => false,
        };
    }

    private static bool IsDataObject(INamedTypeSymbol type, AnalyzerConfigOptionsProvider options, CancellationToken cancellationToken)
    {
        if (type.IsRecord)
        {
            return true;
        }

        if (options.GlobalOptions.TryGetValue("build_property.IXModularityProjectRole", out string? role)
            && string.Equals(role, "Contracts", StringComparison.OrdinalIgnoreCase))
        {
            return type.TypeKind != TypeKind.Interface && !IsService(type);
        }

        cancellationToken.ThrowIfCancellationRequested();
        return s_dataSuffixes.Any(suffix => type.Name.EndsWith(suffix, StringComparison.Ordinal));
    }

    private static bool IsService(INamedTypeSymbol type)
    {
        if (type.Name.EndsWith("Service", StringComparison.Ordinal))
        {
            return true;
        }

        return type.AllInterfaces.Any(IsServiceInterface);
    }

    private static bool IsServiceInterface(INamedTypeSymbol type)
    {
        return type.Name.StartsWith("I", StringComparison.Ordinal)
        && type.Name.EndsWith("Service", StringComparison.Ordinal);
    }

    private static bool IsEligibleClass(INamedTypeSymbol type)
    {
        return type.TypeKind == TypeKind.Class
        && !type.IsAbstract
        && !type.IsStatic
        && !type.IsRecord
        && (type.BaseType is null || type.BaseType.SpecialType == SpecialType.System_Object);
    }

    private static bool IsExternallyVisible(INamedTypeSymbol type)
    {
        for (INamedTypeSymbol? current = type; current is not null; current = current.ContainingType)
        {
            if (current.DeclaredAccessibility != Accessibility.Public
                && current.DeclaredAccessibility != Accessibility.Protected
                && current.DeclaredAccessibility != Accessibility.ProtectedOrInternal)
            {
                return false;
            }
        }

        return true;
    }

    private static bool IsExternallyVisible(ISymbol symbol)
    {
        return symbol.DeclaredAccessibility is Accessibility.Public or Accessibility.Protected or Accessibility.ProtectedOrInternal
        && symbol.ContainingType is not null
        && IsExternallyVisible(symbol.ContainingType);
    }

    private static bool IsGenerated(ISymbol symbol, CancellationToken cancellationToken)
    {
        Location[] locations = symbol.Locations.Where(static candidate => candidate.IsInSource).ToArray();
        return locations.Length == 0 || locations.All(location => IsGenerated(location, cancellationToken));
    }

    private static bool IsGenerated(Location location, CancellationToken cancellationToken)
    {
        if (location.SourceTree is null)
        {
            return true;
        }

        string path = location.SourceTree.FilePath;
        if (path.EndsWith(".g.cs", StringComparison.OrdinalIgnoreCase)
            || path.EndsWith(".generated.cs", StringComparison.OrdinalIgnoreCase)
            || path.EndsWith(".designer.cs", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        string text = location.SourceTree.GetText(cancellationToken).ToString();
        return text.IndexOf("<auto-generated", StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private static Location GetLocation(ISymbol symbol, CancellationToken cancellationToken)
    {
        return symbol.Locations
            .Where(static location => location.IsInSource)
            .Where(location => !IsGenerated(location, cancellationToken))
            .OrderBy(static location => location.SourceTree?.FilePath, StringComparer.Ordinal)
            .ThenBy(static location => location.SourceSpan.Start)
            .FirstOrDefault()
        ?? symbol.Locations.FirstOrDefault(static location => location.IsInSource)
        ?? Location.None;
    }

    private static string[] GetPositionalRecordParameterNames(INamedTypeSymbol type, CancellationToken cancellationToken)
    {
        if (!type.IsRecord)
        {
            return Array.Empty<string>();
        }

        return type.DeclaringSyntaxReferences
            .Select(reference => reference.GetSyntax(cancellationToken))
            .OfType<RecordDeclarationSyntax>()
            .SelectMany(static declaration => declaration.ParameterList?.Parameters ?? [])
            .Select(static parameter => parameter.Identifier.ValueText)
            .Distinct(StringComparer.Ordinal)
            .ToArray();
    }

    private static bool HasCompleteDocumentation(
        ISymbol symbol,
        IEnumerable<string> additionalParameterNames,
        Compilation compilation,
        CancellationToken cancellationToken)
    {
        return HasCompleteDocumentation(symbol, additionalParameterNames, compilation, new HashSet<ISymbol>(SymbolEqualityComparer.Default), cancellationToken);
    }

    private static bool HasCompleteDocumentation(
        ISymbol symbol,
        IEnumerable<string> additionalParameterNames,
        Compilation compilation,
        ISet<ISymbol> visited,
        CancellationToken cancellationToken)
    {
        if (!visited.Add(symbol))
        {
            return false;
        }

        try
        {
            string? xml = symbol.GetDocumentationCommentXml(cancellationToken: cancellationToken);
            if (string.IsNullOrWhiteSpace(xml))
            {
                return false;
            }

            try
            {
                XElement root = LoadDocumentationXml(xml!);
                XElement[] inheritdocElements = root.Elements("inheritdoc").ToArray();
                if (inheritdocElements.Length > 0)
                {
                    return inheritdocElements.Length == 1
                        && HasCompleteInheritdocSource(symbol, inheritdocElements[0], compilation, visited, cancellationToken);
                }

                if (!HasExactlyOneText(root, "summary")
                    || !HasNamedElements(root, "typeparam", GetTypeParameterNames(symbol))
                    || !HasNamedElements(root, "param", GetParameterNames(symbol).Concat(additionalParameterNames)))
                {
                    return false;
                }

                return HasReturnDocumentation(root, symbol) && HasValueDocumentation(root, symbol);
            }
            catch (XmlException)
            {
                return false;
            }
        }
        finally
        {
            _ = visited.Remove(symbol);
        }
    }

    private static XElement LoadDocumentationXml(string xml)
    {
        var settings = new XmlReaderSettings
        {
            DtdProcessing = DtdProcessing.Prohibit,
            XmlResolver = null,
            MaxCharactersInDocument = 1_000_000,
        };

        using var stringReader = new StringReader(xml);
        using XmlReader reader = XmlReader.Create(stringReader, settings);
        return XElement.Load(reader, LoadOptions.None);
    }

    private static bool HasCompleteInheritdocSource(
        ISymbol symbol,
        XElement inheritdoc,
        Compilation compilation,
        ISet<ISymbol> visited,
        CancellationToken cancellationToken)
    {
        string? cref = (string?)inheritdoc.Attribute("cref");
        IEnumerable<ISymbol> sources = string.IsNullOrWhiteSpace(cref)
            ? GetInheritdocSources(symbol)
            : GetNamedInheritdocSource(cref!, compilation);

        return sources.Any(source => HasCompleteDocumentation(source, Array.Empty<string>(), compilation, visited, cancellationToken));
    }

    private static IEnumerable<ISymbol> GetNamedInheritdocSource(string cref, Compilation compilation)
    {
        ISymbol? source = DocumentationCommentId.GetFirstSymbolForReferenceId(cref, compilation);
        if (source is not null)
        {
            yield return source;
        }
    }

    private static IEnumerable<ISymbol> GetInheritdocSources(ISymbol symbol)
    {
        if (symbol is INamedTypeSymbol type)
        {
            foreach (ISymbol source in GetTypeInheritdocSources(type))
            {
                yield return source;
            }
        }

        foreach (ISymbol source in GetMemberInheritdocSources(symbol))
        {
            yield return source;
        }
    }

    private static IEnumerable<ISymbol> GetTypeInheritdocSources(INamedTypeSymbol type)
    {
        if (type.BaseType is { SpecialType: not SpecialType.System_Object } baseType)
        {
            yield return baseType;
        }

        foreach (INamedTypeSymbol @interface in type.AllInterfaces)
        {
            yield return @interface;
        }
    }

    private static IEnumerable<ISymbol> GetMemberInheritdocSources(ISymbol symbol)
    {
        if (symbol is IMethodSymbol method)
        {
            if (method.OverriddenMethod is not null)
            {
                yield return method.OverriddenMethod;
            }

            foreach (IMethodSymbol implementation in method.ExplicitInterfaceImplementations)
            {
                yield return implementation;
            }
        }

        if (symbol is IPropertySymbol property)
        {
            if (property.OverriddenProperty is not null)
            {
                yield return property.OverriddenProperty;
            }

            foreach (IPropertySymbol implementation in property.ExplicitInterfaceImplementations)
            {
                yield return implementation;
            }
        }

        if (symbol is IEventSymbol @event)
        {
            if (@event.OverriddenEvent is not null)
            {
                yield return @event.OverriddenEvent;
            }

            foreach (IEventSymbol implementation in @event.ExplicitInterfaceImplementations)
            {
                yield return implementation;
            }
        }

        INamedTypeSymbol? containingType = symbol.ContainingType;
        if (containingType is null)
        {
            yield break;
        }

        foreach (INamedTypeSymbol @interface in containingType.AllInterfaces)
        {
            foreach (ISymbol interfaceMember in @interface.GetMembers())
            {
                ISymbol? implementation = containingType.FindImplementationForInterfaceMember(interfaceMember);
                if (SymbolEqualityComparer.Default.Equals(implementation, symbol)
                    || (containingType.TypeKind == TypeKind.Interface && IsMatchingInterfaceMember(symbol, interfaceMember)))
                {
                    yield return interfaceMember;
                }
            }
        }
    }

    private static bool IsMatchingInterfaceMember(ISymbol member, ISymbol contractMember)
    {
        return member.Kind == contractMember.Kind
        && string.Equals(member.Name, contractMember.Name, StringComparison.Ordinal)
        && member switch
        {
            IMethodSymbol method when contractMember is IMethodSymbol contractMethod =>
                method.Arity == contractMethod.Arity
                && method.Parameters.Length == contractMethod.Parameters.Length
                && method.Parameters.Zip(contractMethod.Parameters, static (parameter, contractParameter) =>
                    SymbolEqualityComparer.Default.Equals(parameter.Type, contractParameter.Type)).All(static matches => matches),
            IPropertySymbol property when contractMember is IPropertySymbol contractProperty =>
                property.Parameters.Length == contractProperty.Parameters.Length
                && property.Parameters.Zip(contractProperty.Parameters, static (parameter, contractParameter) =>
                    SymbolEqualityComparer.Default.Equals(parameter.Type, contractParameter.Type)).All(static matches => matches),
            IEventSymbol when contractMember is IEventSymbol => true,
            _ => false,
        };
    }

    private static IEnumerable<string> GetTypeParameterNames(ISymbol symbol)
    {
        return symbol switch
        {
            INamedTypeSymbol type => type.TypeParameters.Select(static parameter => parameter.Name),
            IMethodSymbol method => method.TypeParameters.Select(static parameter => parameter.Name),
            _ => Array.Empty<string>(),
        };
    }

    private static IEnumerable<string> GetParameterNames(ISymbol symbol)
    {
        return symbol switch
        {
            IMethodSymbol method => method.Parameters.Select(static parameter => parameter.Name),
            IPropertySymbol property => property.Parameters.Select(static parameter => parameter.Name),
            INamedTypeSymbol { TypeKind: TypeKind.Delegate, DelegateInvokeMethod: not null } type => type.DelegateInvokeMethod.Parameters.Select(static parameter => parameter.Name),
            _ => Array.Empty<string>(),
        };
    }

    private static bool HasReturnDocumentation(XElement root, ISymbol symbol)
    {
        return (symbol is IMethodSymbol method && !method.ReturnsVoid)
            || (symbol is INamedTypeSymbol { TypeKind: TypeKind.Delegate, DelegateInvokeMethod.ReturnsVoid: false })
            ? HasExactlyOneText(root, "returns")
            : true;
    }

    private static bool HasValueDocumentation(XElement root, ISymbol symbol)
    {
        return symbol is IPropertySymbol property && property.Parameters.Length == 0
            ? HasExactlyOneText(root, "value")
            : true;
    }

    private static bool HasNamedElements(XElement root, string elementName, IEnumerable<string> names)
    {
        string[] expectedNames = names.Distinct(StringComparer.Ordinal).ToArray();
        XElement[] elements = root.Elements(elementName).ToArray();
        return elements.Length == expectedNames.Length
            && expectedNames.All(name => elements.Where(element => string.Equals((string?)element.Attribute("name"), name, StringComparison.Ordinal) && HasText(element)).Take(2).Count() == 1);
    }

    private static bool HasExactlyOneText(XElement root, string elementName)
    {
        XElement[] elements = root.Elements(elementName).ToArray();
        return elements.Length == 1 && HasText(elements[0]);
    }

    private static bool HasText(XElement element)
    {
        return !string.IsNullOrWhiteSpace(element.Value);
    }

    private static DiagnosticDescriptor CreateDescriptor(string id, string title, string message, string category, DiagnosticSeverity severity, string? description = null)
    {
        return new(id, title, message, category, severity, isEnabledByDefault: true, description: description ?? string.Empty, helpLinkUri: HelpLinkBase + id + ".md");
    }

    private sealed class ResultSymbols
    {
        internal ResultSymbols(Compilation compilation)
        {
            Result = compilation.GetTypeByMetadataName("FluentResults.Result");
            GenericResult = compilation.GetTypeByMetadataName("FluentResults.Result`1");
            Error = compilation.GetTypeByMetadataName("FluentResults.Error");
            IError = compilation.GetTypeByMetadataName("FluentResults.IError");
            ResultBaseOfT = compilation.GetTypeByMetadataName("FluentResults.ResultBase`1");
            ResultExtensions = compilation.GetTypeByMetadataName("FluentResults.Extensions.ResultExtensions");
            TaskOfT = compilation.GetTypeByMetadataName("System.Threading.Tasks.Task`1");
            ValueTaskOfT = compilation.GetTypeByMetadataName("System.Threading.Tasks.ValueTask`1");
            EnumerableOfT = compilation.GetTypeByMetadataName("System.Collections.Generic.IEnumerable`1");
            Exception = compilation.GetTypeByMetadataName("System.Exception");
        }

        internal INamedTypeSymbol? Result
        {
            get;
        }

        internal INamedTypeSymbol? GenericResult
        {
            get;
        }

        internal INamedTypeSymbol? Error
        {
            get;
        }

        internal INamedTypeSymbol? IError
        {
            get;
        }

        internal INamedTypeSymbol? ResultBaseOfT
        {
            get;
        }

        internal INamedTypeSymbol? ResultExtensions
        {
            get;
        }

        internal INamedTypeSymbol? TaskOfT
        {
            get;
        }

        internal INamedTypeSymbol? ValueTaskOfT
        {
            get;
        }

        internal INamedTypeSymbol? EnumerableOfT
        {
            get;
        }

        internal INamedTypeSymbol? Exception
        {
            get;
        }

        internal bool HasServiceReturnSymbols => Result is not null && GenericResult is not null && TaskOfT is not null && ValueTaskOfT is not null;

        internal bool IsApprovedServiceReturn(ITypeSymbol type)
        {
            if (IsResult(type))
            {
                return true;
            }

            if (type is not INamedTypeSymbol wrapper || wrapper.TypeArguments.Length != 1)
            {
                return false;
            }

            return (SymbolEqualityComparer.Default.Equals(wrapper.OriginalDefinition, TaskOfT)
                    || SymbolEqualityComparer.Default.Equals(wrapper.OriginalDefinition, ValueTaskOfT))
                && IsResult(wrapper.TypeArguments[0]);
        }

        internal bool IsResult(ITypeSymbol? type)
        {
            return SymbolEqualityComparer.Default.Equals(type, Result)
                || (type is INamedTypeSymbol named && SymbolEqualityComparer.Default.Equals(named.OriginalDefinition, GenericResult));
        }

        internal bool IsFluentResultsMethod(IMethodSymbol method)
        {
            if (Result is null || GenericResult is null || Error is null || IError is null)
            {
                return false;
            }

            return SymbolEqualityComparer.Default.Equals(method.ContainingType, Result)
                || SymbolEqualityComparer.Default.Equals(method.ContainingType.OriginalDefinition, ResultBaseOfT)
                || SymbolEqualityComparer.Default.Equals(method.ContainingType, ResultExtensions);
        }

        internal bool IsFailureBoundary(IMethodSymbol method)
        {
            if (SymbolEqualityComparer.Default.Equals(method.ContainingType, Result))
            {
                return string.Equals(method.Name, "Fail", StringComparison.Ordinal)
                    || string.Equals(method.Name, "OkIf", StringComparison.Ordinal)
                    || string.Equals(method.Name, "FailIf", StringComparison.Ordinal)
                    || string.Equals(method.Name, "FailIfNotEmpty", StringComparison.Ordinal);
            }

            if (SymbolEqualityComparer.Default.Equals(method.ContainingType.OriginalDefinition, ResultBaseOfT))
            {
                return string.Equals(method.Name, "WithError", StringComparison.Ordinal)
                    || string.Equals(method.Name, "WithErrors", StringComparison.Ordinal);
            }

            return SymbolEqualityComparer.Default.Equals(method.ContainingType, ResultExtensions)
                && string.Equals(method.Name, "OrFailIf", StringComparison.Ordinal);
        }

        internal bool IsFailureArgument(ITypeSymbol? type)
        {
            if (type is null)
            {
                return false;
            }

            if (type.SpecialType == SpecialType.System_String || IsError(type) || IsErrorCollection(type) || IsStringCollection(type))
            {
                return true;
            }

            return type is INamedTypeSymbol named
                && named.DelegateInvokeMethod is { ReturnType: ITypeSymbol returnType }
                && (returnType.SpecialType == SpecialType.System_String || IsError(returnType));
        }

        internal bool IsFailureValue(ITypeSymbol? type)
        {
            return IsError(type) || IsErrorCollection(type);
        }

        internal bool IsError(ITypeSymbol? type)
        {
            if (type is not INamedTypeSymbol named || Error is null)
            {
                return false;
            }

            for (INamedTypeSymbol? current = named; current is not null; current = current.BaseType)
            {
                if (SymbolEqualityComparer.Default.Equals(current, Error))
                {
                    return true;
                }
            }

            return SymbolEqualityComparer.Default.Equals(type, IError);
        }

        internal bool IsErrorCollection(ITypeSymbol? type)
        {
            if (type is IArrayTypeSymbol array)
            {
                return IsError(array.ElementType);
            }

            return type is INamedTypeSymbol named
                && named.AllInterfaces.Concat([named]).Any(@interface => @interface is INamedTypeSymbol candidate
                    && SymbolEqualityComparer.Default.Equals(candidate.OriginalDefinition, EnumerableOfT)
                    && candidate.TypeArguments.Length == 1
                    && IsError(candidate.TypeArguments[0]));
        }

        private bool IsStringCollection(ITypeSymbol type)
        {
            return type is INamedTypeSymbol named
                && named.AllInterfaces.Concat([named]).Any(@interface => @interface is INamedTypeSymbol candidate
                    && SymbolEqualityComparer.Default.Equals(candidate.OriginalDefinition, EnumerableOfT)
                    && candidate.TypeArguments.Length == 1
                    && candidate.TypeArguments[0].SpecialType == SpecialType.System_String);
        }
    }
}

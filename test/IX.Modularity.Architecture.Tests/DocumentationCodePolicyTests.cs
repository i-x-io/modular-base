using System.Globalization;
using System.Text.RegularExpressions;
using AwesomeAssertions;
using Markdig;
using Markdig.Syntax;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Xunit;

namespace IX.Modularity.Architecture.Tests;

/// <summary>Validates that selected C# documentation examples satisfy the repository's enforced policies.</summary>
public sealed partial class DocumentationCodePolicyTests
{
    private static readonly string s_repositoryRoot = FindRepositoryRoot();
    private static readonly MarkdownPipeline s_markdownPipeline = new MarkdownPipelineBuilder().Build();

    [Fact]
    public void Governed_CSharp_documentation_examples_conform_to_result_and_exception_policies()
    {
        List<PolicyViolation> violations = [];

        violations.AddRange(ValidateErrorCodesInDocument("docs/recipes/fastendpoints-validation-results.md"));
        violations.AddRange(ValidateErrorCodesInDocument("docs/recipes/efcore-npgsql-exception-mapping.md"));
        violations.AddRange(ValidateExceptionPropagationInDocument("docs/recipes/durable-mail-outbox.md"));
        violations.AddRange(ValidateDurableMailOutboxContracts("docs/recipes/durable-mail-outbox.md"));
        violations.AddRange(ValidateIxm1005Example("docs/architecture/diagnostics/ixm1005.md"));

        _ = violations.Should().BeEmpty("governed C# documentation examples are copied into analyzer-enforced projects");
    }

    [Fact]
    public void Error_code_validator_reports_concrete_error_without_a_direct_constant_code()
    {
        IReadOnlyCollection<PolicyViolation> violations = ValidateErrorCodes(
            "fixture.md",
            1,
            "public sealed class DuplicateSkuError : Error\n{\n    public string Code => \"sku_already_exists\";\n}");

        _ = violations.Should().ContainSingle()
            .Which.Message.Should().Contain("public const string Code");
    }

    [Fact]
    public void Error_code_validator_reports_constant_code_that_is_not_lowercase_snake_case()
    {
        IReadOnlyCollection<PolicyViolation> violations = ValidateErrorCodes(
            "fixture.md",
            1,
            "public sealed class DuplicateSkuError : Error\n{\n    public const string Code = \"SkuAlreadyExists\";\n}");

        _ = violations.Should().ContainSingle()
            .Which.Message.Should().Contain("lowercase snake case");
    }

    [Fact]
    public void Error_code_validator_reports_concrete_descendant_that_inherits_a_base_error_code()
    {
        IReadOnlyCollection<PolicyViolation> violations = ValidateErrorCodes(
            "fixture.md",
            1,
            "public abstract class CatalogError : Error\n{\n    public const string Code = \"catalog_error\";\n}\n\npublic sealed class DuplicateSkuError : CatalogError\n{\n}");

        _ = violations.Should().ContainSingle()
            .Which.Message.Should().Contain("DuplicateSkuError");
    }

    [Fact]
    public void Exception_catch_validator_reports_broad_catch_that_replaces_the_original_exception()
    {
        IReadOnlyCollection<PolicyViolation> violations = ValidateExceptionPropagation(
            "fixture.md",
            1,
            "try { Work(); }\ncatch (Exception exception)\n{\n    throw new InvalidOperationException(\"replacement\", exception);\n}");

        _ = violations.Should().ContainSingle()
            .Which.Message.Should().Contain("bare throw");
    }

    [Fact]
    public void Exception_catch_validator_reports_cancellation_catch_that_returns_a_replacement_exception()
    {
        IReadOnlyCollection<PolicyViolation> violations = ValidateExceptionPropagation(
            "fixture.md",
            1,
            "try { Work(); }\ncatch (OperationCanceledException exception)\n{\n    throw new InvalidOperationException(\"cancelled\", exception);\n}");

        _ = violations.Should().ContainSingle()
            .Which.Message.Should().Contain("OperationCanceledException");
    }

    [Fact]
    public void Exception_catch_validator_reports_nested_early_return_before_a_bare_rethrow()
    {
        IReadOnlyCollection<PolicyViolation> violations = ValidateExceptionPropagation(
            "fixture.md",
            1,
            "void Work()\n{\n    try { Throw(); }\n    catch (Exception)\n    {\n        if (shouldReturn)\n        {\n            return;\n        }\n\n        throw;\n    }\n}");

        _ = violations.Should().ContainSingle()
            .Which.Message.Should().Contain("bare throw");
    }

    [Fact]
    public void Ixm1005_validator_reports_service_operation_that_omits_result_wrapper()
    {
        IReadOnlyCollection<PolicyViolation> violations = ValidateIxm1005ReturnType(
            "fixture.md",
            1,
            "public interface IOperations\n{\n    ValueTask<OperationResult> ExecuteAsync(\n        OperationRequest request,\n        CancellationToken cancellationToken);\n}");

        _ = violations.Should().ContainSingle()
            .Which.Message.Should().Contain("ValueTask<Result<OperationResult>>");
    }

    [Fact]
    public void Durable_mail_outbox_validator_reports_outcome_unknown_write_without_cancellation_finalization()
    {
        IReadOnlyCollection<PolicyViolation> violations = ValidateOutcomeUnknownCancellationHandling(
            "fixture.md",
            1,
            "class Worker\n{\n    async Task RecordFailureAsync(MailDeliveryException exception, CancellationToken cancellationToken)\n    {\n        switch (exception.Disposition)\n        {\n            case DeliveryDisposition.OutcomeUnknown:\n                await outbox.MarkOutcomeUnknownAsync(mail.Id, mail.LeaseToken, exception.FailureCode, cancellationToken);\n                return;\n        }\n    }\n}");

        _ = violations.Should().ContainSingle()
            .Which.Message.Should().Contain("FinalizeUnknownOutcomeAsync");
    }

    [Fact]
    public void Durable_mail_outbox_validator_reports_mark_submitted_contract_with_a_raw_response_parameter()
    {
        IReadOnlyCollection<PolicyViolation> violations = ValidateMailOutboxContract(
            "fixture.md",
            1,
            "public interface IMailOutbox\n{\n    Task MarkSubmissionStartedAsync(Guid id, string leaseToken, CancellationToken cancellationToken);\n    Task MarkSubmittedAsync(Guid id, string leaseToken, string response, CancellationToken cancellationToken);\n}");

        _ = violations.Should().ContainSingle()
            .Which.Message.Should().Contain("Guid id, string leaseToken, and CancellationToken cancellationToken");
    }

    [Fact]
    public void Durable_mail_outbox_validator_reports_acknowledgement_outside_submit_cancellation_boundary()
    {
        IReadOnlyCollection<PolicyViolation> violations = ValidateSubmissionAcknowledgementCancellationBoundary(
            "fixture.md",
            1,
            "class Worker\n{\n    async Task ExecuteAsync()\n    {\n        foreach (OutboxMail mail in batch)\n        {\n            try\n            {\n                await transport.SubmitAsync(mail, stoppingToken);\n            }\n            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)\n            {\n                await FinalizeUnknownOutcomeAsync(mail);\n                throw;\n            }\n\n            await outbox.MarkSubmittedAsync(mail.Id, mail.LeaseToken, stoppingToken);\n        }\n    }\n}");

        _ = violations.Should().ContainSingle()
            .Which.Message.Should().Contain("same cancellation boundary");
    }

    [Fact]
    public void Durable_mail_outbox_validator_reports_submission_started_after_submit()
    {
        IReadOnlyCollection<PolicyViolation> violations = ValidateSubmissionAcknowledgementCancellationBoundary(
            "fixture.md",
            1,
            "class Worker\n{\n    async Task ExecuteAsync()\n    {\n        foreach (OutboxMail mail in batch)\n        {\n            try\n            {\n                await transport.SubmitAsync(mail, stoppingToken);\n                await outbox.MarkSubmissionStartedAsync(mail.Id, mail.LeaseToken, stoppingToken);\n                await outbox.MarkSubmittedAsync(mail.Id, mail.LeaseToken, stoppingToken);\n            }\n            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)\n            {\n                await FinalizeUnknownOutcomeAsync(mail);\n                throw;\n            }\n        }\n    }\n}");

        _ = violations.Should().ContainSingle()
            .Which.Message.Should().Contain("MarkSubmissionStartedAsync before SubmitAsync");
    }

    [Fact]
    public void Analyzer_release_records_ship_all_current_rules_and_leave_no_rules_unreleased()
    {
        string[] expectedRuleIds =
        [
            "IXM1001", "IXM1002", "IXM1003", "IXM1004", "IXM1005", "IXM2001", "IXM3001", "IXM3002", "IXM3003",
        ];

        AnalyzerRelease shippedRelease = Assert.Single(GetShippedReleases("src/IX.Modularity.Analyzers/AnalyzerReleases.Shipped.md"));
        string[] unshippedRuleIds = GetRuleIds("src/IX.Modularity.Analyzers/AnalyzerReleases.Unshipped.md");

        Assert.Equal("0.1.0", shippedRelease.Version);
        _ = shippedRelease.RuleIds.Should().BeEquivalentTo(expectedRuleIds, options => options.WithStrictOrdering());
        _ = unshippedRuleIds.Should().BeEmpty();
    }

    private static PolicyViolation[] ValidateErrorCodesInDocument(string relativePath)
    {
        List<PolicyViolation> violations = [];
        foreach (CSharpFence fence in EnumerateCSharpFences(relativePath))
        {
            violations.AddRange(ValidateErrorCodes(fence.RelativePath, fence.Number, fence.Code));
        }

        return [.. violations];
    }

    private static PolicyViolation[] ValidateExceptionPropagationInDocument(string relativePath)
    {
        List<PolicyViolation> violations = [];
        foreach (CSharpFence fence in EnumerateCSharpFences(relativePath))
        {
            violations.AddRange(ValidateExceptionPropagation(fence.RelativePath, fence.Number, fence.Code));
        }

        return [.. violations];
    }

    private static PolicyViolation[] ValidateIxm1005Example(string relativePath)
    {
        List<PolicyViolation> violations = [];
        foreach (CSharpFence fence in EnumerateCSharpFences(relativePath))
        {
            violations.AddRange(ValidateIxm1005ReturnType(fence.RelativePath, fence.Number, fence.Code));
        }

        return [.. violations];
    }

    private static PolicyViolation[] ValidateDurableMailOutboxContracts(string relativePath)
    {
        List<PolicyViolation> violations = [];
        bool foundOutboxContract = false;
        bool foundTransportContract = false;
        bool foundRecordFailure = false;
        bool foundWorker = false;

        foreach (CSharpFence fence in EnumerateCSharpFences(relativePath))
        {
            SyntaxNode root = CSharpSyntaxTree.ParseText(fence.Code, cancellationToken: CancellationToken.None).GetRoot(CancellationToken.None);
            foreach (InterfaceDeclarationSyntax declaration in root.DescendantNodes().OfType<InterfaceDeclarationSyntax>())
            {
                if (string.Equals(declaration.Identifier.ValueText, "IMailOutbox", StringComparison.Ordinal))
                {
                    foundOutboxContract = true;
                    violations.AddRange(ValidateMailOutboxContract(relativePath, fence.Number, declaration));
                }
                else if (string.Equals(declaration.Identifier.ValueText, "IMailTransport", StringComparison.Ordinal))
                {
                    foundTransportContract = true;
                    MethodDeclarationSyntax? submit = declaration.Members.OfType<MethodDeclarationSyntax>()
                        .FirstOrDefault(static method => string.Equals(method.Identifier.ValueText, "SubmitAsync", StringComparison.Ordinal));
                    if (submit is null || !IsNamedType(submit.ReturnType, "Task"))
                    {
                        violations.Add(CreateViolation(relativePath, fence.Number, declaration, "IMailTransport.SubmitAsync must return non-generic Task."));
                    }
                }
            }

            PolicyViolation[] outcomeUnknownViolations = ValidateOutcomeUnknownCancellationHandling(relativePath, fence.Number, fence.Code);
            foundRecordFailure |= outcomeUnknownViolations.Length > 0 || root.DescendantNodes().OfType<MethodDeclarationSyntax>().Any(static method => string.Equals(method.Identifier.ValueText, "RecordFailureAsync", StringComparison.Ordinal));
            violations.AddRange(outcomeUnknownViolations);

            PolicyViolation[] submissionBoundaryViolations = ValidateSubmissionAcknowledgementCancellationBoundary(relativePath, fence.Number, fence.Code);
            foundWorker |= submissionBoundaryViolations.Length > 0 || root.DescendantNodes().OfType<MethodDeclarationSyntax>().Any(static method => string.Equals(method.Identifier.ValueText, "ExecuteAsync", StringComparison.Ordinal));
            violations.AddRange(submissionBoundaryViolations);
        }

        if (!foundOutboxContract)
        {
            violations.Add(new PolicyViolation($"{relativePath}: durable recipe must declare IMailOutbox."));
        }

        if (!foundTransportContract)
        {
            violations.Add(new PolicyViolation($"{relativePath}: durable recipe must declare IMailTransport."));
        }

        if (!foundRecordFailure)
        {
            violations.Add(new PolicyViolation($"{relativePath}: durable recipe must declare RecordFailureAsync."));
        }

        if (!foundWorker)
        {
            violations.Add(new PolicyViolation($"{relativePath}: durable recipe must declare ExecuteAsync."));
        }

        return [.. violations];
    }

    private static PolicyViolation[] ValidateMailOutboxContract(string relativePath, int fenceNumber, string code)
    {
        SyntaxNode root = CSharpSyntaxTree.ParseText(code, cancellationToken: CancellationToken.None).GetRoot(CancellationToken.None);
        InterfaceDeclarationSyntax? outbox = root.DescendantNodes().OfType<InterfaceDeclarationSyntax>()
            .FirstOrDefault(static declaration => string.Equals(declaration.Identifier.ValueText, "IMailOutbox", StringComparison.Ordinal));
        return outbox is null
            ? [new PolicyViolation(string.Create(CultureInfo.InvariantCulture, $"{relativePath} fence {fenceNumber}: durable recipe must declare IMailOutbox."))]
            : ValidateMailOutboxContract(relativePath, fenceNumber, outbox);
    }

    private static PolicyViolation[] ValidateMailOutboxContract(string relativePath, int fenceNumber, InterfaceDeclarationSyntax outbox)
    {
        MethodDeclarationSyntax? markSubmitted = outbox.Members.OfType<MethodDeclarationSyntax>()
            .FirstOrDefault(static method => string.Equals(method.Identifier.ValueText, "MarkSubmittedAsync", StringComparison.Ordinal));
        MethodDeclarationSyntax? markSubmissionStarted = outbox.Members.OfType<MethodDeclarationSyntax>()
            .FirstOrDefault(static method => string.Equals(method.Identifier.ValueText, "MarkSubmissionStartedAsync", StringComparison.Ordinal));
        List<PolicyViolation> violations = [];
        if (markSubmitted is null || !HasRequiredOutboxTransitionSignature(markSubmitted))
        {
            violations.Add(CreateViolation(relativePath, fenceNumber, outbox, "IMailOutbox.MarkSubmittedAsync must accept exactly Guid id, string leaseToken, and CancellationToken cancellationToken."));
        }

        if (markSubmissionStarted is null || !HasRequiredOutboxTransitionSignature(markSubmissionStarted))
        {
            violations.Add(CreateViolation(relativePath, fenceNumber, outbox, "IMailOutbox.MarkSubmissionStartedAsync must accept exactly Guid id, string leaseToken, and CancellationToken cancellationToken."));
        }

        return [.. violations];
    }

    private static bool HasRequiredOutboxTransitionSignature(MethodDeclarationSyntax method)
    {
        SeparatedSyntaxList<ParameterSyntax> parameters = method.ParameterList.Parameters;
        return IsNamedType(method.ReturnType, "Task")
            && parameters.Count == 3
            && HasParameter(parameters[0], "Guid", "id")
            && HasParameter(parameters[1], "string", "leaseToken")
            && HasParameter(parameters[2], "CancellationToken", "cancellationToken");
    }

    private static bool HasParameter(ParameterSyntax parameter, string typeName, string parameterName)
    {
        return parameter.Type is not null
            && IsNamedType(parameter.Type, typeName)
            && string.Equals(parameter.Identifier.ValueText, parameterName, StringComparison.Ordinal);
    }

    private static PolicyViolation[] ValidateOutcomeUnknownCancellationHandling(string relativePath, int fenceNumber, string code)
    {
        SyntaxNode root = CSharpSyntaxTree.ParseText(code, cancellationToken: CancellationToken.None).GetRoot(CancellationToken.None);
        List<PolicyViolation> violations = [];
        foreach (MethodDeclarationSyntax recordFailure in root.DescendantNodes().OfType<MethodDeclarationSyntax>()
            .Where(static method => string.Equals(method.Identifier.ValueText, "RecordFailureAsync", StringComparison.Ordinal)))
        {
            foreach (SwitchSectionSyntax section in recordFailure.DescendantNodes().OfType<SwitchSectionSyntax>().Where(IsOutcomeUnknownSection))
            {
                if (!HasOutcomeUnknownCancellationFinalization(section))
                {
                    violations.Add(CreateViolation(relativePath, fenceNumber, section, "OutcomeUnknown handling must catch cancellation from MarkOutcomeUnknownAsync, invoke FinalizeUnknownOutcomeAsync with exception.FailureCode, and bare rethrow."));
                }
            }
        }

        return [.. violations];
    }

    private static PolicyViolation[] ValidateSubmissionAcknowledgementCancellationBoundary(string relativePath, int fenceNumber, string code)
    {
        SyntaxNode root = CSharpSyntaxTree.ParseText(code, cancellationToken: CancellationToken.None).GetRoot(CancellationToken.None);
        List<PolicyViolation> violations = [];
        foreach (MethodDeclarationSyntax executeAsync in root.DescendantNodes().OfType<MethodDeclarationSyntax>()
            .Where(static method => string.Equals(method.Identifier.ValueText, "ExecuteAsync", StringComparison.Ordinal)))
        {
            foreach (ForEachStatementSyntax loop in executeAsync.DescendantNodes().OfType<ForEachStatementSyntax>())
            {
                if (!loop.DescendantNodes().OfType<InvocationExpressionSyntax>().Any(IsSubmitInvocation))
                {
                    continue;
                }

                bool isProtectedBySameBoundary = loop.DescendantNodes().OfType<TryStatementSyntax>().Any(tryStatement =>
                    HasOrderedSubmissionTransitions(tryStatement)
                    && tryStatement.Catches.Any(HasSubmissionCancellationCatch));
                if (!isProtectedBySameBoundary)
                {
                    violations.Add(CreateViolation(relativePath, fenceNumber, loop, "MarkSubmissionStartedAsync before SubmitAsync and MarkSubmittedAsync after SubmitAsync must be in the same cancellation boundary that finalizes unknown outcome and bare rethrows."));
                }
            }
        }

        return [.. violations];
    }

    private static bool IsSubmitInvocation(InvocationExpressionSyntax invocation)
    {
        return invocation.Expression is MemberAccessExpressionSyntax memberAccess
            && string.Equals(memberAccess.Name.Identifier.ValueText, "SubmitAsync", StringComparison.Ordinal);
    }

    private static bool IsMarkSubmittedInvocation(InvocationExpressionSyntax invocation)
    {
        return invocation.Expression is MemberAccessExpressionSyntax memberAccess
            && string.Equals(memberAccess.Name.Identifier.ValueText, "MarkSubmittedAsync", StringComparison.Ordinal);
    }

    private static bool IsMarkSubmissionStartedInvocation(InvocationExpressionSyntax invocation)
    {
        return invocation.Expression is MemberAccessExpressionSyntax memberAccess
            && string.Equals(memberAccess.Name.Identifier.ValueText, "MarkSubmissionStartedAsync", StringComparison.Ordinal);
    }

    private static bool HasOrderedSubmissionTransitions(TryStatementSyntax tryStatement)
    {
        InvocationExpressionSyntax? submissionStarted = tryStatement.Block.DescendantNodes().OfType<InvocationExpressionSyntax>()
            .FirstOrDefault(IsMarkSubmissionStartedInvocation);
        InvocationExpressionSyntax? submitted = tryStatement.Block.DescendantNodes().OfType<InvocationExpressionSyntax>()
            .FirstOrDefault(IsMarkSubmittedInvocation);
        InvocationExpressionSyntax? submit = tryStatement.Block.DescendantNodes().OfType<InvocationExpressionSyntax>()
            .FirstOrDefault(IsSubmitInvocation);
        return submissionStarted is not null
            && submit is not null
            && submitted is not null
            && submissionStarted.SpanStart < submit.SpanStart
            && submit.SpanStart < submitted.SpanStart;
    }

    private static bool HasSubmissionCancellationCatch(CatchClauseSyntax catchClause)
    {
        return IsCancellationCatch(catchClause)
            && catchClause.Block.Statements.LastOrDefault() is ThrowStatementSyntax { Expression: null }
            && catchClause.Block.DescendantNodes().OfType<InvocationExpressionSyntax>().Any(IsUnknownOutcomeFinalizationInvocation);
    }

    private static bool IsUnknownOutcomeFinalizationInvocation(InvocationExpressionSyntax invocation)
    {
        return invocation.Expression is IdentifierNameSyntax identifier
            && string.Equals(identifier.Identifier.ValueText, "FinalizeUnknownOutcomeAsync", StringComparison.Ordinal);
    }

    private static bool IsOutcomeUnknownSection(SwitchSectionSyntax section)
    {
        return section.Labels.OfType<CaseSwitchLabelSyntax>().Any(static label =>
            label.Value is MemberAccessExpressionSyntax memberAccess
            && memberAccess.Expression is IdentifierNameSyntax identifier
            && string.Equals(identifier.Identifier.ValueText, "DeliveryDisposition", StringComparison.Ordinal)
            && string.Equals(memberAccess.Name.Identifier.ValueText, "OutcomeUnknown", StringComparison.Ordinal));
    }

    private static bool HasOutcomeUnknownCancellationFinalization(SwitchSectionSyntax section)
    {
        return section.DescendantNodes().OfType<TryStatementSyntax>().Any(tryStatement =>
            tryStatement.Block.DescendantNodes().OfType<InvocationExpressionSyntax>().Any(IsMarkOutcomeUnknownInvocation)
            && tryStatement.Catches.Any(HasCancellationFinalizationCatch));
    }

    private static bool IsMarkOutcomeUnknownInvocation(InvocationExpressionSyntax invocation)
    {
        return invocation.Expression is MemberAccessExpressionSyntax memberAccess
            && string.Equals(memberAccess.Name.Identifier.ValueText, "MarkOutcomeUnknownAsync", StringComparison.Ordinal);
    }

    private static bool HasCancellationFinalizationCatch(CatchClauseSyntax catchClause)
    {
        return IsCancellationCatch(catchClause)
            && catchClause.Block.Statements.LastOrDefault() is ThrowStatementSyntax { Expression: null }
            && catchClause.Block.DescendantNodes().OfType<InvocationExpressionSyntax>().Any(IsFailureCodeFinalizationInvocation);
    }

    private static bool IsFailureCodeFinalizationInvocation(InvocationExpressionSyntax invocation)
    {
        return invocation.Expression is IdentifierNameSyntax identifier
            && string.Equals(identifier.Identifier.ValueText, "FinalizeUnknownOutcomeAsync", StringComparison.Ordinal)
            && invocation.ArgumentList.Arguments.Any(static argument =>
                argument.Expression is MemberAccessExpressionSyntax memberAccess
                && memberAccess.Expression is IdentifierNameSyntax exceptionIdentifier
                && string.Equals(exceptionIdentifier.Identifier.ValueText, "exception", StringComparison.Ordinal)
                && string.Equals(memberAccess.Name.Identifier.ValueText, "FailureCode", StringComparison.Ordinal));
    }

    private static PolicyViolation[] ValidateErrorCodes(string relativePath, int fenceNumber, string code)
    {
        SyntaxTree tree = CSharpSyntaxTree.ParseText(code, cancellationToken: CancellationToken.None);
        List<PolicyViolation> violations = [];
        TypeDeclarationSyntax[] declarations = [.. tree.GetRoot(CancellationToken.None).DescendantNodes().OfType<TypeDeclarationSyntax>()];
        foreach (TypeDeclarationSyntax declaration in declarations)
        {
            if (!declaration.Modifiers.Any(SyntaxKind.AbstractKeyword)
                && IsErrorDescendant(declaration, declarations)
                && !HasValidDirectCodeConstant(declaration))
            {
                violations.Add(CreateViolation(relativePath, fenceNumber, declaration, $"Concrete Error descendant '{declaration.Identifier.ValueText}' must directly declare public const string Code with a lowercase snake case value."));
            }
        }

        return [.. violations];
    }

    private static PolicyViolation[] ValidateExceptionPropagation(string relativePath, int fenceNumber, string code)
    {
        SyntaxTree tree = CSharpSyntaxTree.ParseText(code, cancellationToken: CancellationToken.None);
        List<PolicyViolation> violations = [];
        foreach (CatchClauseSyntax catchClause in tree.GetRoot(CancellationToken.None).DescendantNodes().OfType<CatchClauseSyntax>())
        {
            if (IsBroadOrCancellationCatch(catchClause)
                && (!EndsWithBareRethrow(catchClause.Block) || HasForbiddenCatchExit(catchClause.Block)))
            {
                string message = IsCancellationCatch(catchClause)
                    ? "OperationCanceledException catch must terminate with a bare throw."
                    : "Untyped or Exception catch must terminate with a bare throw.";
                violations.Add(CreateViolation(relativePath, fenceNumber, catchClause, message));
            }
        }

        return [.. violations];
    }

    private static PolicyViolation[] ValidateIxm1005ReturnType(string relativePath, int fenceNumber, string code)
    {
        MethodDeclarationSyntax[] methods = GetExecuteAsyncMethods(code);
        List<PolicyViolation> violations = [];
        if (methods.Length != 1)
        {
            violations.Add(new PolicyViolation(string.Create(CultureInfo.InvariantCulture, $"{relativePath} fence {fenceNumber}: IXM1005 example must declare exactly one ExecuteAsync member.")));
        }
        else if (!IsRequiredIxm1005ReturnType(methods[0].ReturnType))
        {
            violations.Add(CreateViolation(relativePath, fenceNumber, methods[0].ReturnType, "IXM1005 ExecuteAsync example must return ValueTask<Result<OperationResult>>."));
        }

        return [.. violations];
    }

    private static CSharpFence[] EnumerateCSharpFences(string relativePath)
    {
        string path = GetSafeRepositoryPath(relativePath);
        MarkdownDocument document = Markdown.Parse(File.ReadAllText(path), s_markdownPipeline);

        List<CSharpFence> fences = [];
        int number = 0;
        foreach (FencedCodeBlock block in document.Descendants<FencedCodeBlock>())
        {
            if (IsCSharpFence(block.Info ?? string.Empty))
            {
                fences.Add(new CSharpFence(relativePath, ++number, block.Lines.ToString()));
            }
        }

        return [.. fences];
    }

    private static bool IsCSharpFence(string info)
    {
        string language = info.Split((char[]?)null, 2, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? string.Empty;
        return string.Equals(language, "csharp", StringComparison.OrdinalIgnoreCase)
            || string.Equals(language, "cs", StringComparison.OrdinalIgnoreCase);
    }

    private static MethodDeclarationSyntax[] GetExecuteAsyncMethods(string code)
    {
        SyntaxTree tree = CSharpSyntaxTree.ParseText(code, cancellationToken: CancellationToken.None);
        MethodDeclarationSyntax[] methods = [.. tree.GetRoot(CancellationToken.None).DescendantNodes().OfType<MethodDeclarationSyntax>()
            .Where(static method => string.Equals(method.Identifier.ValueText, "ExecuteAsync", StringComparison.Ordinal))];
        if (methods.Length > 0)
        {
            return methods;
        }

        SyntaxTree wrappedTree = CSharpSyntaxTree.ParseText($"public interface DocumentationSnippet\n{{\n{code}\n}}", cancellationToken: CancellationToken.None);
        return [.. wrappedTree.GetRoot(CancellationToken.None).DescendantNodes().OfType<MethodDeclarationSyntax>()
            .Where(static method => string.Equals(method.Identifier.ValueText, "ExecuteAsync", StringComparison.Ordinal))];
    }

    private static bool IsErrorDescendant(TypeDeclarationSyntax declaration, IReadOnlyCollection<TypeDeclarationSyntax> declarations)
    {
        HashSet<string> visitedTypes = new(StringComparer.Ordinal);
        return IsErrorDescendant(declaration, declarations, visitedTypes);
    }

    private static bool IsErrorDescendant(TypeDeclarationSyntax declaration, IReadOnlyCollection<TypeDeclarationSyntax> declarations, ISet<string> visitedTypes)
    {
        if (!visitedTypes.Add(declaration.Identifier.ValueText))
        {
            return false;
        }

        foreach (BaseTypeSyntax baseType in declaration.BaseList?.Types ?? [])
        {
            if (IsNamedType(baseType.Type, "Error"))
            {
                return true;
            }

            string? baseTypeName = GetTypeName(baseType.Type);
            TypeDeclarationSyntax? localBaseType = declarations.FirstOrDefault(candidate => string.Equals(candidate.Identifier.ValueText, baseTypeName, StringComparison.Ordinal));
            if (localBaseType is not null && IsErrorDescendant(localBaseType, declarations, visitedTypes))
            {
                return true;
            }
        }

        return false;
    }

    private static bool HasValidDirectCodeConstant(TypeDeclarationSyntax declaration)
    {
        return declaration.Members.OfType<FieldDeclarationSyntax>().Any(field =>
            field.Modifiers.Any(SyntaxKind.PublicKeyword)
            && field.Modifiers.Any(SyntaxKind.ConstKeyword)
            && IsNamedType(field.Declaration.Type, "string")
            && field.Declaration.Variables.Any(variable =>
                string.Equals(variable.Identifier.ValueText, "Code", StringComparison.Ordinal)
                && variable.Initializer?.Value is LiteralExpressionSyntax literal
                && literal.IsKind(SyntaxKind.StringLiteralExpression)
                && LowercaseSnakeCaseRegex.IsMatch(literal.Token.ValueText)));
    }

    private static bool IsBroadOrCancellationCatch(CatchClauseSyntax catchClause)
    {
        return catchClause.Declaration is null || IsNamedType(catchClause.Declaration.Type, "Exception") || IsCancellationCatch(catchClause);
    }

    private static bool IsCancellationCatch(CatchClauseSyntax catchClause)
    {
        return catchClause.Declaration is not null && IsNamedType(catchClause.Declaration.Type, "OperationCanceledException");
    }

    private static bool EndsWithBareRethrow(BlockSyntax block)
    {
        return block.Statements.LastOrDefault() is ThrowStatementSyntax { Expression: null };
    }

    private static bool HasForbiddenCatchExit(BlockSyntax block)
    {
        return block.DescendantNodes().Any(static node => node is ReturnStatementSyntax
            or BreakStatementSyntax
            or ContinueStatementSyntax
            or GotoStatementSyntax
            or ThrowExpressionSyntax
            or ThrowStatementSyntax { Expression: not null });
    }

    private static bool IsRequiredIxm1005ReturnType(TypeSyntax returnType)
    {
        return returnType is GenericNameSyntax
        {
            Identifier.ValueText: var valueTaskName,
            TypeArgumentList.Arguments: [GenericNameSyntax
            {
                Identifier.ValueText: "Result",
                TypeArgumentList.Arguments: [TypeSyntax resultArgument],
            }],
        } && string.Equals(valueTaskName, "ValueTask", StringComparison.Ordinal)
            && IsNamedType(resultArgument, "OperationResult");
    }

    private static bool IsNamedType(TypeSyntax type, string expectedName)
    {
        return string.Equals(GetTypeName(type), expectedName, StringComparison.Ordinal);
    }

    private static string? GetTypeName(TypeSyntax type)
    {
        return type switch
        {
            IdentifierNameSyntax identifier => identifier.Identifier.ValueText,
            PredefinedTypeSyntax predefined => predefined.Keyword.ValueText,
            QualifiedNameSyntax qualified => qualified.Right.Identifier.ValueText,
            AliasQualifiedNameSyntax aliasQualified => aliasQualified.Name.Identifier.ValueText,
            _ => null,
        };
    }

    private static PolicyViolation CreateViolation(string relativePath, int fenceNumber, SyntaxNode node, string message)
    {
        int line = node.GetLocation().GetLineSpan().StartLinePosition.Line + 1;
        return new PolicyViolation(string.Create(CultureInfo.InvariantCulture, $"{relativePath} fence {fenceNumber}, code line {line}: {message}"));
    }

    private static string GetSafeRepositoryPath(string relativePath)
    {
        string path = Path.GetFullPath(Path.Combine(s_repositoryRoot, relativePath));
        string rootWithSeparator = s_repositoryRoot.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
        return path.StartsWith(rootWithSeparator, StringComparison.Ordinal)
            && File.Exists(path)
            && !File.GetAttributes(path).HasFlag(FileAttributes.ReparsePoint)
            ? path
            : throw new InvalidDataException($"Documentation path '{relativePath}' is not a safe repository file.");
    }

    private static AnalyzerRelease[] GetShippedReleases(string relativePath)
    {
        List<AnalyzerRelease> releases = [];
        string? version = null;
        List<string> ruleIds = [];

        foreach (string line in File.ReadAllText(GetSafeRepositoryPath(relativePath)).Split('\n', StringSplitOptions.TrimEntries))
        {
            if (line.StartsWith("## Release ", StringComparison.Ordinal))
            {
                if (version is not null)
                {
                    releases.Add(new AnalyzerRelease(version, [.. ruleIds]));
                    ruleIds.Clear();
                }

                version = line["## Release ".Length..];
            }
            else if (line.StartsWith("IXM", StringComparison.Ordinal))
            {
                ruleIds.Add(line.Split('|', 2, StringSplitOptions.TrimEntries)[0]);
            }
        }

        if (version is not null)
        {
            releases.Add(new AnalyzerRelease(version, [.. ruleIds]));
        }

        return [.. releases];
    }

    private static string[] GetRuleIds(string relativePath)
    {
        return [.. File.ReadAllText(GetSafeRepositoryPath(relativePath)).Split('\n', StringSplitOptions.TrimEntries)
            .Where(static line => line.StartsWith("IXM", StringComparison.Ordinal))
            .Select(static line => line.Split('|', 2, StringSplitOptions.TrimEntries)[0])];
    }

    private static string FindRepositoryRoot()
    {
        for (DirectoryInfo? directory = new(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
        {
            if (File.Exists(Path.Combine(directory.FullName, "IX.Modularity.slnx")))
            {
                return directory.FullName;
            }
        }

        throw new DirectoryNotFoundException("Could not locate the repository root containing IX.Modularity.slnx.");
    }

    [GeneratedRegex("^[a-z][a-z0-9]*(?:_[a-z0-9]+)*$", RegexOptions.CultureInvariant | RegexOptions.NonBacktracking)]
    private static partial Regex LowercaseSnakeCaseRegex
    {
        get;
    }

    private sealed record CSharpFence(string RelativePath, int Number, string Code);

    private sealed record PolicyViolation(string Message);

    private sealed record AnalyzerRelease(string Version, string[] RuleIds);
}

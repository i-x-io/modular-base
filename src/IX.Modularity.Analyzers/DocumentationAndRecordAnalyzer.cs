using System;
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

    /// <inheritdoc />
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
    [
        s_publicDataObjectDocumentation,
        s_publicInterfaceDocumentation,
        s_interfaceMemberDocumentation,
        s_serviceTypeDocumentation,
        s_serviceMemberDocumentation,
        s_preferRecord,
    ];

    /// <inheritdoc />
    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSymbolAction(AnalyzeNamedType, SymbolKind.NamedType);
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

    private static DiagnosticDescriptor CreateDescriptor(string id, string title, string message, string category, DiagnosticSeverity severity)
    {
        return new(id, title, message, category, severity, isEnabledByDefault: true, helpLinkUri: HelpLinkBase + id + ".md");
    }
}

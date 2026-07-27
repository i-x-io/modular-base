using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Markdig;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;
using Xunit;

namespace IX.Modularity.Architecture.Tests;

/// <summary>Validates the offline Markdown documentation contract for this repository.</summary>
public sealed partial class DocumentationIntegrityTests
{
    private static readonly string s_repositoryRoot = FindRepositoryRoot();
    private static readonly HashSet<string> s_repositoryPaths = EnumerateRepositoryPaths(s_repositoryRoot)
        .ToHashSet(StringComparer.Ordinal);
    private static readonly MarkdownPipeline s_markdownPipeline = new MarkdownPipelineBuilder().Build();

    private static readonly string[] s_architecturePages =
    [
        "design-principles.md",
        "boundaries-and-dependencies.md",
        "domain-modeling.md",
        "type-system-and-data-modeling.md",
        "library-public-api-and-evolution.md",
        "performance-and-resource-management.md",
        "observability-and-operability.md",
        "documentation-testing-and-quality.md",
    ];

    private static readonly string[] s_packageGuideHeadings =
    [
        "Catalog entry",
        "Decision and scope",
        "Recommended registration and use",
        "Enterprise implementation guidance",
        "Integration with the catalog",
        "Security, performance, AOT, trimming, and operations",
        "Avoid",
        "Verification checklist",
        "Sources",
    ];

    [Fact]
    public void Governed_markdown_documents_have_one_h1_and_resolvable_links()
    {
        IReadOnlyDictionary<string, MarkdownFile> files = LoadMarkdownFiles();

        foreach (MarkdownFile file in files.Values)
        {
            _ = Assert.Single(file.Headings, static heading => heading.Level == 1);
            AssertAllLinksResolve(file, files);
        }
    }

    [Fact]
    public void Documentation_indexes_describe_the_governed_hierarchy_once()
    {
        IReadOnlyDictionary<string, MarkdownFile> files = LoadMarkdownFiles();
        MarkdownFile repositoryIndex = GetFile(files, "docs/README.md");
        MarkdownFile architectureIndex = GetFile(files, "docs/architecture/README.md");
        MarkdownFile terminologyHub = GetFile(files, "docs/architecture/terminology.md");

        foreach (string topLevelDocument in new[] { "architecture/README.md", "packages/README.md" })
        {
            Assert.Equal(1, CountLinksTo(repositoryIndex, topLevelDocument));
        }

        string[] governedArchitecturePages =
        [
            .. EnumerateFilesSafely(Path.Combine(s_repositoryRoot, "docs", "architecture"), "*.md")
            .Select(ToRepositoryRelativePath)
            .Where(static path => !string.Equals(path, "docs/architecture/README.md", StringComparison.Ordinal)),
        ];

        foreach (string architecturePage in governedArchitecturePages)
        {
            Assert.Equal(1, CountLinksTo(architectureIndex, architecturePage["docs/architecture/".Length..]));
        }

        Assert.Equal(governedArchitecturePages.Length, architectureIndex.Links.Count(link => IsLocalMarkdownLink(link.Destination)));

        IReadOnlyCollection<string> deepPagePaths = s_architecturePages
            .Select(page => "docs/architecture/" + page)
            .ToArray();
        List<string> canonicalAnchors = GetCanonicalDefinitionAnchors(files, deepPagePaths);

        Assert.NotEmpty(canonicalAnchors);
        Assert.Equal(canonicalAnchors.Count, canonicalAnchors.Distinct(StringComparer.Ordinal).Count());

        IReadOnlyCollection<MarkdownLink> termLinks = terminologyHub.Links
            .Where(link => HasDeepArchitectureFragment(link, deepPagePaths))
            .ToArray();
        Assert.NotEmpty(termLinks);
        Assert.Equal(termLinks.Count, termLinks.Select(static link => link.Destination).Distinct(StringComparer.Ordinal).Count());
        Assert.Equal(termLinks.Count, termLinks.Select(static link => link.Text).Distinct(StringComparer.Ordinal).Count());

        foreach (string anchor in canonicalAnchors)
        {
            Assert.Contains(termLinks, link => string.Equals(GetFragment(link.Destination), anchor, StringComparison.Ordinal));
        }

        foreach (MarkdownLink termLink in termLinks)
        {
            Assert.Contains(GetFragment(termLink.Destination), canonicalAnchors, StringComparer.Ordinal);
        }
    }

    [Fact]
    public void Explicit_documentation_anchors_are_unique_and_terms_are_a_to_z()
    {
        IReadOnlyDictionary<string, MarkdownFile> files = LoadMarkdownFiles();
        MarkdownFile terminologyHub = GetFile(files, "docs/architecture/terminology.md");

        foreach (MarkdownFile file in files.Values)
        {
            Assert.Equal(file.ExplicitAnchors.Count, file.ExplicitAnchors.Distinct(StringComparer.Ordinal).Count());
        }

        IReadOnlyCollection<string> alphabet = terminologyHub.Headings
            .Where(static heading => heading.Level == 2 && heading.Text.Length == 1 && char.IsAsciiLetter(heading.Text[0]))
            .Select(static heading => heading.Text)
            .ToArray();

        Assert.Equal(Enumerable.Range('A', 26).Select(static letter => ((char)letter).ToString()), alphabet, StringComparer.Ordinal);
    }

    [Fact]
    public void Central_packages_have_one_indexed_guide_with_the_required_schema()
    {
        IReadOnlyDictionary<string, MarkdownFile> files = LoadMarkdownFiles();
        MarkdownFile packageIndex = GetFile(files, "docs/packages/README.md");
        IReadOnlyCollection<CentralPackage> packages = LoadCentralPackages();

        Assert.Equal(packages.Count, packages.Select(static package => package.Id).Distinct(StringComparer.Ordinal).Count());

        foreach (CentralPackage package in packages)
        {
            MarkdownLink indexLink = Assert.Single(packageIndex.Links, link => string.Equals(link.Text, package.Id, StringComparison.Ordinal));
            string guidePath = ResolveRelativePath(packageIndex.RelativePath, GetPathPart(indexLink.Destination));
            MarkdownFile guide = GetFile(files, guidePath);
            AssertPackageGuideSchema(guide, package.Id);
            Assert.Matches($"\\|\\s*\\[[^\\]]*{Regex.Escape(package.Id)}[^\\]]*\\]\\([^)]*\\)\\s*\\|\\s*`{Regex.Escape(package.Version)}`\\s*\\|", packageIndex.Content);
        }
    }

    [Fact]
    public void Produced_analyzer_package_guide_has_the_required_schema()
    {
        IReadOnlyDictionary<string, MarkdownFile> files = LoadMarkdownFiles();
        AssertPackageGuideSchema(GetFile(files, "docs/packages/ix-modularity-analyzers.md"), "IX.Modularity.Analyzers");
    }

    [Fact]
    public void Packable_projects_and_the_root_readme_are_documented()
    {
        IReadOnlyDictionary<string, MarkdownFile> files = LoadMarkdownFiles();
        MarkdownFile rootReadme = GetFile(files, "README.md");
        MarkdownFile packageIndex = GetFile(files, "docs/packages/README.md");

        foreach (MarkdownLink link in rootReadme.Links.Where(static link => IsLocalMarkdownLink(link.Destination)))
        {
            string resolvedPath = ResolveRelativePath(rootReadme.RelativePath, GetPathPart(link.Destination));
            _ = GetFile(files, resolvedPath);
        }

        foreach (string projectPath in EnumerateFilesSafely(s_repositoryRoot, "*.csproj"))
        {
            XElement project = ProjectXmlDocumentLoader.Load(projectPath).Root ?? throw new InvalidDataException($"Project '{projectPath}' has no root element.");
            if (!IsPackable(project))
            {
                continue;
            }

            string packageId = GetProjectProperty(project, "PackageId") ?? Path.GetFileNameWithoutExtension(projectPath);
            MarkdownLink indexLink = Assert.Single(packageIndex.Links, link => string.Equals(link.Text, packageId, StringComparison.Ordinal));
            string guidePath = ResolveRelativePath(packageIndex.RelativePath, GetPathPart(indexLink.Destination));
            _ = GetFile(files, guidePath);
        }
    }

    [Fact]
    public void Analyzer_diagnostic_help_links_and_documentation_contracts_are_complete()
    {
        string analyzerSourcePath = Path.Combine(s_repositoryRoot, "src", "IX.Modularity.Analyzers", "DocumentationAndRecordAnalyzer.cs");
        EnsureRepositoryPathIsSafe(analyzerSourcePath, "analyzer source");
        string analyzerSource = File.ReadAllText(analyzerSourcePath);
        string[] diagnosticIds = [.. DescriptorIdRegex.Matches(analyzerSource)
            .Select(static match => match.Groups["id"].Value)
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)];

        Assert.Equal(6, diagnosticIds.Length);
        Assert.Equal(6, DescriptorIdRegex.Count(analyzerSource));
        Assert.True(HelpLinkFactoryRegex.IsMatch(analyzerSource), "Analyzer descriptors must derive each help link from HelpLinkBase, the diagnostic ID, and the .md suffix.");

        Match helpLinkBaseMatch = Assert.Single(HelpLinkBaseRegex.Matches(analyzerSource));
        string helpLinkBase = helpLinkBaseMatch.Groups["helpLinkBase"].Value;
        Assert.True(Uri.TryCreate(helpLinkBase, UriKind.Absolute, out Uri? baseUri) && string.Equals(baseUri.Scheme, Uri.UriSchemeHttps, StringComparison.Ordinal), "Analyzer help link base must be an absolute HTTPS URL.");

        string sourceDiagnosticDirectory = Path.Combine(s_repositoryRoot, "src", "IX.Modularity.Analyzers", "docs", "analyzers", "diagnostics");
        foreach (string diagnosticId in diagnosticIds)
        {
            string helpLink = helpLinkBase + diagnosticId + ".md";
            Assert.True(Uri.TryCreate(helpLink, UriKind.Absolute, out Uri? uri) && string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.Ordinal), $"{diagnosticId}: help link '{helpLink}' must be an absolute HTTPS URL.");
            Assert.EndsWith($"/{diagnosticId}.md", uri.AbsolutePath, StringComparison.Ordinal);

            string sourceDiagnosticPath = Path.Combine(sourceDiagnosticDirectory, diagnosticId + ".md");
            EnsureRepositoryPathIsSafe(sourceDiagnosticPath, $"source diagnostic page for '{diagnosticId}'");
            Assert.True(File.Exists(sourceDiagnosticPath), $"{diagnosticId}: source diagnostic page is missing.");
        }

        Assert.Equal(diagnosticIds, GetDiagnosticIdsFromReleaseEntries(), StringComparer.Ordinal);
        Assert.Equal(diagnosticIds, GetDiagnosticIdsFromArchitecturePage("docs/architecture/analyzer-index.md"), StringComparer.Ordinal);
        Assert.Equal(diagnosticIds, GetDiagnosticIdsFromArchitecturePage("docs/architecture/analyzer-taxonomy.md"), StringComparer.Ordinal);
    }

    private static Dictionary<string, MarkdownFile> LoadMarkdownFiles()
    {
        IEnumerable<string> paths = EnumerateFilesSafely(Path.Combine(s_repositoryRoot, "docs"), "*.md")
            .Append(Path.Combine(s_repositoryRoot, "README.md"));

        return paths.ToDictionary(
            ToRepositoryRelativePath,
            ParseMarkdownFile,
            StringComparer.Ordinal);
    }

    private static MarkdownFile ParseMarkdownFile(string path)
    {
        string content = File.ReadAllText(path);
        MarkdownDocument document = Markdown.Parse(content, s_markdownPipeline);
        IReadOnlyCollection<MarkdownHeading> headings = document.Descendants<HeadingBlock>()
            .Select(static heading => new MarkdownHeading(heading.Level, GetInlineText(heading.Inline)))
            .ToArray();
        IReadOnlyCollection<MarkdownLink> links = document.Descendants<LinkInline>()
            .Where(static link => !link.IsImage && !string.IsNullOrWhiteSpace(link.Url))
            .Select(static link => new MarkdownLink(GetInlineText(link), link.Url!))
            .ToArray();
        IReadOnlyCollection<string> explicitAnchors = ExplicitAnchorRegex.Matches(content)
            .Select(static match => match.Groups["anchor"].Value)
            .ToArray();

        return new MarkdownFile(ToRepositoryRelativePath(path), content, headings, links, explicitAnchors);
    }

    private static void AssertAllLinksResolve(MarkdownFile source, IReadOnlyDictionary<string, MarkdownFile> files)
    {
        foreach (MarkdownLink link in source.Links)
        {
            if (Uri.TryCreate(link.Destination, UriKind.Absolute, out Uri? absoluteUri))
            {
                Assert.True(IsHttpUrl(absoluteUri), $"{source.RelativePath}: external link '{link.Destination}' must use http or https.");
                continue;
            }

            string pathPart = GetPathPart(link.Destination);
            string resolvedPath = string.IsNullOrEmpty(pathPart) ? source.RelativePath : ResolveRelativePath(source.RelativePath, pathPart);
            string? fragment = GetFragment(link.Destination);
            if (!resolvedPath.EndsWith(".md", StringComparison.OrdinalIgnoreCase))
            {
                Assert.True(RepositoryPathExistsWithOrdinalCase(resolvedPath), $"{source.RelativePath}: link '{link.Destination}' targets missing path '{resolvedPath}' or uses incorrect casing.");
                Assert.Null(fragment);
                continue;
            }

            MarkdownFile target = GetFile(files, resolvedPath);
            if (fragment is not null)
            {
                Assert.Contains(fragment, GetAnchors(target), StringComparer.Ordinal);
            }
        }
    }

    private static List<string> GetCanonicalDefinitionAnchors(IReadOnlyDictionary<string, MarkdownFile> files, IReadOnlyCollection<string> paths)
    {
        List<string> anchors = [];
        foreach (string path in paths)
        {
            MarkdownFile file = GetFile(files, path);
            int canonicalDefinitionsIndex = file.Headings.ToList().FindIndex(static heading => heading.Level == 2 && string.Equals(heading.Text, "Canonical definitions", StringComparison.Ordinal));
            Assert.True(canonicalDefinitionsIndex >= 0, $"{path}: missing '## Canonical definitions'.");

            IReadOnlyList<MarkdownHeading> headings = file.Headings.ToList();
            for (int index = canonicalDefinitionsIndex + 1; index < headings.Count && headings[index].Level > 2; index++)
            {
                string anchor = ToGitHubStyleAnchor(headings[index].Text);
                Assert.Contains(anchor, GetAnchors(file), StringComparer.Ordinal);
                anchors.Add(anchor);
            }
        }

        return anchors;
    }

    private static string[] GetAnchors(MarkdownFile file)
    {
        return [
        .. file.Headings
            .Select(static heading => ToGitHubStyleAnchor(heading.Text))
            .Concat(file.ExplicitAnchors)
            .Distinct(StringComparer.Ordinal),
    ];
    }

    private static int CountLinksTo(MarkdownFile source, string expectedRelativePath)
    {
        return source.Links.Count(link =>
            !Uri.TryCreate(link.Destination, UriKind.Absolute, out _)
            && string.Equals(ResolveRelativePath(source.RelativePath, GetPathPart(link.Destination)), Path.Combine(Path.GetDirectoryName(source.RelativePath) ?? string.Empty, expectedRelativePath).Replace('\\', '/').TrimStart('/'), StringComparison.Ordinal));
    }

    private static bool HasDeepArchitectureFragment(MarkdownLink link, IReadOnlyCollection<string> deepPagePaths)
    {
        if (Uri.TryCreate(link.Destination, UriKind.Absolute, out _))
        {
            return false;
        }

        string path = ResolveRelativePath("docs/architecture/terminology.md", GetPathPart(link.Destination));
        return GetFragment(link.Destination) is not null && deepPagePaths.Contains(path, StringComparer.Ordinal);
    }

    private static bool IsLocalMarkdownLink(string destination)
    {
        return !Uri.TryCreate(destination, UriKind.Absolute, out _) && GetPathPart(destination).EndsWith(".md", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsHttpUrl(Uri uri)
    {
        return string.Equals(uri.Scheme, Uri.UriSchemeHttp, StringComparison.Ordinal) || string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.Ordinal);
    }

    private static string GetPathPart(string destination)
    {
        int fragmentIndex = destination.IndexOf('#', StringComparison.Ordinal);
        int queryIndex = destination.IndexOf('?', StringComparison.Ordinal);
        int endIndex = new[] { fragmentIndex, queryIndex }.Where(static index => index >= 0).DefaultIfEmpty(destination.Length).Min();
        return Uri.UnescapeDataString(destination[..endIndex]);
    }

    private static string? GetFragment(string destination)
    {
        int fragmentIndex = destination.IndexOf('#', StringComparison.Ordinal);
        return fragmentIndex < 0 ? null : Uri.UnescapeDataString(destination[(fragmentIndex + 1)..]);
    }

    private static string ResolveRelativePath(string sourceRelativePath, string destinationPath)
    {
        string sourceDirectory = Path.GetDirectoryName(sourceRelativePath) ?? string.Empty;
        string candidate = Path.GetFullPath(Path.Combine(s_repositoryRoot, sourceDirectory, destinationPath));
        string rootWithSeparator = s_repositoryRoot.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
        Assert.StartsWith(rootWithSeparator, candidate, StringComparison.Ordinal);
        EnsureRepositoryPathIsSafe(candidate, $"Markdown link target '{destinationPath}' from '{sourceRelativePath}'");
        return ToRepositoryRelativePath(candidate);
    }

    private static void AssertPackageGuideSchema(MarkdownFile guide, string packageId)
    {
        Assert.Equal(packageId, Assert.Single(guide.Headings, static heading => heading.Level == 1).Text);
        Assert.Equal(s_packageGuideHeadings, guide.Headings.Where(static heading => heading.Level == 2).Select(static heading => heading.Text), StringComparer.Ordinal);
    }

    private static MarkdownFile GetFile(IReadOnlyDictionary<string, MarkdownFile> files, string relativePath)
    {
        Assert.True(files.TryGetValue(relativePath, out MarkdownFile? file), $"Expected documentation file '{relativePath}' does not exist.");
        return file ?? throw new InvalidDataException($"Expected documentation file '{relativePath}' does not exist.");
    }

    private static CentralPackage[] LoadCentralPackages()
    {
        return [
        .. ProjectXmlDocumentLoader.Load(Path.Combine(s_repositoryRoot, "Directory.Packages.props"))
            .Descendants()
            .Where(static element => string.Equals(element.Name.LocalName, "PackageVersion", StringComparison.Ordinal) || string.Equals(element.Name.LocalName, "GlobalPackageReference", StringComparison.Ordinal))
            .Select(static element => new CentralPackage(
                element.Attribute("Include")?.Value ?? throw new InvalidDataException("Central package entry has no Include attribute."),
                element.Attribute("Version")?.Value ?? throw new InvalidDataException("Central package entry has no Version attribute."))),
    ];
    }

    private static bool IsPackable(XElement project)
    {
        return bool.TryParse(GetProjectProperty(project, "IsPackable"), out bool isPackable) && isPackable;
    }

    private static string? GetProjectProperty(XElement project, string propertyName)
    {
        return project.Descendants()
        .FirstOrDefault(element => string.Equals(element.Name.LocalName, propertyName, StringComparison.Ordinal))?.Value.Trim();
    }

    private static string GetInlineText(ContainerInline? inline)
    {
        StringBuilder text = new();
        for (Inline? child = inline?.FirstChild; child is not null; child = child.NextSibling)
        {
            if (child is LiteralInline literal)
            {
                _ = text.Append(literal.Content);
            }
            else if (child is CodeInline code)
            {
                _ = text.Append(code.Content);
            }
            else if (child is ContainerInline container)
            {
                _ = text.Append(GetInlineText(container));
            }
        }

        return text.ToString();
    }

    private static string ToGitHubStyleAnchor(string heading)
    {
        return HeadingAnchorRegex.Replace(string.Concat(heading.Select(char.ToLowerInvariant)), "").Replace(' ', '-');
    }

    private static string ToRepositoryRelativePath(string path)
    {
        return Path.GetRelativePath(s_repositoryRoot, path).Replace('\\', '/');
    }

    private static bool RepositoryPathExistsWithOrdinalCase(string relativePath)
    {
        return s_repositoryPaths.Contains(relativePath);
    }

    private static string[] GetDiagnosticIdsFromReleaseEntries()
    {
        string releasePath = Path.Combine(s_repositoryRoot, "src", "IX.Modularity.Analyzers", "AnalyzerReleases.Shipped.md");
        EnsureRepositoryPathIsSafe(releasePath, "analyzer release entries");
        return GetDiagnosticIds(AnalyzerReleaseEntryRegex.Matches(File.ReadAllText(releasePath)));
    }

    private static string[] GetDiagnosticIdsFromArchitecturePage(string relativePath)
    {
        string path = Path.Combine(s_repositoryRoot, relativePath);
        EnsureRepositoryPathIsSafe(path, $"architecture documentation '{relativePath}'");
        return GetDiagnosticIds(ArchitectureDiagnosticEntryRegex.Matches(File.ReadAllText(path)));
    }

    private static string[] GetDiagnosticIds(MatchCollection matches)
    {
        return [.. matches
            .Select(static match => match.Groups["id"].Value)
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)];
    }

    private static IEnumerable<string> EnumerateRepositoryPaths(string root)
    {
        foreach (string entry in EnumerateFileSystemEntriesSafely(root))
        {
            yield return ToRepositoryRelativePath(entry);
        }
    }

    private static IEnumerable<string> EnumerateFilesSafely(string root, string searchPattern)
    {
        return EnumerateFileSystemEntriesSafely(root)
            .Where(path => path.EndsWith(searchPattern[1..], StringComparison.OrdinalIgnoreCase));
    }

    private static IEnumerable<string> EnumerateFileSystemEntriesSafely(string root)
    {
        EnsureRepositoryPathIsSafe(root, "repository content root");
        Stack<string> directories = new();
        directories.Push(root);

        while (directories.Count > 0)
        {
            string directory = directories.Pop();
            IEnumerable<string> entries;
            try
            {
                entries = Directory.EnumerateFileSystemEntries(directory).ToArray();
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
            {
                throw new InvalidDataException($"Unable to enumerate repository directory '{directory}'.", exception);
            }

            foreach (string entry in entries)
            {
                FileAttributes attributes = GetFileAttributesOrReject(entry, "repository content");
                if (attributes.HasFlag(FileAttributes.ReparsePoint))
                {
                    continue;
                }

                yield return entry;
                if (attributes.HasFlag(FileAttributes.Directory))
                {
                    directories.Push(entry);
                }
            }
        }
    }

    private static void EnsureRepositoryPathIsSafe(string path, string description)
    {
        string normalizedRoot = Path.GetFullPath(s_repositoryRoot);
        string normalizedPath = Path.GetFullPath(path);
        string relativePath = Path.GetRelativePath(normalizedRoot, normalizedPath);
        if (Path.IsPathRooted(relativePath)
            || string.Equals(relativePath, "..", StringComparison.Ordinal)
            || relativePath.StartsWith($"..{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
            || relativePath.StartsWith($"..{Path.AltDirectorySeparatorChar}", StringComparison.Ordinal))
        {
            throw new InvalidDataException($"{description} resolves outside the repository.");
        }

        FileAttributes rootAttributes = GetFileAttributesOrReject(normalizedRoot, description);
        if (rootAttributes.HasFlag(FileAttributes.ReparsePoint))
        {
            throw new InvalidDataException($"{description} repository root is a reparse point.");
        }

        string currentPath = normalizedRoot;
        foreach (string segment in relativePath.Split([Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar], StringSplitOptions.RemoveEmptyEntries))
        {
            currentPath = Path.Combine(currentPath, segment);
            FileAttributes attributes = GetFileAttributesOrReject(currentPath, description);
            if (attributes.HasFlag(FileAttributes.ReparsePoint))
            {
                throw new InvalidDataException($"{description} traverses reparse point '{currentPath}'.");
            }
        }
    }

    private static FileAttributes GetFileAttributesOrReject(string path, string description)
    {
        try
        {
            return File.GetAttributes(path);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            throw new InvalidDataException($"Unable to inspect {description} path '{path}'.", exception);
        }
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

    [GeneratedRegex("<a\\s+[^>]*\\bid\\s*=\\s*[\"'](?<anchor>[^\"']+)[\"'][^>]*>", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture | RegexOptions.NonBacktracking)]
    private static partial Regex ExplicitAnchorRegex
    {
        get;
    }

    [GeneratedRegex("[^a-z0-9 -]", RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture | RegexOptions.NonBacktracking)]
    private static partial Regex HeadingAnchorRegex
    {
        get;
    }

    [GeneratedRegex("HelpLinkBase\\s*=\\s*\"(?<helpLinkBase>[^\"]+)\"", RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture | RegexOptions.NonBacktracking)]
    private static partial Regex HelpLinkBaseRegex
    {
        get;
    }

    [GeneratedRegex("CreateDescriptor\\(\\s*\"(?<id>IXM[0-9]{4})\"", RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture | RegexOptions.NonBacktracking)]
    private static partial Regex DescriptorIdRegex
    {
        get;
    }

    [GeneratedRegex("helpLinkUri\\s*:\\s*HelpLinkBase\\s*\\+\\s*id\\s*\\+\\s*\"\\.md\"", RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture | RegexOptions.NonBacktracking)]
    private static partial Regex HelpLinkFactoryRegex
    {
        get;
    }

    [GeneratedRegex("^\\s*(?<id>IXM[0-9]{4})\\s*\\|", RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture | RegexOptions.Multiline | RegexOptions.NonBacktracking)]
    private static partial Regex AnalyzerReleaseEntryRegex
    {
        get;
    }

    [GeneratedRegex("^\\|\\s*`(?<id>IXM[0-9]{4})`\\s*\\|", RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture | RegexOptions.Multiline | RegexOptions.NonBacktracking)]
    private static partial Regex ArchitectureDiagnosticEntryRegex
    {
        get;
    }

    private sealed record MarkdownFile(string RelativePath, string Content, IReadOnlyCollection<MarkdownHeading> Headings, IReadOnlyCollection<MarkdownLink> Links, IReadOnlyCollection<string> ExplicitAnchors);

    private sealed record MarkdownHeading(int Level, string Text);

    private sealed record MarkdownLink(string Text, string Destination);

    private sealed record CentralPackage(string Id, string Version);
}

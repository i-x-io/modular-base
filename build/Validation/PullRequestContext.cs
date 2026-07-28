namespace ModularBase.Build.Validation;

internal sealed record PullRequestContext(
    int Number,
    string Title,
    string Body,
    string HeadBranch,
    string HeadRepository,
    string BaseRepository,
    string Author);

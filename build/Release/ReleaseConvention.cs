namespace ModularBase.Build.Release;

internal sealed record ReleaseConvention(string StableTitlePrefix, string TagPrefix)
{
    public static ReleaseConvention Default { get; } = new("RELEASE:", "v");
}

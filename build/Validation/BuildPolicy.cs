using ModularBase.Build.Release;

namespace ModularBase.Build.Validation;

[System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembers(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.All)]
internal sealed record BuildPolicy(ValidationPolicy Validation, ReleaseConvention Release)
{
    public static BuildPolicy Default
    {
        get;
    } = new(
        ValidationPolicy.Default,
        ReleaseConvention.Default);
}

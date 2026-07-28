using ModularBase.Build.Validation;

namespace ModularBase.Build.Tests;

public sealed class DependencyAuditParserTests
{
    [Fact]
    public void CountsDirectAndTransitiveFindingsAcrossFrameworks()
    {
        const string Json = "{\"projects\":[{\"frameworks\":["
            + "{\"topLevelPackages\":[{\"id\":\"Direct\"}],"
            + "\"transitivePackages\":[{\"id\":\"One\"},{\"id\":\"Two\"}]},"
            + "{\"topLevelPackages\":[],\"transitivePackages\":[]}]}]}";

        int count = DependencyAuditParser.CountFindings(Json);

        Assert.Equal(3, count);
    }

    [Fact]
    public void RejectsAnIncompatibleReportShape()
    {
        InvalidDataException exception = Assert.Throws<InvalidDataException>(
            () => DependencyAuditParser.CountFindings(/*lang=json,strict*/ "{ \"version\": 2 }"));

        Assert.Contains("projects array", exception.Message, StringComparison.Ordinal);
    }
}

using ModularBase.Build.Validation;

namespace ModularBase.Build.Tests;

public sealed class SbomValidatorTests
{
    [Fact]
    public void AcceptsOneNonEmptySbom()
    {
        string directory = CreateDirectory();
        try
        {
            string path = Path.Combine(directory, "bom.json");
            File.WriteAllText(path, /*lang=json,strict*/ "{\"components\":[{\"name\":\"IX.Modularity\"}]}");

            string result = SbomValidator.Validate(directory);

            Assert.Equal(path, result);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Theory]
    [InlineData(/*lang=json,strict*/ "{}")]
    [InlineData(/*lang=json,strict*/ "{\"components\":[]}")]
    public void RejectsAnEmptySbom(string json)
    {
        string directory = CreateDirectory();
        try
        {
            File.WriteAllText(Path.Combine(directory, "bom.json"), json);

            _ = Assert.Throws<InvalidDataException>(() => SbomValidator.Validate(directory));
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    private static string CreateDirectory()
    {
        string directory = Path.Combine(Path.GetTempPath(), $"modular-base-sbom-{Guid.NewGuid():N}");
        _ = Directory.CreateDirectory(directory);
        return directory;
    }
}

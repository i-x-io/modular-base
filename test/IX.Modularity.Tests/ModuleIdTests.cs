namespace IX.Modularity.Tests;

public sealed class ModuleIdTests
{
    [Theory]
    [InlineData("payments")]
    [InlineData("ix.modularity")]
    [InlineData("reporting-v2")]
    public void Parse_accepts_valid_identifiers(string value)
    {
        var identifier = ModuleId.Parse(value);

        Assert.Equal(value, identifier.Value);
        Assert.Equal(value, identifier.ToString());
    }

    [Theory]
    [InlineData("")]
    [InlineData("Payments")]
    [InlineData("payments_api")]
    [InlineData("payments..api")]
    [InlineData("1payments")]
    public void TryParse_rejects_invalid_identifiers(string value)
    {
        bool parsed = ModuleId.TryParse(value, out ModuleId identifier);

        Assert.False(parsed);
        Assert.Equal(default, identifier);
    }

    [Fact]
    public void Default_identifier_has_no_value()
    {
        ModuleId identifier = default;

        _ = Assert.Throws<InvalidOperationException>(() => identifier.Value);
    }
}

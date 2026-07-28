using System.Text;
using ModularBase.Build.Release;

namespace ModularBase.Build.Tests;

public sealed class HashingTests
{
    [Fact]
    public void ProducesUppercaseSha256()
    {
        string path = Path.GetTempFileName();
        try
        {
            File.WriteAllText(path, "modular-base", Encoding.UTF8);

            string hash = Hashing.Sha256(path);

            Assert.Equal("D2C77A2A0B93F8E73408498F4E3C5A9264CC1752F4013EC7F83CE40BC1A50BED", hash);
        }
        finally
        {
            File.Delete(path);
        }
    }
}

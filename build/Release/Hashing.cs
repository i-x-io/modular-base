using System.Security.Cryptography;

namespace ModularBase.Build.Release;

internal static class Hashing
{
    public static string Sha256(string path)
    {
        using FileStream stream = File.OpenRead(path);
        return Convert.ToHexString(SHA256.HashData(stream));
    }
}

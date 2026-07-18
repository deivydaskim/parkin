using System.Security.Cryptography;
using System.Text;

namespace Parkin.Api.Domain.ApiKeyAggregate;

// Pure BCL crypto only, no framework deps — safe for both Domain (Create) and
// Infrastructure (the X-Api-Key auth handler) to call.
public static class ApiKeySecret
{
  private const string KeyPrefix = "pk_live_";
  private const int RandomByteCount = 32;
  private const int DisplayPrefixLength = 12;

  public static (string RawKey, string DisplayPrefix, string Hash) Generate()
  {
    var randomPart = Base64UrlEncode(RandomNumberGenerator.GetBytes(RandomByteCount));
    var rawKey = KeyPrefix + randomPart;
    var displayPrefix = rawKey[..Math.Min(rawKey.Length, DisplayPrefixLength)];
    return (rawKey, displayPrefix, Hash(rawKey));
  }

  public static string Hash(string rawKey)
    => Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(rawKey)));

  private static string Base64UrlEncode(byte[] bytes)
    => Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
}

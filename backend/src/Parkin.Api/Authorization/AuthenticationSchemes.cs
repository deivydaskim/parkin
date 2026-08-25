namespace Parkin.Api.Authorization;

// Lives here, not on the handler options, so feature slices can opt into the scheme without
// referencing Infrastructure (forbidden by config.nsdepcop).
public static class AuthenticationSchemes
{
  public const string ApiKey = "ApiKey";
}

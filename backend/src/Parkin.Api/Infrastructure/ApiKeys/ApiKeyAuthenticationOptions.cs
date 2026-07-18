using Microsoft.AspNetCore.Authentication;

namespace Parkin.Api.Infrastructure.ApiKeys;

public class ApiKeyAuthenticationOptions : AuthenticationSchemeOptions
{
  public const string SchemeName = "ApiKey";
  public const string HeaderName = "X-Api-Key";
}

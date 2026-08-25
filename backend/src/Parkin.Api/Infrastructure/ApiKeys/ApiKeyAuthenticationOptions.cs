using Microsoft.AspNetCore.Authentication;
using Parkin.Api.Authorization;

namespace Parkin.Api.Infrastructure.ApiKeys;

public class ApiKeyAuthenticationOptions : AuthenticationSchemeOptions
{
  public const string SchemeName = AuthenticationSchemes.ApiKey;
  public const string HeaderName = "X-Api-Key";
}

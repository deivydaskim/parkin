using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Parkin.Api.Domain.ApiKeyAggregate;
using Parkin.Api.Infrastructure.Data;

namespace Parkin.Api.Infrastructure.ApiKeys;

public class ApiKeyAuthenticationHandler(
  IOptionsMonitor<ApiKeyAuthenticationOptions> options,
  ILoggerFactory loggerFactory,
  UrlEncoder encoder,
  AppDbContext dbContext)
  : AuthenticationHandler<ApiKeyAuthenticationOptions>(options, loggerFactory, encoder)
{
  protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
  {
    if (!Request.Headers.TryGetValue(ApiKeyAuthenticationOptions.HeaderName, out var headerValues))
    {
      return AuthenticateResult.NoResult();
    }

    var presentedKey = headerValues.ToString();
    if (string.IsNullOrWhiteSpace(presentedKey))
    {
      return AuthenticateResult.Fail("Missing API key.");
    }

    var hash = ApiKeySecret.Hash(presentedKey);
    var apiKey = await dbContext.ApiKeys
      .AsNoTracking()
      .SingleOrDefaultAsync(k => k.KeyHash == hash && k.Status == ApiKeyStatus.Active, Context.RequestAborted);

    if (apiKey is null)
    {
      return AuthenticateResult.Fail("Invalid or revoked API key.");
    }

    var claims = new[]
    {
      new Claim(ClaimTypes.NameIdentifier, apiKey.Id.Value.ToString()),
      new Claim("api_key_name", apiKey.Name)
    };
    var identity = new ClaimsIdentity(claims, Scheme.Name);
    var principal = new ClaimsPrincipal(identity);
    var ticket = new AuthenticationTicket(principal, Scheme.Name);
    return AuthenticateResult.Success(ticket);
  }
}

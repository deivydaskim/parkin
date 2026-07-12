using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;

namespace Parkin.Api.Configurations;

public static class AuthConfig
{
  public const string CorsPolicy = "spa";

  public static IServiceCollection AddAuthConfigs(
    this IServiceCollection services,
    IConfiguration configuration,
    IWebHostEnvironment environment)
  {
    // Identity.Application cookie is the default scheme so FastEndpoints authorization uses it.
    services.AddAuthentication(IdentityConstants.ApplicationScheme)
            .AddIdentityCookies();

    services.ConfigureApplicationCookie(options =>
    {
      options.Cookie.Name = "parkin.auth";
      options.Cookie.HttpOnly = true;
      options.ExpireTimeSpan = TimeSpan.FromHours(8);
      options.SlidingExpiration = true;

      // Prod: hardened cross-site defence (HTTPS-only, no cross-site sending).
      // Dev: the SPA (http://localhost:5173) and API share the http scheme, so
      // Strict+Secure would drop the cookie — relax to keep local dev working.
      if (environment.IsDevelopment())
      {
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
      }
      else
      {
        options.Cookie.SameSite = SameSiteMode.Strict;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
      }

      // API, not an MVC app: emit status codes instead of HTML redirects.
      options.Events.OnRedirectToLogin = context =>
      {
        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        return Task.CompletedTask;
      };
      options.Events.OnRedirectToAccessDenied = context =>
      {
        context.Response.StatusCode = StatusCodes.Status403Forbidden;
        return Task.CompletedTask;
      };
    });

    services.AddAuthorization();

    // Default revalidation interval is 30 minutes — too slow for "disable ends sessions
    // immediately" (A4 AC). A rotated security stamp (see UserFeatures/Disable) is only
    // checked on the next revalidation, so this interval bounds how stale a live cookie can be.
    services.Configure<SecurityStampValidatorOptions>(o => o.ValidationInterval = TimeSpan.FromSeconds(30));

    var allowedOrigins = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
    services.AddCors(options =>
    {
      options.AddPolicy(CorsPolicy, policy =>
        policy.WithOrigins(allowedOrigins)
              .AllowCredentials()
              .AllowAnyHeader()
              .AllowAnyMethod());
    });

    return services;
  }
}

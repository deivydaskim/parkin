namespace Parkin.Api.Domain.Services;

// Which pool an ALLOW-ed entry occupies; only meaningful on Allow, null on Deny.
public enum SessionPool
{
  General,
  Reserved
}

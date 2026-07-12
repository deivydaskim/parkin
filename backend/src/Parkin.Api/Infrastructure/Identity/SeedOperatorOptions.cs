namespace Parkin.Api.Infrastructure.Identity;

public class SeedOperatorOptions
{
  public const string SectionName = "SeedOperator";

  public string Email { get; set; } = string.Empty;

  public string Password { get; set; } = string.Empty;
}

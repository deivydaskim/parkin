using Microsoft.AspNetCore.Identity;

namespace Parkin.Api.Infrastructure.Identity;

public class ApplicationUser : IdentityUser<Guid>
{
  public string DisplayName { get; set; } = string.Empty;

  public UserStatus Status { get; set; } = UserStatus.Active;
}

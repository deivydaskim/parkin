using Microsoft.AspNetCore.Identity;
using Parkin.Api.Domain.StaffUsers;

namespace Parkin.Api.Infrastructure.Identity;

public class ApplicationUser : IdentityUser<Guid>
{
  public string DisplayName { get; set; } = string.Empty;

  public UserStatus Status { get; set; } = UserStatus.Active;
}

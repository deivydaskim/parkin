using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Parkin.Api.Domain.AuditAggregate;
using Parkin.Api.Domain.StaffUsers;
using Parkin.Api.Infrastructure.Data;

namespace Parkin.Api.Infrastructure.Identity;

public sealed class StaffUserService(UserManager<ApplicationUser> userManager, AppDbContext dbContext)
  : IStaffUserService
{
  public async Task<StaffUser?> FindByIdAsync(Guid userId, CancellationToken cancellationToken)
  {
    var user = await userManager.FindByIdAsync(userId.ToString());
    return user is null ? null : await ToStaffUserAsync(user);
  }

  public async Task<StaffUser?> FindByEmailAsync(string email, CancellationToken cancellationToken)
  {
    var user = await userManager.FindByEmailAsync(email);
    return user is null ? null : await ToStaffUserAsync(user);
  }

  public Task<int> CountActiveSystemAdminsAsync(CancellationToken cancellationToken)
    => (from user in dbContext.Users
        join userRole in dbContext.UserRoles on user.Id equals userRole.UserId
        join role in dbContext.Roles on userRole.RoleId equals role.Id
        where role.Name == StaffRoles.SystemAdmin && user.Status == UserStatus.Active
        select user.Id)
      .Distinct()
      .CountAsync(cancellationToken);

  public Task<Result<StaffUser>> CreateAsync(NewStaffUser newUser, AuditLogEntry audit,
    CancellationToken cancellationToken)
    => InTransactionAsync<StaffUser>(async () =>
    {
      var user = new ApplicationUser
      {
        Id = newUser.Id,
        UserName = newUser.Email,
        Email = newUser.Email,
        EmailConfirmed = true,
        DisplayName = newUser.DisplayName,
        Status = UserStatus.Active
      };

      var created = await userManager.CreateAsync(user, newUser.Password);
      if (!created.Succeeded) return Result<StaffUser>.Invalid(ToValidationErrors(created));

      var addedToRole = await SaveWithAuditAsync(audit, () => userManager.AddToRoleAsync(user, newUser.Role));
      if (!addedToRole.Succeeded) return Result<StaffUser>.Error(Describe(addedToRole));

      return ToStaffUser(user, [newUser.Role]);
    }, cancellationToken);

  public Task<Result<StaffUser>> ChangeRoleAsync(Guid userId, string role, AuditLogEntry audit,
    CancellationToken cancellationToken)
    => InTransactionAsync<StaffUser>(async () =>
    {
      var user = await userManager.FindByIdAsync(userId.ToString());
      if (user is null) return Result<StaffUser>.NotFound();

      var currentRoles = await userManager.GetRolesAsync(user);
      if (currentRoles.Count > 0)
      {
        var removed = await userManager.RemoveFromRolesAsync(user, currentRoles);
        if (!removed.Succeeded) return Result<StaffUser>.Error(Describe(removed));
      }

      var added = await SaveWithAuditAsync(audit, () => userManager.AddToRoleAsync(user, role));
      if (!added.Succeeded) return Result<StaffUser>.Error(Describe(added));

      return ToStaffUser(user, [role]);
    }, cancellationToken);

  public async Task<Result> DisableAsync(Guid userId, AuditLogEntry audit, CancellationToken cancellationToken)
  {
    var user = await userManager.FindByIdAsync(userId.ToString());
    if (user is null) return Result.NotFound();

    user.Status = UserStatus.Disabled;
    var updated = await SaveWithAuditAsync(audit, () => userManager.UpdateSecurityStampAsync(user));
    return updated.Succeeded ? Result.Success() : Result.Error(Describe(updated));
  }

  public async Task<Result> EnableAsync(Guid userId, AuditLogEntry audit, CancellationToken cancellationToken)
  {
    var user = await userManager.FindByIdAsync(userId.ToString());
    if (user is null) return Result.NotFound();

    user.Status = UserStatus.Active;
    var updated = await SaveWithAuditAsync(audit, () => userManager.UpdateAsync(user));
    return updated.Succeeded ? Result.Success() : Result.Error(Describe(updated));
  }

  private async Task<IdentityResult> SaveWithAuditAsync(AuditLogEntry audit, Func<Task<IdentityResult>> identityWrite)
  {
    var entry = dbContext.AuditLogEntries.Add(audit);
    var result = await identityWrite();
    if (!result.Succeeded) entry.State = EntityState.Detached;
    return result;
  }

  private async Task<Result<T>> InTransactionAsync<T>(Func<Task<Result<T>>> action, CancellationToken cancellationToken)
  {
    await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
    var result = await action();
    if (result.IsSuccess) await transaction.CommitAsync(cancellationToken);
    return result;
  }

  private async Task<StaffUser> ToStaffUserAsync(ApplicationUser user)
    => ToStaffUser(user, [.. await userManager.GetRolesAsync(user)]);

  private static StaffUser ToStaffUser(ApplicationUser user, IReadOnlyList<string> roles)
    => new(user.Id, user.Email ?? string.Empty, user.DisplayName, roles, user.Status);

  private static ValidationError[] ToValidationErrors(IdentityResult result)
    => result.Errors
      .Select(error => new ValidationError(
        error.Code.Contains("Password", StringComparison.OrdinalIgnoreCase) ? "Password" : "Email",
        error.Description))
      .ToArray();

  private static string Describe(IdentityResult result)
    => string.Join("; ", result.Errors.Select(error => error.Description));
}

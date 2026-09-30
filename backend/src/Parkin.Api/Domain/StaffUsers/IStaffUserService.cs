using Parkin.Api.Domain.AuditAggregate;

namespace Parkin.Api.Domain.StaffUsers;

public interface IStaffUserService
{
  Task<StaffUser?> FindByIdAsync(Guid userId, CancellationToken cancellationToken);

  Task<StaffUser?> FindByEmailAsync(string email, CancellationToken cancellationToken);

  Task<int> CountActiveSystemAdminsAsync(CancellationToken cancellationToken);

  Task<Result<StaffUser>> CreateAsync(NewStaffUser user, AuditLogEntry audit, CancellationToken cancellationToken);

  Task<Result<StaffUser>> ChangeRoleAsync(Guid userId, string role, AuditLogEntry audit, CancellationToken cancellationToken);

  Task<Result> DisableAsync(Guid userId, AuditLogEntry audit, CancellationToken cancellationToken);

  Task<Result> EnableAsync(Guid userId, AuditLogEntry audit, CancellationToken cancellationToken);
}

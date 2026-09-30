namespace Parkin.Api.Domain.StaffUsers;

public sealed record NewStaffUser(Guid Id, string Email, string DisplayName, string Password, string Role);

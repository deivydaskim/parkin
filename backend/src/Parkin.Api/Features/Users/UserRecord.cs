namespace Parkin.Api.Features.Users;

public record UserRecord(Guid Id, string Email, string DisplayName, string Role, string Status);

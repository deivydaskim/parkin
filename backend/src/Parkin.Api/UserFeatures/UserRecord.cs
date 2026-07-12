namespace Parkin.Api.UserFeatures;

public record UserRecord(Guid Id, string Email, string DisplayName, string Role, string Status);

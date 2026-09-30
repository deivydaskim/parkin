namespace Parkin.Api.Features.Lots;

internal static class LotErrors
{
  public static ValidationError DuplicateName => new("Name", "A lot with this name already exists");
}

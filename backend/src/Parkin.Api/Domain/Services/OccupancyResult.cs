namespace Parkin.Api.Domain.Services;

// GeneralFree is floored at 0 (never negative). IsOverCapacity is true only once active GENERAL
// sessions exceed capacity (i.e. an ALLOW_OVERFLOW entry was admitted past a full lot) - it is
// stronger than IsGeneralPoolFull, which trips at exactly-full already.
public sealed record OccupancyResult
{
  public required int GeneralCapacity { get; init; }
  public required int GeneralUsed { get; init; }
  public required int GeneralFree { get; init; }
  public required bool IsGeneralPoolFull { get; init; }
  public required bool IsOverCapacity { get; init; }
  public required int ReservedCount { get; init; }
}

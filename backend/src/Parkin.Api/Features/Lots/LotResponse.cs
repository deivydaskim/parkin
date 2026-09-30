using Parkin.Api.Domain.ParkingLotAggregate;

namespace Parkin.Api.Features.Lots;

public record LotResponse(
  Guid Id,
  string Name,
  string? Address,
  string Timezone,
  AccessMode AccessMode,
  FullBehavior FullBehavior,
  LotStatus Status,
  int Capacity,
  LotLayoutResponse? Layout)
{
  public static LotResponse From(ParkingLot lot) =>
    new(lot.Id.Value, lot.Name, lot.Address, lot.Timezone, lot.AccessMode, lot.FullBehavior, lot.Status,
      lot.Capacity, LotLayoutResponse.From(lot.Layout));
}

public record LotLayoutResponse(decimal WidthMeters, decimal LengthMeters, int LevelCount)
{
  public static LotLayoutResponse? From(LotLayout? layout) =>
    layout is null ? null : new(layout.WidthMeters, layout.LengthMeters, layout.LevelCount);
}

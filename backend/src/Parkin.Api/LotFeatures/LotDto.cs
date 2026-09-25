using Parkin.Api.Domain.ParkingLotAggregate;

namespace Parkin.Api.LotFeatures;

public record LotDto(
  ParkingLotId Id,
  string Name,
  string? Address,
  string Timezone,
  AccessMode AccessMode,
  FullBehavior FullBehavior,
  LotStatus Status,
  int Capacity,
  LotLayout? Layout)
{
  public static LotDto FromEntity(ParkingLot lot) =>
    new(lot.Id, lot.Name, lot.Address, lot.Timezone, lot.AccessMode, lot.FullBehavior, lot.Status, lot.Capacity, lot.Layout);
}

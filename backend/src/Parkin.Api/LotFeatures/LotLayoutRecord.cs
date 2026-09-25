using Parkin.Api.Domain.ParkingLotAggregate;

namespace Parkin.Api.LotFeatures;

public record LotLayoutRecord(decimal WidthMeters, decimal LengthMeters, int LevelCount)
{
  public static LotLayoutRecord? FromValue(LotLayout? layout) =>
    layout is null ? null : new(layout.WidthMeters, layout.LengthMeters, layout.LevelCount);
}

using Parkin.Api.Domain.ParkingLotAggregate;

namespace Parkin.Api.LotFeatures;

public record LotRecord(
  Guid Id,
  string Name,
  string? Address,
  string Timezone,
  AccessMode AccessMode,
  FullBehavior FullBehavior,
  LotStatus Status,
  int Capacity);

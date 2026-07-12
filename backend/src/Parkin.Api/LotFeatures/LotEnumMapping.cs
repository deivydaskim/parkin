using Parkin.Api.Domain.ParkingLotAggregate;

namespace Parkin.Api.LotFeatures;

/// <summary>
/// Converts ParkingLot domain enums to/from the exact PRD wire vocabulary
/// (e.g. "OPEN"/"RESTRICTED"). No global JsonStringEnumConverter is configured
/// anywhere in this codebase, so the conversion is kept local to this slice.
/// </summary>
internal static class LotEnumMapping
{
  public static AccessMode ParseAccessMode(string value) => value switch
  {
    "RESTRICTED" => AccessMode.Restricted,
    "OPEN" => AccessMode.Open,
    _ => throw new ArgumentOutOfRangeException(nameof(value), value, "AccessMode must be OPEN or RESTRICTED")
  };

  public static FullBehavior ParseFullBehavior(string value) => value switch
  {
    "ALLOW_OVERFLOW" => FullBehavior.AllowOverflow,
    "BLOCK" => FullBehavior.Block,
    _ => throw new ArgumentOutOfRangeException(nameof(value), value, "FullBehavior must be BLOCK or ALLOW_OVERFLOW")
  };

  public static string ToWire(AccessMode mode) => mode switch
  {
    AccessMode.Restricted => "RESTRICTED",
    AccessMode.Open => "OPEN",
    _ => throw new ArgumentOutOfRangeException(nameof(mode))
  };

  public static string ToWire(FullBehavior behavior) => behavior switch
  {
    FullBehavior.AllowOverflow => "ALLOW_OVERFLOW",
    FullBehavior.Block => "BLOCK",
    _ => throw new ArgumentOutOfRangeException(nameof(behavior))
  };

  public static string ToWire(LotStatus status) => status switch
  {
    LotStatus.Archived => "ARCHIVED",
    LotStatus.Active => "ACTIVE",
    _ => throw new ArgumentOutOfRangeException(nameof(status))
  };

  // Capacity is derived from active ParkingSpace rows, which don't exist until T2.2.
  public static LotRecord ToRecord(LotDto dto) => new(
    dto.Id.Value,
    dto.Name,
    dto.Address,
    dto.Timezone,
    ToWire(dto.AccessMode),
    ToWire(dto.FullBehavior),
    ToWire(dto.Status),
    Capacity: 0);
}

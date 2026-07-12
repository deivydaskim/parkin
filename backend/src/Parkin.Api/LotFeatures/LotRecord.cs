namespace Parkin.Api.LotFeatures;

/// <summary>
/// Wire DTO. AccessMode/FullBehavior/Status are the exact PRD vocabulary strings
/// (e.g. "OPEN"/"RESTRICTED"), not the .NET enum names.
/// </summary>
public record LotRecord(
  Guid Id,
  string Name,
  string? Address,
  string Timezone,
  string AccessMode,
  string FullBehavior,
  string Status,
  int Capacity);

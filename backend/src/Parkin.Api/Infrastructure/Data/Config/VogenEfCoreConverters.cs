using Parkin.Api.Domain.AccessGrantAggregate;
using Parkin.Api.Domain.AuditAggregate;
using Parkin.Api.Domain.DriverAggregate;
using Parkin.Api.Domain.ParkingLotAggregate;
using Vogen;

namespace Parkin.Api.Infrastructure.Data.Config;

[EfCoreConverter<ParkingLotId>]
[EfCoreConverter<ParkingSpaceId>]
[EfCoreConverter<AuditLogEntryId>]
[EfCoreConverter<DriverId>]
[EfCoreConverter<PlateId>]
[EfCoreConverter<AccessGrantId>]
internal partial class VogenEfCoreConverters;

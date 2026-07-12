using Parkin.Api.Domain.AuditAggregate;
using Parkin.Api.Domain.ParkingLotAggregate;
using Vogen;

namespace Parkin.Api.Infrastructure.Data.Config;

[EfCoreConverter<ParkingLotId>]
[EfCoreConverter<AuditLogEntryId>]
internal partial class VogenEfCoreConverters;

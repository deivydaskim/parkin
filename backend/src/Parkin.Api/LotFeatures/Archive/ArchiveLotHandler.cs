using Parkin.Api.Domain.ParkingLotAggregate;
using Parkin.Api.Domain.ParkingLotAggregate.Specifications;

namespace Parkin.Api.LotFeatures.Archive;

public record ArchiveLotCommand(ParkingLotId LotId, Guid? ActorId) : ICommand<Result<LotDto>>;

public class ArchiveLotHandler(IRepository<ParkingLot> repository)
  : ICommandHandler<ArchiveLotCommand, Result<LotDto>>
{
  public async ValueTask<Result<LotDto>> Handle(ArchiveLotCommand request, CancellationToken cancellationToken)
  {
    var lot = await repository.FirstOrDefaultAsync(new ParkingLotByIdSpec(request.LotId), cancellationToken);
    if (lot == null) return Result.NotFound();

    lot.Archive(request.ActorId);
    await repository.UpdateAsync(lot, cancellationToken);

    return new LotDto(lot.Id, lot.Name, lot.Address, lot.Timezone, lot.AccessMode, lot.FullBehavior, lot.Status, lot.Capacity);
  }
}

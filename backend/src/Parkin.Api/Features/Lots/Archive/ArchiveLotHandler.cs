using Parkin.Api.Domain.ParkingLotAggregate;
using Parkin.Api.Domain.ParkingLotAggregate.Specifications;

namespace Parkin.Api.Features.Lots.Archive;

public record ArchiveLotCommand(ParkingLotId LotId, Guid? ActorId) : ICommand<Result<LotResponse>>;

public class ArchiveLotHandler(IRepository<ParkingLot> repository)
  : ICommandHandler<ArchiveLotCommand, Result<LotResponse>>
{
  public async ValueTask<Result<LotResponse>> Handle(ArchiveLotCommand request, CancellationToken cancellationToken)
  {
    var lot = await repository.FirstOrDefaultAsync(new ParkingLotByIdSpec(request.LotId), cancellationToken);
    if (lot is null) return Result.NotFound();

    lot.Archive(request.ActorId);
    await repository.UpdateAsync(lot, cancellationToken);

    return LotResponse.From(lot);
  }
}

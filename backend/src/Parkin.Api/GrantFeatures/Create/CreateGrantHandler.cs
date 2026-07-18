using Parkin.Api.Domain.AccessGrantAggregate;
using Parkin.Api.Domain.AccessGrantAggregate.Specifications;
using Parkin.Api.Domain.DriverAggregate;
using Parkin.Api.Domain.DriverAggregate.Specifications;
using Parkin.Api.Domain.ParkingLotAggregate;
using Parkin.Api.Domain.ParkingLotAggregate.Specifications;

namespace Parkin.Api.GrantFeatures.Create;

public record CreateGrantCommand(
  DriverId DriverId,
  ParkingLotId LotId,
  DateTimeOffset? ValidFrom,
  DateTimeOffset? ValidTo,
  Guid? ActorId) : ICommand<Result<GrantDto>>;

public class CreateGrantHandler(
  IRepository<Driver> driverRepository,
  IRepository<ParkingLot> lotRepository,
  IRepository<AccessGrant> grantRepository)
  : ICommandHandler<CreateGrantCommand, Result<GrantDto>>
{
  public async ValueTask<Result<GrantDto>> Handle(CreateGrantCommand request, CancellationToken cancellationToken)
  {
    var driver = await driverRepository.FirstOrDefaultAsync(new DriverByIdSpec(request.DriverId), cancellationToken);
    if (driver == null) return Result.NotFound();

    var lot = await lotRepository.FirstOrDefaultAsync(new ParkingLotByIdSpec(request.LotId), cancellationToken);
    if (lot == null) return Result.NotFound();

    if (lot.Status != LotStatus.Active)
    {
      return Result.Invalid(new ValidationError("LotId", "Lot is not active"));
    }

    var existing = await grantRepository.FirstOrDefaultAsync(
      new ActiveGrantForDriverLotSpec(request.DriverId, request.LotId), cancellationToken);
    if (existing != null)
    {
      return Result.Invalid(new ValidationError("LotId", "An active grant already exists for this driver and lot"));
    }

    var grant = AccessGrant.Create(request.DriverId, request.LotId, request.ValidFrom, request.ValidTo, request.ActorId);
    await grantRepository.AddAsync(grant, cancellationToken);

    return new GrantDto(grant.Id, grant.DriverId, grant.ParkingLotId, grant.ValidFrom, grant.ValidTo, grant.Status);
  }
}

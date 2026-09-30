using Parkin.Api.Domain.AccessGrantAggregate;
using Parkin.Api.Domain.AccessGrantAggregate.Specifications;
using Parkin.Api.Domain.DriverAggregate;
using Parkin.Api.Domain.DriverAggregate.Specifications;
using Parkin.Api.Domain.ParkingLotAggregate;

namespace Parkin.Api.Features.Grants.Create;

public record CreateGrantCommand(
  DriverId DriverId,
  ParkingLotId LotId,
  DateTimeOffset? ValidFrom,
  DateTimeOffset? ValidTo,
  Guid? ActorId) : ICommand<Result<GrantResponse>>;

public class CreateGrantHandler(
  IReadRepository<Driver> driverRepository,
  IReadRepository<ParkingLot> lotRepository,
  IRepository<AccessGrant> grantRepository,
  TimeProvider timeProvider)
  : ICommandHandler<CreateGrantCommand, Result<GrantResponse>>
{
  public async ValueTask<Result<GrantResponse>> Handle(CreateGrantCommand request, CancellationToken cancellationToken)
  {
    if (!await driverRepository.AnyAsync(new DriverByIdSpec(request.DriverId), cancellationToken)) return Result.NotFound();

    var lot = await lotRepository.GetByIdAsync(request.LotId, cancellationToken);
    if (lot is null) return Result.NotFound();

    if (lot.Status != LotStatus.Active)
    {
      return Result.Invalid(new ValidationError("LotId", "Lot is not active"));
    }

    if (await grantRepository.AnyAsync(new ActiveGrantForDriverLotSpec(request.DriverId, request.LotId), cancellationToken))
    {
      return Result.Invalid(new ValidationError("LotId", "An active grant already exists for this driver and lot"));
    }

    var result = AccessGrant.Create(request.DriverId, request.LotId, request.ValidFrom, request.ValidTo,
      timeProvider.GetUtcNow(), request.ActorId);
    if (result.IsSuccess) await grantRepository.AddAsync(result.Value, cancellationToken);

    return result.Map(grant => GrantResponse.From(grant, lot.Name));
  }
}

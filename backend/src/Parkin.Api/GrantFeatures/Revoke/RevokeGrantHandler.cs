using Parkin.Api.Domain.AccessGrantAggregate;
using Parkin.Api.Domain.AccessGrantAggregate.Specifications;

namespace Parkin.Api.GrantFeatures.Revoke;

public record RevokeGrantCommand(AccessGrantId GrantId, Guid? ActorId) : ICommand<Result<GrantDto>>;

public class RevokeGrantHandler(IRepository<AccessGrant> repository)
  : ICommandHandler<RevokeGrantCommand, Result<GrantDto>>
{
  public async ValueTask<Result<GrantDto>> Handle(RevokeGrantCommand request, CancellationToken cancellationToken)
  {
    var grant = await repository.FirstOrDefaultAsync(new AccessGrantByIdSpec(request.GrantId), cancellationToken);
    if (grant == null) return Result.NotFound();

    grant.Revoke(request.ActorId);
    await repository.UpdateAsync(grant, cancellationToken);

    return new GrantDto(grant.Id, grant.DriverId, grant.ParkingLotId, grant.ValidFrom, grant.ValidTo, grant.Status);
  }
}

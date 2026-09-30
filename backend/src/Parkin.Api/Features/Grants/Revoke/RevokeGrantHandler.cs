using Parkin.Api.Domain.AccessGrantAggregate;

namespace Parkin.Api.Features.Grants.Revoke;

public record RevokeGrantCommand(AccessGrantId GrantId, Guid? ActorId) : ICommand<Result<GrantResponse>>;

public class RevokeGrantHandler(IRepository<AccessGrant> repository)
  : ICommandHandler<RevokeGrantCommand, Result<GrantResponse>>
{
  public async ValueTask<Result<GrantResponse>> Handle(RevokeGrantCommand request, CancellationToken cancellationToken)
  {
    var grant = await repository.GetByIdAsync(request.GrantId, cancellationToken);
    if (grant is null) return Result.NotFound();

    var result = grant.Revoke(request.ActorId);
    if (!result.IsSuccess) return result;

    await repository.UpdateAsync(grant, cancellationToken);
    return GrantResponse.From(grant);
  }
}

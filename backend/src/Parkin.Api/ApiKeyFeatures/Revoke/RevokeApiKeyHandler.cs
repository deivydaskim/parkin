using Parkin.Api.Domain.ApiKeyAggregate;
using Parkin.Api.Domain.ApiKeyAggregate.Specifications;

namespace Parkin.Api.ApiKeyFeatures.Revoke;

public record RevokeApiKeyCommand(ApiKeyId ApiKeyId, Guid? ActorId) : ICommand<Result<ApiKeyDto>>;

public class RevokeApiKeyHandler(IRepository<ApiKey> repository)
  : ICommandHandler<RevokeApiKeyCommand, Result<ApiKeyDto>>
{
  public async ValueTask<Result<ApiKeyDto>> Handle(RevokeApiKeyCommand request, CancellationToken cancellationToken)
  {
    var apiKey = await repository.FirstOrDefaultAsync(new ApiKeyByIdSpec(request.ApiKeyId), cancellationToken);
    if (apiKey == null) return Result.NotFound();

    apiKey.Revoke(request.ActorId);
    await repository.UpdateAsync(apiKey, cancellationToken);

    return new ApiKeyDto(apiKey.Id, apiKey.Name, apiKey.Prefix, apiKey.Status, apiKey.CreatedAt, apiKey.RevokedAt);
  }
}

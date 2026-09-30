using Parkin.Api.Domain.ApiKeyAggregate;

namespace Parkin.Api.Features.ApiKeys.Revoke;

public record RevokeApiKeyCommand(ApiKeyId ApiKeyId, Guid? ActorId) : ICommand<Result<ApiKeyResponse>>;

public class RevokeApiKeyHandler(IRepository<ApiKey> repository, TimeProvider timeProvider)
  : ICommandHandler<RevokeApiKeyCommand, Result<ApiKeyResponse>>
{
  public async ValueTask<Result<ApiKeyResponse>> Handle(RevokeApiKeyCommand request,
    CancellationToken cancellationToken)
  {
    var apiKey = await repository.GetByIdAsync(request.ApiKeyId, cancellationToken);
    if (apiKey is null) return Result.NotFound();

    var revoked = apiKey.Revoke(request.ActorId, timeProvider.GetUtcNow());
    if (!revoked.IsSuccess) return Result.Conflict([.. revoked.Errors]);

    await repository.UpdateAsync(apiKey, cancellationToken);
    return ApiKeyResponse.From(apiKey);
  }
}

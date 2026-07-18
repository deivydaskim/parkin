using Parkin.Api.Domain.ApiKeyAggregate;

namespace Parkin.Api.ApiKeyFeatures.Create;

public record ApiKeyCreateDto(ApiKeyDto ApiKey, string RawKey);

public record CreateApiKeyCommand(string Name, Guid? ActorId) : ICommand<Result<ApiKeyCreateDto>>;

public class CreateApiKeyHandler(IRepository<ApiKey> repository)
  : ICommandHandler<CreateApiKeyCommand, Result<ApiKeyCreateDto>>
{
  public async ValueTask<Result<ApiKeyCreateDto>> Handle(CreateApiKeyCommand request, CancellationToken cancellationToken)
  {
    var (apiKey, rawKey) = ApiKey.Create(request.Name, request.ActorId);
    await repository.AddAsync(apiKey, cancellationToken);

    var dto = new ApiKeyDto(apiKey.Id, apiKey.Name, apiKey.Prefix, apiKey.Status, apiKey.CreatedAt, apiKey.RevokedAt);
    return new ApiKeyCreateDto(dto, rawKey);
  }
}

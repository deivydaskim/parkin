using Parkin.Api.Domain.ApiKeyAggregate;

namespace Parkin.Api.Features.ApiKeys.Create;

public record CreateApiKeyCommand(string Name, Guid? ActorId) : ICommand<Result<CreateApiKeyResponse>>;

public class CreateApiKeyHandler(IRepository<ApiKey> repository, TimeProvider timeProvider)
  : ICommandHandler<CreateApiKeyCommand, Result<CreateApiKeyResponse>>
{
  public async ValueTask<Result<CreateApiKeyResponse>> Handle(CreateApiKeyCommand request,
    CancellationToken cancellationToken)
  {
    var (apiKey, rawKey) = ApiKey.Create(request.Name, request.ActorId, timeProvider.GetUtcNow());
    await repository.AddAsync(apiKey, cancellationToken);

    return CreateApiKeyResponse.From(apiKey, rawKey);
  }
}

using Parkin.Api.Domain.ApiKeyAggregate;
using Parkin.Api.Domain.ParkingLotAggregate;

namespace Parkin.Api.Features.ApiKeys.Create;

public record CreateApiKeyCommand(string Name, ParkingLotId LotId, Guid? ActorId)
  : ICommand<Result<CreateApiKeyResponse>>;

public class CreateApiKeyHandler(
  IRepository<ApiKey> repository,
  IReadRepository<ParkingLot> lotRepository,
  TimeProvider timeProvider)
  : ICommandHandler<CreateApiKeyCommand, Result<CreateApiKeyResponse>>
{
  public async ValueTask<Result<CreateApiKeyResponse>> Handle(CreateApiKeyCommand request,
    CancellationToken cancellationToken)
  {
    var lot = await lotRepository.GetByIdAsync(request.LotId, cancellationToken);
    if (lot is null) return Result.NotFound();

    if (lot.Status != LotStatus.Active)
    {
      return Result.Invalid(new ValidationError("LotId", "Lot is not active"));
    }

    var (apiKey, rawKey) = ApiKey.Create(request.Name, lot.Id, request.ActorId, timeProvider.GetUtcNow());
    await repository.AddAsync(apiKey, cancellationToken);

    return CreateApiKeyResponse.From(apiKey, lot.Name, rawKey);
  }
}

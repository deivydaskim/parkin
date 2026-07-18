using Vogen;

namespace Parkin.Api.Domain.ApiKeyAggregate;

[ValueObject<Guid>]
public readonly partial struct ApiKeyId
{
  private static Validation Validate(Guid value)
      => value != Guid.Empty ? Validation.Ok : Validation.Invalid("ApiKeyId cannot be empty.");
}

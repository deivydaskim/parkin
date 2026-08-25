namespace Parkin.Api.Domain.AccessEventAggregate.Specifications;

public class AccessEventByIdempotencyKeySpec : Specification<AccessEvent>
{
  public AccessEventByIdempotencyKeySpec(string idempotencyKey) =>
    Query.Where(accessEvent => accessEvent.IdempotencyKey == idempotencyKey);
}

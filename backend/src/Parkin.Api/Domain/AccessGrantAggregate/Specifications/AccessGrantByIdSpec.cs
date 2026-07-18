namespace Parkin.Api.Domain.AccessGrantAggregate.Specifications;

public class AccessGrantByIdSpec : Specification<AccessGrant>
{
  public AccessGrantByIdSpec(AccessGrantId grantId) =>
    Query
        .Where(grant => grant.Id == grantId);
}

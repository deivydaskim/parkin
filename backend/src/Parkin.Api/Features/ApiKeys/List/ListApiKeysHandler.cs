namespace Parkin.Api.Features.ApiKeys.List;

public record ListApiKeysQuery : IQuery<Result<IReadOnlyList<ApiKeyResponse>>>;

public class ListApiKeysHandler(IListApiKeysQueryService query)
  : IQueryHandler<ListApiKeysQuery, Result<IReadOnlyList<ApiKeyResponse>>>
{
  public async ValueTask<Result<IReadOnlyList<ApiKeyResponse>>> Handle(ListApiKeysQuery request,
    CancellationToken cancellationToken)
    => Result.Success(await query.ListAsync(cancellationToken));
}

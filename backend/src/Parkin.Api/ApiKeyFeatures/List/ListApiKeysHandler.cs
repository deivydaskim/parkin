namespace Parkin.Api.ApiKeyFeatures.List;

public record ListApiKeysQuery : IQuery<Result<IReadOnlyList<ApiKeyDto>>>;

public class ListApiKeysHandler(IListApiKeysQueryService query)
  : IQueryHandler<ListApiKeysQuery, Result<IReadOnlyList<ApiKeyDto>>>
{
  public async ValueTask<Result<IReadOnlyList<ApiKeyDto>>> Handle(ListApiKeysQuery request, CancellationToken cancellationToken)
  {
    var result = await query.ListAsync(cancellationToken);
    return Result.Success(result);
  }
}

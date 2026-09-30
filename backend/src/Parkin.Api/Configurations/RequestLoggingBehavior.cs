using System.Diagnostics;

namespace Parkin.Api.Configurations;

public sealed class RequestLoggingBehavior<TRequest, TResponse>(ILogger<RequestLoggingBehavior<TRequest, TResponse>> logger)
  : IPipelineBehavior<TRequest, TResponse>
  where TRequest : notnull, IMessage
{
  public async ValueTask<TResponse> Handle(TRequest request, MessageHandlerDelegate<TRequest, TResponse> next,
    CancellationToken cancellationToken)
  {
    var startedAt = Stopwatch.GetTimestamp();
    var response = await next(request, cancellationToken);

    logger.LogInformation("Handled {RequestName} with status {Status} in {ElapsedMs:0.0} ms",
      typeof(TRequest).Name,
      response is Ardalis.Result.IResult result ? result.Status : ResultStatus.Ok,
      Stopwatch.GetElapsedTime(startedAt).TotalMilliseconds);

    return response;
  }
}

using Microsoft.AspNetCore.Http.HttpResults;

namespace Parkin.Api.Web;

public static class ResultHttpExtensions
{
  public static Results<Ok<TResponse>, ValidationProblem, ProblemHttpResult> ToOkResult<TValue, TResponse>(
    this Result<TValue> result, Func<TValue, TResponse> map)
    => result.ToHttpResult(value => TypedResults.Ok(map(value)));

  public static Results<Created<TResponse>, ValidationProblem, ProblemHttpResult> ToCreatedResult<TValue, TResponse>(
    this Result<TValue> result, Func<TValue, string> location, Func<TValue, TResponse> map)
    => result.ToHttpResult(value => TypedResults.Created(location(value), map(value)));

  public static Results<NoContent, ValidationProblem, ProblemHttpResult> ToNoContentResult(
    this Ardalis.Result.IResult result)
    => result.IsOk() ? TypedResults.NoContent() : result.ToFailure<NoContent>();

  public static Results<TSuccess, ValidationProblem, ProblemHttpResult> ToHttpResult<TValue, TSuccess>(
    this Result<TValue> result, Func<TValue, TSuccess> onSuccess)
    where TSuccess : Microsoft.AspNetCore.Http.IResult
    => result.IsSuccess ? onSuccess(result.Value) : result.ToFailure<TSuccess>();

  public static Results<TSuccess, ValidationProblem, ProblemHttpResult> ToFailure<TSuccess>(
    this Ardalis.Result.IResult result)
    where TSuccess : Microsoft.AspNetCore.Http.IResult
  {
    if (result.Status == ResultStatus.Invalid)
    {
      return TypedResults.ValidationProblem(result.ValidationErrors
        .GroupBy(error => error.Identifier ?? string.Empty)
        .ToDictionary(group => group.Key, group => group.Select(error => error.ErrorMessage).ToArray()));
    }

    var (statusCode, title) = result.Status switch
    {
      ResultStatus.NotFound => (StatusCodes.Status404NotFound, "Not found"),
      ResultStatus.Conflict => (StatusCodes.Status409Conflict, "Conflict"),
      ResultStatus.Forbidden => (StatusCodes.Status403Forbidden, "Forbidden"),
      ResultStatus.Unauthorized => (StatusCodes.Status401Unauthorized, "Unauthorized"),
      ResultStatus.Unavailable => (StatusCodes.Status503ServiceUnavailable, "Unavailable"),
      ResultStatus.CriticalError => (StatusCodes.Status500InternalServerError, "Unexpected error"),
      _ => (StatusCodes.Status400BadRequest, "Request failed")
    };

    var errors = result.Errors.ToList();
    return TypedResults.Problem(
      title: title,
      detail: errors.Count > 0 ? string.Join("; ", errors) : null,
      statusCode: statusCode);
  }

  private static bool IsOk(this Ardalis.Result.IResult result)
    => result.Status is ResultStatus.Ok or ResultStatus.NoContent or ResultStatus.Created;
}

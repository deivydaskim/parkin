using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Parkin.Api.Domain.Exceptions;

namespace Parkin.Api.Web;

public sealed class UniqueConstraintViolationExceptionHandler(IProblemDetailsService problemDetailsService)
  : IExceptionHandler
{
  public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception,
    CancellationToken cancellationToken)
  {
    if (exception is not UniqueConstraintViolationException) return false;

    httpContext.Response.StatusCode = StatusCodes.Status409Conflict;
    return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
    {
      HttpContext = httpContext,
      Exception = exception,
      ProblemDetails = new ProblemDetails
      {
        Status = StatusCodes.Status409Conflict,
        Title = "Conflict",
        Detail = "The request conflicts with an existing resource."
      }
    });
  }
}

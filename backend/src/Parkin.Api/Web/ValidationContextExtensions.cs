using FluentValidation;

namespace Parkin.Api.Web;

public static class ValidationContextExtensions
{
  public static void AddFailures<T>(this ValidationContext<T> context, Ardalis.Result.IResult result)
  {
    foreach (var error in result.ValidationErrors)
    {
      context.AddFailure(error.Identifier, error.ErrorMessage);
    }
  }
}

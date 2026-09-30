using FastEndpoints;
using FluentValidation;
using Microsoft.AspNetCore.Http.HttpResults;
using Parkin.Api.Domain.StaffUsers;

namespace Parkin.Api.Features.Auth.Login;

public sealed class LoginRequest
{
  public const string Route = "/auth/login";

  public string Email { get; init; } = string.Empty;
  public string Password { get; init; } = string.Empty;
}

public class LoginEndpoint(IMediator mediator) :
  Endpoint<LoginRequest, Results<Ok<CurrentUserResponse>, ProblemHttpResult>>
{
  private const string InvalidCredentials = "Invalid email or password.";
  private const string LockedOut = "Account temporarily locked. Try again later.";

  public override void Configure()
  {
    Post(LoginRequest.Route);
    AllowAnonymous();

    Summary(s =>
    {
      s.Summary = "Staff login";
      s.Description = "Authenticates a staff member and issues an HttpOnly session cookie.";
      s.Responses[200] = "Authenticated; session cookie set";
      s.Responses[401] = "Invalid credentials";
      s.Responses[423] = "Account temporarily locked";
    });

    Tags("Auth");
  }

  public override async Task<Results<Ok<CurrentUserResponse>, ProblemHttpResult>>
    ExecuteAsync(LoginRequest request, CancellationToken cancellationToken)
  {
    var result = await mediator.Send(new LoginCommand(request.Email, request.Password), cancellationToken);

    return result switch
    {
      { Outcome: StaffSignInResult.Succeeded, User: { } user } => TypedResults.Ok(user),
      { Outcome: StaffSignInResult.LockedOut } => TypedResults.Problem(LockedOut, statusCode: StatusCodes.Status423Locked),
      _ => TypedResults.Problem(InvalidCredentials, statusCode: StatusCodes.Status401Unauthorized)
    };
  }
}

public sealed class LoginValidator : Validator<LoginRequest>
{
  public LoginValidator()
  {
    RuleFor(x => x.Email)
      .NotEmpty().WithMessage("Email is required")
      .EmailAddress().WithMessage("A valid email is required");

    RuleFor(x => x.Password)
      .NotEmpty().WithMessage("Password is required");
  }
}

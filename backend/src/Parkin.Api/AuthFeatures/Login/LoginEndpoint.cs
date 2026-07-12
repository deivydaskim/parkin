using FastEndpoints;
using FluentValidation;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;
using Parkin.Api.Infrastructure.Identity;

namespace Parkin.Api.AuthFeatures.Login;

public sealed class LoginRequest
{
  public string Email { get; init; } = string.Empty;
  public string Password { get; init; } = string.Empty;
}

public class LoginEndpoint(
  SignInManager<ApplicationUser> signInManager,
  UserManager<ApplicationUser> userManager) :
  Endpoint<LoginRequest,
           Results<Ok<CurrentUserResponse>, ProblemHttpResult>>
{
  // Generic message for every failure path so accounts can't be enumerated.
  private const string InvalidCredentials = "Invalid email or password.";
  private const string LockedOut = "Account temporarily locked. Try again later.";

  private readonly SignInManager<ApplicationUser> _signInManager = signInManager;
  private readonly UserManager<ApplicationUser> _userManager = userManager;

  public override void Configure()
  {
    Post("/auth/login");
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
    var user = await _userManager.FindByEmailAsync(request.Email);
    if (user is null || user.Status == UserStatus.Disabled)
    {
      return TypedResults.Problem(InvalidCredentials, statusCode: StatusCodes.Status401Unauthorized);
    }

    var result = await _signInManager.PasswordSignInAsync(
      user, request.Password, isPersistent: true, lockoutOnFailure: true);

    if (result.Succeeded)
    {
      var response = await CurrentUserResponseFactory.BuildAsync(_userManager, user);
      return TypedResults.Ok(response);
    }

    if (result.IsLockedOut)
    {
      return TypedResults.Problem(LockedOut, statusCode: StatusCodes.Status423Locked);
    }

    return TypedResults.Problem(InvalidCredentials, statusCode: StatusCodes.Status401Unauthorized);
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

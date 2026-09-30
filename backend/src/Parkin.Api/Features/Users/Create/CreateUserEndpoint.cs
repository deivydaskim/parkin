using FastEndpoints;
using FluentValidation;
using Microsoft.AspNetCore.Http.HttpResults;
using Parkin.Api.Authorization;
using Parkin.Api.Domain.StaffUsers;
using Parkin.Api.Web;

namespace Parkin.Api.Features.Users.Create;

public sealed class CreateUserRequest
{
  public const string Route = "/users";

  public string Email { get; init; } = string.Empty;
  public string Password { get; init; } = string.Empty;
  public string DisplayName { get; init; } = string.Empty;
  public string Role { get; init; } = string.Empty;
}

public class CreateUserEndpoint(IMediator mediator, ICurrentUser currentUser) :
  Endpoint<CreateUserRequest, Results<Created<UserResponse>, ValidationProblem, ProblemHttpResult>>
{
  public override void Configure()
  {
    Post(CreateUserRequest.Route);
    Roles(AccessPolicies.AdminOnly);

    Summary(s =>
    {
      s.Summary = "Create a staff account";
      s.Description = "Creates a staff account with the given email, password, display name, and role.";
      s.Responses[201] = "User created successfully";
      s.Responses[400] = "Invalid request data, or the password/email failed Identity's policy";
    });

    Tags("Users");

    Description(builder => builder
      .Accepts<CreateUserRequest>()
      .Produces<UserResponse>(201, "application/json")
      .ProducesProblem(400));
  }

  public override async Task<Results<Created<UserResponse>, ValidationProblem, ProblemHttpResult>>
    ExecuteAsync(CreateUserRequest request, CancellationToken cancellationToken)
  {
    var command = new CreateUserCommand(request.Email, request.Password, request.DisplayName, request.Role,
      currentUser.Id);
    var result = await mediator.Send(command, cancellationToken);
    return result.ToCreatedResult(user => $"/users/{user.Id}", user => user);
  }
}

public sealed class CreateUserValidator : Validator<CreateUserRequest>
{
  public CreateUserValidator()
  {
    RuleFor(x => x.Email)
      .NotEmpty().WithMessage("Email is required")
      .EmailAddress().WithMessage("A valid email is required");

    RuleFor(x => x.Password)
      .NotEmpty().WithMessage("Password is required");

    RuleFor(x => x.DisplayName)
      .NotEmpty().WithMessage("Display name is required")
      .MaximumLength(200).WithMessage("Display name must not exceed 200 characters");

    RuleFor(x => x.Role)
      .Must(v => StaffRoles.All.Contains(v))
      .WithMessage($"Role must be one of: {string.Join(", ", StaffRoles.All)}");
  }
}

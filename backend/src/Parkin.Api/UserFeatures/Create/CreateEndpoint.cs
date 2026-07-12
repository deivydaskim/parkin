using FastEndpoints;
using FluentValidation;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;
using Parkin.Api.Authorization;
using Parkin.Api.Domain.AuditAggregate;
using Parkin.Api.Infrastructure.Identity;

namespace Parkin.Api.UserFeatures.Create;

public sealed class CreateUserRequest
{
  public string Email { get; init; } = string.Empty;
  public string Password { get; init; } = string.Empty;
  public string DisplayName { get; init; } = string.Empty;
  public string Role { get; init; } = string.Empty;
}

public class CreateEndpoint(UserManager<ApplicationUser> userManager, IRepository<AuditLogEntry> auditRepository) :
  Endpoint<CreateUserRequest, Results<Created<UserRecord>, ValidationProblem, ProblemHttpResult>>
{
  public override void Configure()
  {
    Post("/users");
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
      .Produces<UserRecord>(201, "application/json")
      .ProducesProblem(400));
  }

  public override async Task<Results<Created<UserRecord>, ValidationProblem, ProblemHttpResult>>
    ExecuteAsync(CreateUserRequest request, CancellationToken cancellationToken)
  {
    var user = new ApplicationUser
    {
      UserName = request.Email,
      Email = request.Email,
      EmailConfirmed = true,
      DisplayName = request.DisplayName,
      Status = UserStatus.Active
    };

    var createResult = await userManager.CreateAsync(user, request.Password);
    if (!createResult.Succeeded)
    {
      var errors = createResult.Errors
        .GroupBy(e => e.Code.Contains("Password", StringComparison.OrdinalIgnoreCase) ? "Password" : "Email")
        .ToDictionary(g => g.Key, g => g.Select(e => e.Description).ToArray());
      return TypedResults.ValidationProblem(errors);
    }

    var addToRoleResult = await userManager.AddToRoleAsync(user, request.Role);
    if (!addToRoleResult.Succeeded)
    {
      return TypedResults.Problem(
        title: "Create failed",
        detail: string.Join("; ", addToRoleResult.Errors.Select(e => e.Description)),
        statusCode: StatusCodes.Status400BadRequest);
    }

    var entry = AuditLogEntry.Create(
      AuditActorType.Staff,
      HttpContext.User.ActorId(userManager),
      "user.create",
      "User",
      user.Id,
      HttpContext.Connection.RemoteIpAddress,
      new { role = request.Role });
    await auditRepository.AddAsync(entry, cancellationToken);
    await auditRepository.SaveChangesAsync(cancellationToken);

    var response = new UserRecord(user.Id, user.Email, user.DisplayName, request.Role, user.Status.ToString());
    return TypedResults.Created($"/users/{user.Id}", response);
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
      .Must(v => Roles.All.Contains(v))
      .WithMessage($"Role must be one of: {string.Join(", ", Roles.All)}");
  }
}

using Parkin.Api.Domain.StaffUsers;
using Parkin.Api.Features.Users.ChangeRole;
using Shouldly;
using Xunit;

namespace Parkin.UnitTests.UserFeatures.ChangeRole;

public class ChangeUserRoleValidatorTests
{
  private readonly ChangeUserRoleValidator _validator = new();

  [Fact]
  public void Validate_ValidRequest_HasNoErrors()
  {
    var request = new ChangeUserRoleRequest { UserId = Guid.NewGuid(), Role = StaffRoles.SystemAdmin };

    var result = _validator.Validate(request);

    result.ShouldSatisfyAllConditions(
      () => result.IsValid.ShouldBeTrue(),
      () => result.Errors.ShouldBeEmpty());
  }

  [Fact]
  public void Validate_EmptyUserId_HasError()
  {
    var request = new ChangeUserRoleRequest { UserId = Guid.Empty, Role = StaffRoles.Operator };

    var result = _validator.Validate(request);

    result.ShouldSatisfyAllConditions(
      () => result.IsValid.ShouldBeFalse(),
      () => result.Errors.ShouldContain(e => e.PropertyName == nameof(ChangeUserRoleRequest.UserId)));
  }

  [Fact]
  public void Validate_UnknownRole_HasError()
  {
    var request = new ChangeUserRoleRequest { UserId = Guid.NewGuid(), Role = "Nonsense" };

    var result = _validator.Validate(request);

    result.ShouldSatisfyAllConditions(
      () => result.IsValid.ShouldBeFalse(),
      () => result.Errors.ShouldContain(e => e.PropertyName == nameof(ChangeUserRoleRequest.Role)));
  }
}

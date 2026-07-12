using Parkin.Api.Infrastructure.Identity;
using Parkin.Api.UserFeatures.ChangeRole;
using Shouldly;
using Xunit;

namespace Parkin.UnitTests.UserFeatures.ChangeRole;

public class ChangeRoleValidatorTests
{
  private readonly ChangeRoleValidator _validator = new();

  [Fact]
  public void Validate_ValidRequest_HasNoErrors()
  {
    var request = new ChangeRoleRequest { UserId = Guid.NewGuid(), Role = Roles.SystemAdmin };

    var result = _validator.Validate(request);

    result.ShouldSatisfyAllConditions(
      () => result.IsValid.ShouldBeTrue(),
      () => result.Errors.ShouldBeEmpty());
  }

  [Fact]
  public void Validate_EmptyUserId_HasError()
  {
    var request = new ChangeRoleRequest { UserId = Guid.Empty, Role = Roles.Operator };

    var result = _validator.Validate(request);

    result.ShouldSatisfyAllConditions(
      () => result.IsValid.ShouldBeFalse(),
      () => result.Errors.ShouldContain(e => e.PropertyName == nameof(ChangeRoleRequest.UserId)));
  }

  [Fact]
  public void Validate_UnknownRole_HasError()
  {
    var request = new ChangeRoleRequest { UserId = Guid.NewGuid(), Role = "Nonsense" };

    var result = _validator.Validate(request);

    result.ShouldSatisfyAllConditions(
      () => result.IsValid.ShouldBeFalse(),
      () => result.Errors.ShouldContain(e => e.PropertyName == nameof(ChangeRoleRequest.Role)));
  }
}

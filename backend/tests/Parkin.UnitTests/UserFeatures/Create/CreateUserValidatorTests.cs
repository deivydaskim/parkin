using Parkin.Api.Infrastructure.Identity;
using Parkin.Api.UserFeatures.Create;
using Shouldly;
using Xunit;

namespace Parkin.UnitTests.UserFeatures.Create;

public class CreateUserValidatorTests
{
  private readonly CreateUserValidator _validator = new();

  [Fact]
  public void Validate_ValidRequest_HasNoErrors()
  {
    var request = new CreateUserRequest
    {
      Email = "operator@parkin.test",
      Password = "P@ssw0rd!",
      DisplayName = "Test Operator",
      Role = Roles.Operator
    };

    var result = _validator.Validate(request);

    result.ShouldSatisfyAllConditions(
      () => result.IsValid.ShouldBeTrue(),
      () => result.Errors.ShouldBeEmpty());
  }

  [Fact]
  public void Validate_InvalidEmail_HasError()
  {
    var request = new CreateUserRequest
    {
      Email = "not-an-email",
      Password = "P@ssw0rd!",
      DisplayName = "Test Operator",
      Role = Roles.Operator
    };

    var result = _validator.Validate(request);

    result.ShouldSatisfyAllConditions(
      () => result.IsValid.ShouldBeFalse(),
      () => result.Errors.ShouldContain(e => e.PropertyName == nameof(CreateUserRequest.Email)));
  }

  [Fact]
  public void Validate_EmptyPassword_HasError()
  {
    var request = new CreateUserRequest
    {
      Email = "operator@parkin.test",
      Password = "",
      DisplayName = "Test Operator",
      Role = Roles.Operator
    };

    var result = _validator.Validate(request);

    result.ShouldSatisfyAllConditions(
      () => result.IsValid.ShouldBeFalse(),
      () => result.Errors.ShouldContain(e => e.PropertyName == nameof(CreateUserRequest.Password)));
  }

  [Fact]
  public void Validate_EmptyDisplayName_HasError()
  {
    var request = new CreateUserRequest
    {
      Email = "operator@parkin.test",
      Password = "P@ssw0rd!",
      DisplayName = "",
      Role = Roles.Operator
    };

    var result = _validator.Validate(request);

    result.ShouldSatisfyAllConditions(
      () => result.IsValid.ShouldBeFalse(),
      () => result.Errors.ShouldContain(e => e.PropertyName == nameof(CreateUserRequest.DisplayName)));
  }

  [Fact]
  public void Validate_UnknownRole_HasError()
  {
    var request = new CreateUserRequest
    {
      Email = "operator@parkin.test",
      Password = "P@ssw0rd!",
      DisplayName = "Test Operator",
      Role = "Nonsense"
    };

    var result = _validator.Validate(request);

    result.ShouldSatisfyAllConditions(
      () => result.IsValid.ShouldBeFalse(),
      () => result.Errors.ShouldContain(e => e.PropertyName == nameof(CreateUserRequest.Role)));
  }
}

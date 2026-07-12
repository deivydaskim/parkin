using Parkin.Api.ProductFeatures.Create;
using Shouldly;
using Xunit;

namespace Parkin.UnitTests.ProductFeatures.Create;

public class CreateProductValidatorTests
{
  private readonly CreateProductValidator _validator = new();

  [Fact]
  public void Validate_ValidRequest_HasNoErrors()
  {
    // Arrange
    var request = new CreateProductRequest { Name = "Widget", UnitPrice = 9.99m };

    // Act
    var result = _validator.Validate(request);

    // Assert
    result.ShouldSatisfyAllConditions(
      () => result.IsValid.ShouldBeTrue(),
      () => result.Errors.ShouldBeEmpty());
  }

  [Fact]
  public void Validate_EmptyName_HasError()
  {
    // Arrange
    var request = new CreateProductRequest { Name = "", UnitPrice = 9.99m };

    // Act
    var result = _validator.Validate(request);

    // Assert
    result.ShouldSatisfyAllConditions(
      () => result.IsValid.ShouldBeFalse(),
      () => result.Errors.ShouldContain(e => e.PropertyName == nameof(CreateProductRequest.Name)));
  }

  [Fact]
  public void Validate_ZeroUnitPrice_HasError()
  {
    // Arrange
    var request = new CreateProductRequest { Name = "Widget", UnitPrice = 0m };

    // Act
    var result = _validator.Validate(request);

    // Assert
    result.ShouldSatisfyAllConditions(
      () => result.IsValid.ShouldBeFalse(),
      () => result.Errors.ShouldContain(e => e.PropertyName == nameof(CreateProductRequest.UnitPrice)));
  }
}

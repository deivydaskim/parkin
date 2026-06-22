using Parkin.Api.Domain.ProductAggregate;

namespace Parkin.Api.ProductFeatures;
public record ProductDto(ProductId Id, string Name, decimal UnitPrice);

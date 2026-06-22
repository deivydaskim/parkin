using Parkin.Web.Domain.ProductAggregate;

namespace Parkin.Web.ProductFeatures;
public record ProductDto(ProductId Id, string Name, decimal UnitPrice);

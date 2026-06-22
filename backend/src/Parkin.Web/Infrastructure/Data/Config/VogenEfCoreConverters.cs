using Parkin.Web.Domain.CartAggregate;
using Parkin.Web.Domain.GuestUserAggregate;
using Parkin.Web.Domain.OrderAggregate;
using Parkin.Web.Domain.ProductAggregate;
using Vogen;

namespace Parkin.Web.Infrastructure.Data.Config;

[EfCoreConverter<ProductId>]
[EfCoreConverter<CartId>]
[EfCoreConverter<CartItemId>]
[EfCoreConverter<GuestUserId>]
[EfCoreConverter<OrderId>]
[EfCoreConverter<OrderItemId>]
[EfCoreConverter<Quantity>]
[EfCoreConverter<Price>]
internal partial class VogenEfCoreConverters;

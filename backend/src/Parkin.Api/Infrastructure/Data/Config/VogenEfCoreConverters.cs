using Parkin.Api.Domain.CartAggregate;
using Parkin.Api.Domain.GuestUserAggregate;
using Parkin.Api.Domain.OrderAggregate;
using Parkin.Api.Domain.ParkingLotAggregate;
using Parkin.Api.Domain.ProductAggregate;
using Vogen;

namespace Parkin.Api.Infrastructure.Data.Config;

[EfCoreConverter<ProductId>]
[EfCoreConverter<CartId>]
[EfCoreConverter<CartItemId>]
[EfCoreConverter<GuestUserId>]
[EfCoreConverter<OrderId>]
[EfCoreConverter<OrderItemId>]
[EfCoreConverter<Quantity>]
[EfCoreConverter<Price>]
[EfCoreConverter<ParkingLotId>]
internal partial class VogenEfCoreConverters;

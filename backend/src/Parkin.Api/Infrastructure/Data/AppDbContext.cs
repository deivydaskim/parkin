using System.Reflection;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Parkin.Api.Domain.CartAggregate;
using Parkin.Api.Domain.GuestUserAggregate;
using Parkin.Api.Domain.OrderAggregate;
using Parkin.Api.Domain.ProductAggregate;
using Parkin.Api.Infrastructure.Identity;

namespace Parkin.Api.Infrastructure.Data;
public class AppDbContext(DbContextOptions<AppDbContext> options) :
  IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>(options)
{
  public DbSet<Product> Products => Set<Product>();
  public DbSet<Cart> Carts => Set<Cart>();
  public DbSet<CartItem> CartItems => Set<CartItem>();
  public DbSet<GuestUser> GuestUsers => Set<GuestUser>();
  public DbSet<Order> Orders => Set<Order>();
  public DbSet<OrderItem> OrderItems => Set<OrderItem>();

  protected override void OnModelCreating(ModelBuilder modelBuilder)
  {
    base.OnModelCreating(modelBuilder);
    modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
  }

  public override int SaveChanges() =>
        SaveChangesAsync().GetAwaiter().GetResult();
}

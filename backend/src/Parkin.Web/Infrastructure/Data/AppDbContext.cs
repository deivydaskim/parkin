using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Parkin.Web.Domain.CartAggregate;
using Parkin.Web.Domain.GuestUserAggregate;
using Parkin.Web.Domain.OrderAggregate;
using Parkin.Web.Domain.ProductAggregate;

namespace Parkin.Web.Infrastructure.Data;
public class AppDbContext(DbContextOptions<AppDbContext> options) : 
  DbContext(options)
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

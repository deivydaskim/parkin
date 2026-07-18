using System.Reflection;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Parkin.Api.Domain.AccessGrantAggregate;
using Parkin.Api.Domain.AuditAggregate;
using Parkin.Api.Domain.DriverAggregate;
using Parkin.Api.Domain.ParkingLotAggregate;
using Parkin.Api.Infrastructure.Identity;

namespace Parkin.Api.Infrastructure.Data;
public class AppDbContext(DbContextOptions<AppDbContext> options) :
  IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>(options)
{
  public DbSet<ParkingLot> ParkingLots => Set<ParkingLot>();
  public DbSet<ParkingSpace> ParkingSpaces => Set<ParkingSpace>();
  public DbSet<AuditLogEntry> AuditLogEntries => Set<AuditLogEntry>();
  public DbSet<Driver> Drivers => Set<Driver>();
  public DbSet<Plate> Plates => Set<Plate>();
  public DbSet<AccessGrant> AccessGrants => Set<AccessGrant>();

  protected override void OnModelCreating(ModelBuilder modelBuilder)
  {
    base.OnModelCreating(modelBuilder);
    modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
  }

  public override int SaveChanges() =>
        SaveChangesAsync().GetAwaiter().GetResult();
}

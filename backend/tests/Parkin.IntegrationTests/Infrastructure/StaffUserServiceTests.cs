using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Parkin.Api.Domain.AuditAggregate;
using Parkin.Api.Domain.StaffUsers;
using Parkin.Api.Infrastructure.Data;
using Parkin.Api.Infrastructure.Identity;
using Shouldly;
using Xunit;

namespace Parkin.IntegrationTests.Infrastructure;

public class StaffUserServiceTests : IClassFixture<PostgresFixture>, IAsyncLifetime
{
  private const string Password = "Staff!2345";

  private readonly PostgresFixture _fixture;
  private ServiceProvider _services = null!;

  public StaffUserServiceTests(PostgresFixture fixture) => _fixture = fixture;

  public async Task InitializeAsync()
  {
    var services = new ServiceCollection();
    services.AddLogging();
    services.AddDbContext<AppDbContext>(options => options.UseNpgsql(_fixture.ConnectionString));
    services.AddIdentityCore<ApplicationUser>(options => options.User.RequireUniqueEmail = true)
      .AddRoles<IdentityRole<Guid>>()
      .AddEntityFrameworkStores<AppDbContext>();
    services.AddScoped<IStaffUserService, StaffUserService>();
    _services = services.BuildServiceProvider();

    await using var scope = _services.CreateAsyncScope();
    await scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.MigrateAsync();
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
    foreach (var role in StaffRoles.All)
    {
      if (!await roleManager.RoleExistsAsync(role)) await roleManager.CreateAsync(new IdentityRole<Guid>(role));
    }
  }

  public async Task DisposeAsync() => await _services.DisposeAsync();

  private static NewStaffUser NewUser(string role)
    => new(Guid.CreateVersion7(), $"staff-{Guid.NewGuid():N}@parkin.local", "Staff", Password, role);

  private static AuditLogEntry Audit(string action, Guid userId)
    => AuditLogEntry.Create(AuditActorType.Staff, Guid.NewGuid(), action, AuditEntityTypes.User, userId,
      DateTimeOffset.UtcNow);

  private async Task<T> InScopeAsync<T>(Func<IServiceProvider, Task<T>> action)
  {
    await using var scope = _services.CreateAsyncScope();
    return await action(scope.ServiceProvider);
  }

  private Task<bool> AuditExistsAsync(string action, Guid userId)
    => InScopeAsync(sp => sp.GetRequiredService<AppDbContext>().AuditLogEntries
      .AnyAsync(e => e.Action == action && e.EntityId == userId));

  [Fact]
  public async Task Create_PersistsUserRoleAndAuditTogether()
  {
    var newUser = NewUser(StaffRoles.Operator);

    var result = await InScopeAsync(sp => sp.GetRequiredService<IStaffUserService>()
      .CreateAsync(newUser, Audit(AuditActions.UserCreate, newUser.Id), CancellationToken.None));

    result.IsSuccess.ShouldBeTrue();
    var stored = await InScopeAsync(sp =>
      sp.GetRequiredService<IStaffUserService>().FindByIdAsync(newUser.Id, CancellationToken.None));
    stored.ShouldNotBeNull();
    stored.Roles.ShouldBe([StaffRoles.Operator]);
    (await AuditExistsAsync(AuditActions.UserCreate, newUser.Id)).ShouldBeTrue();
  }

  [Fact]
  public async Task Create_WhenRoleAssignmentFails_RollsBackUserAndAudit()
  {
    var newUser = NewUser("NoSuchRole");

    await Should.ThrowAsync<InvalidOperationException>(() => InScopeAsync(sp => sp
      .GetRequiredService<IStaffUserService>()
      .CreateAsync(newUser, Audit(AuditActions.UserCreate, newUser.Id), CancellationToken.None)));

    var stored = await InScopeAsync(sp =>
      sp.GetRequiredService<IStaffUserService>().FindByIdAsync(newUser.Id, CancellationToken.None));
    stored.ShouldBeNull();
    (await AuditExistsAsync(AuditActions.UserCreate, newUser.Id)).ShouldBeFalse();
  }

  [Fact]
  public async Task Create_WithWeakPassword_ReturnsInvalid_AndWritesNothing()
  {
    var newUser = NewUser(StaffRoles.Operator) with { Password = "weak" };

    var result = await InScopeAsync(sp => sp.GetRequiredService<IStaffUserService>()
      .CreateAsync(newUser, Audit(AuditActions.UserCreate, newUser.Id), CancellationToken.None));

    result.Status.ShouldBe(Ardalis.Result.ResultStatus.Invalid);
    result.ValidationErrors.ShouldAllBe(error => error.Identifier == "Password");
    (await AuditExistsAsync(AuditActions.UserCreate, newUser.Id)).ShouldBeFalse();
  }

  [Fact]
  public async Task Disable_RotatesSecurityStamp_AndWritesAudit()
  {
    var newUser = NewUser(StaffRoles.Operator);
    await InScopeAsync(sp => sp.GetRequiredService<IStaffUserService>()
      .CreateAsync(newUser, Audit(AuditActions.UserCreate, newUser.Id), CancellationToken.None));
    var stampBefore = await InScopeAsync(sp => sp.GetRequiredService<AppDbContext>().Users
      .Where(u => u.Id == newUser.Id).Select(u => u.SecurityStamp).SingleAsync());

    var result = await InScopeAsync(sp => sp.GetRequiredService<IStaffUserService>()
      .DisableAsync(newUser.Id, Audit(AuditActions.UserDisable, newUser.Id), CancellationToken.None));

    result.IsSuccess.ShouldBeTrue();
    var user = await InScopeAsync(sp => sp.GetRequiredService<AppDbContext>().Users
      .AsNoTracking().SingleAsync(u => u.Id == newUser.Id));
    user.Status.ShouldBe(UserStatus.Disabled);
    user.SecurityStamp.ShouldNotBe(stampBefore);
    (await AuditExistsAsync(AuditActions.UserDisable, newUser.Id)).ShouldBeTrue();
  }

  [Fact]
  public async Task ChangeRole_ReplacesRole_AndCountsActiveAdmins()
  {
    var newUser = NewUser(StaffRoles.Operator);
    await InScopeAsync(sp => sp.GetRequiredService<IStaffUserService>()
      .CreateAsync(newUser, Audit(AuditActions.UserCreate, newUser.Id), CancellationToken.None));
    var adminsBefore = await InScopeAsync(sp =>
      sp.GetRequiredService<IStaffUserService>().CountActiveSystemAdminsAsync(CancellationToken.None));

    var result = await InScopeAsync(sp => sp.GetRequiredService<IStaffUserService>().ChangeRoleAsync(newUser.Id,
      StaffRoles.SystemAdmin, Audit(AuditActions.UserChangeRole, newUser.Id), CancellationToken.None));

    result.IsSuccess.ShouldBeTrue();
    result.Value.Roles.ShouldBe([StaffRoles.SystemAdmin]);
    (await InScopeAsync(sp => sp.GetRequiredService<IStaffUserService>()
      .CountActiveSystemAdminsAsync(CancellationToken.None))).ShouldBe(adminsBefore + 1);
    (await AuditExistsAsync(AuditActions.UserChangeRole, newUser.Id)).ShouldBeTrue();
  }

  [Fact]
  public async Task WriteOnUnknownUser_ReturnsNotFound()
  {
    var userId = Guid.NewGuid();

    var result = await InScopeAsync(sp => sp.GetRequiredService<IStaffUserService>()
      .EnableAsync(userId, Audit(AuditActions.UserEnable, userId), CancellationToken.None));

    result.Status.ShouldBe(Ardalis.Result.ResultStatus.NotFound);
    (await AuditExistsAsync(AuditActions.UserEnable, userId)).ShouldBeFalse();
  }
}

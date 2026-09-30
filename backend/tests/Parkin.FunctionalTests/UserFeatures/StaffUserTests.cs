using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Parkin.Api.Domain.StaffUsers;
using Parkin.Api.Features.ApiKeys.Create;
using Parkin.Api.Features.Audit.List;
using Parkin.Api.Features.Auth;
using Parkin.Api.Features.Users;
using Parkin.Api.Features.Users.List;
using Shouldly;
using Xunit;

namespace Parkin.FunctionalTests.UserFeatures;

public class StaffUserTests : IClassFixture<ParkinApiFactory>
{
  private const string AdminEmail = "admin@parkin.local";
  private const string AdminPassword = "Admin!2345";
  private const string NewUserPassword = "Staff!2345";

  private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
  {
    Converters = { new JsonStringEnumConverter() },
  };

  private readonly ParkinApiFactory _factory;

  public StaffUserTests(ParkinApiFactory factory) => _factory = factory;

  private async Task<HttpClient> LoginAsync(string email, string password)
  {
    var client = _factory.CreateClient();
    var response = await client.PostAsJsonAsync("/auth/login", new { Email = email, Password = password });
    response.EnsureSuccessStatusCode();
    return client;
  }

  private static async Task<T> ReadAsync<T>(HttpResponseMessage response) where T : class
  {
    var body = await response.Content.ReadFromJsonAsync<T>(JsonOptions);
    body.ShouldNotBeNull();
    return body;
  }

  private static async Task<UserResponse> CreateUserAsync(HttpClient admin, string role)
  {
    var response = await admin.PostAsJsonAsync("/users", new
    {
      Email = $"staff-{Guid.NewGuid():N}@parkin.local",
      Password = NewUserPassword,
      DisplayName = "Functional Staff",
      Role = role,
    });
    response.StatusCode.ShouldBe(HttpStatusCode.Created);
    return await ReadAsync<UserResponse>(response);
  }

  private static async Task<AuditListResponse> UserAuditAsync(HttpClient admin)
  {
    var response = await admin.GetAsync("/audit?entity=User&per_page=100");
    response.StatusCode.ShouldBe(HttpStatusCode.OK);
    return await ReadAsync<AuditListResponse>(response);
  }

  [Theory]
  [InlineData("GET", "/users")]
  [InlineData("POST", "/users")]
  [InlineData("POST", "/api-keys")]
  public async Task AdminEndpoints_AsOperator_Return403(string method, string path)
  {
    using var client = await LoginAsync("operator@parkin.local", "Operator!2345");

    var response = await client.SendAsync(new HttpRequestMessage(new HttpMethod(method), path)
    {
      Content = method == "GET" ? null : JsonContent.Create(new { }),
    });

    response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
  }

  [Fact]
  public async Task Create_WritesUserAndAuditEntry_AndNewUserCanSignIn()
  {
    using var admin = await LoginAsync(AdminEmail, AdminPassword);
    using var adminMe = await admin.GetAsync("/auth/me");
    var adminUser = await ReadAsync<CurrentUserResponse>(adminMe);

    var created = await CreateUserAsync(admin, StaffRoles.Operator);

    created.Role.ShouldBe(StaffRoles.Operator);
    created.Status.ShouldBe(nameof(UserStatus.Active));
    var audit = await UserAuditAsync(admin);
    audit.Items.ShouldContain(e =>
      e.EntityId == created.Id && e.Action == "user.create" && e.ActorId == adminUser.Id);

    using var staff = await LoginAsync(created.Email, NewUserPassword);
    var me = await ReadAsync<CurrentUserResponse>(await staff.GetAsync("/auth/me"));
    me.Id.ShouldBe(created.Id);
    me.Roles.ShouldBe([StaffRoles.Operator]);
  }

  [Fact]
  public async Task Create_WithWeakPassword_Returns400_AndCreatesNoUser()
  {
    using var admin = await LoginAsync(AdminEmail, AdminPassword);
    var email = $"weak-{Guid.NewGuid():N}@parkin.local";

    var response = await admin.PostAsJsonAsync("/users",
      new { Email = email, Password = "weak", DisplayName = "Weak", Role = StaffRoles.Operator });

    response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    var users = await ReadAsync<UserListResponse>(await admin.GetAsync("/users?per_page=100"));
    users.Items.ShouldNotContain(u => u.Email == email);
  }

  [Fact]
  public async Task Disable_LastActiveSystemAdmin_Returns409()
  {
    using var admin = await LoginAsync(AdminEmail, AdminPassword);
    var users = await ReadAsync<UserListResponse>(await admin.GetAsync("/users?per_page=100"));
    var activeAdmins = users.Items
      .Where(u => u.Role == StaffRoles.SystemAdmin && u.Status == nameof(UserStatus.Active))
      .ToList();
    foreach (var extraAdmin in activeAdmins.Where(u => u.Email != AdminEmail))
    {
      (await admin.PostAsJsonAsync($"/users/{extraAdmin.Id}/disable", new { })).EnsureSuccessStatusCode();
    }
    var seededAdmin = activeAdmins.Single(u => u.Email == AdminEmail);

    var disable = await admin.PostAsJsonAsync($"/users/{seededAdmin.Id}/disable", new { });
    var demote = await admin.PatchAsJsonAsync($"/users/{seededAdmin.Id}/role", new { Role = StaffRoles.Operator });

    disable.StatusCode.ShouldBe(HttpStatusCode.Conflict);
    demote.StatusCode.ShouldBe(HttpStatusCode.Conflict);
  }

  [Fact]
  public async Task ChangeRoleThenDisable_AuditsBoth_AndBlocksSignIn()
  {
    using var admin = await LoginAsync(AdminEmail, AdminPassword);
    var created = await CreateUserAsync(admin, StaffRoles.Operator);

    var promote = await admin.PatchAsJsonAsync($"/users/{created.Id}/role", new { Role = StaffRoles.SystemAdmin });
    promote.StatusCode.ShouldBe(HttpStatusCode.OK);
    (await ReadAsync<UserResponse>(promote)).Role.ShouldBe(StaffRoles.SystemAdmin);

    var disable = await admin.PostAsJsonAsync($"/users/{created.Id}/disable", new { });
    disable.StatusCode.ShouldBe(HttpStatusCode.NoContent);

    var audit = await UserAuditAsync(admin);
    audit.Items.ShouldContain(e => e.EntityId == created.Id && e.Action == "user.change_role");
    audit.Items.ShouldContain(e => e.EntityId == created.Id && e.Action == "user.disable");

    using var client = _factory.CreateClient();
    var login = await client.PostAsJsonAsync("/auth/login", new { created.Email, Password = NewUserPassword });
    login.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);

    var enable = await admin.PostAsJsonAsync($"/users/{created.Id}/enable", new { });
    enable.StatusCode.ShouldBe(HttpStatusCode.NoContent);
  }

  [Fact]
  public async Task UnknownUser_Returns404()
  {
    using var admin = await LoginAsync(AdminEmail, AdminPassword);

    var response = await admin.PostAsJsonAsync($"/users/{Guid.NewGuid()}/disable", new { });

    response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
  }

  [Fact]
  public async Task RevokeApiKey_Twice_Returns409()
  {
    using var admin = await LoginAsync(AdminEmail, AdminPassword);
    var createResponse = await admin.PostAsJsonAsync("/api-keys", new { Name = $"Gate {Guid.NewGuid():N}" });
    var created = await ReadAsync<CreateApiKeyResponse>(createResponse);

    var first = await admin.PostAsync($"/api-keys/{created.Id}/revoke", null);
    var second = await admin.PostAsync($"/api-keys/{created.Id}/revoke", null);

    first.StatusCode.ShouldBe(HttpStatusCode.OK);
    second.StatusCode.ShouldBe(HttpStatusCode.Conflict);
  }
}

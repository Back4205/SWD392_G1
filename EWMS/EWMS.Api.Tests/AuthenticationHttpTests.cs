using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using EWMS.Api.DTOs.Auth;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace EWMS.Api.Tests;

[Collection("Receiving SQL")]
public class AuthenticationHttpTests
{
    private readonly SqlServerFixture _fixture;
    public AuthenticationHttpTests(SqlServerFixture fixture) { _fixture = fixture; }

    [Fact]
    public async Task LoginRequiresRealPasswordAndCsrfAndHeaderCannotImpersonate()
    {
        var data = await _fixture.SeedAsync();
        await using var app = await new ReceivingHttpTests(_fixture).StartAsync();
        using var client = ReceivingHttpTests.Client(app);
        using var db = _fixture.CreateContext();
        var userName = (await db.Users.FindAsync(data.StaffId))!.UserName;
        client.DefaultRequestHeaders.Add("X-Development-UserId", data.ManagerId.ToString());
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/goods-receipts")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/api/auth/login", new {userName, password=SqlServerFixture.TestPassword})).StatusCode);
        await AuthTestHelper.RefreshCsrfAsync(client);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsJsonAsync("/api/auth/login", new {userName, password="wrong password"})).StatusCode);
        await AuthTestHelper.LoginAsync(client, userName);
        var me = await client.GetFromJsonAsync<AuthUserDto>("/api/auth/me");
        Assert.Equal(data.StaffId, me!.UserId);
        Assert.Equal("Warehouse Staff", me.Role);
        client.DefaultRequestHeaders.Remove("X-CSRF-TOKEN");
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/api/auth/logout", new {})).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/auth/me")).StatusCode);
        await app.StopAsync();
    }

    [Fact]
    public async Task LogoutRevokesSavedJwtAndInactiveAccountCannotAuthenticate()
    {
        var data = await _fixture.SeedAsync();
        await using var app = await new ReceivingHttpTests(_fixture).StartAsync();
        using var client = ReceivingHttpTests.Client(app);
        using var db = _fixture.CreateContext();
        await AuthTestHelper.LoginAsync(client, (await db.Users.FindAsync(data.StaffId))!.UserName);
        string savedJwt;
        await AuthTestHelper.RefreshCsrfAsync(client);
        var login = await client.PostAsJsonAsync("/api/auth/login", new {userName=(await db.Users.FindAsync(data.StaffId))!.UserName,password=SqlServerFixture.TestPassword});
        var result = await login.Content.ReadFromJsonAsync<JsonElement>();
        savedJwt = result.GetProperty("token").GetString()!;
        await AuthTestHelper.RefreshCsrfAsync(client);
        Assert.Equal(HttpStatusCode.NoContent, (await client.PostAsJsonAsync("/api/auth/logout",new {})).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/auth/me")).StatusCode);
        using var attacker = ReceivingHttpTests.Client(app);
        attacker.DefaultRequestHeaders.Add("Cookie", ".EWMS.Session="+savedJwt);
        Assert.Equal(HttpStatusCode.Unauthorized, (await attacker.GetAsync("/api/auth/me")).StatusCode);
        await AuthTestHelper.RefreshCsrfAsync(client);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsJsonAsync("/api/auth/login",new {userName=(await db.Users.FindAsync(data.InactiveUserId))!.UserName,password=SqlServerFixture.TestPassword})).StatusCode);
        await app.StopAsync();
    }

    [Fact]
    public async Task PasswordChangeRevokesAllSessionsAndRejectsOldPassword()
    {
        var data = await _fixture.SeedAsync();
        await using var app = await new ReceivingHttpTests(_fixture).StartAsync();
        using var client = ReceivingHttpTests.Client(app);
        using var second = ReceivingHttpTests.Client(app);
        using var db = _fixture.CreateContext();
        string userName = (await db.Users.FindAsync(data.StaffId))!.UserName;
        await AuthTestHelper.LoginAsync(client,userName);
        await AuthTestHelper.LoginAsync(second,userName);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/api/auth/change-password",new {currentPassword="wrong",newPassword="New-Test-Password-42!",confirmPassword="New-Test-Password-42!"})).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await client.PostAsJsonAsync("/api/auth/change-password",new {currentPassword=SqlServerFixture.TestPassword,newPassword="New-Test-Password-42!",confirmPassword="New-Test-Password-42!"})).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await second.GetAsync("/api/auth/me")).StatusCode);
        await AuthTestHelper.RefreshCsrfAsync(client);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsJsonAsync("/api/auth/login",new {userName,password=SqlServerFixture.TestPassword})).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.PostAsJsonAsync("/api/auth/login",new {userName,password="New-Test-Password-42!"})).StatusCode);
        await app.StopAsync();
    }

    [Fact]
    public async Task ChangingRoleOrDisablingAccountInvalidatesExistingSession()
    {
        var data = await _fixture.SeedAsync();
        await using var app = await new ReceivingHttpTests(_fixture).StartAsync();
        using var client = ReceivingHttpTests.Client(app);
        using var db = _fixture.CreateContext();
        var user = (await db.Users.FindAsync(data.StaffId))!;
        await AuthTestHelper.LoginAsync(client,user.UserName);
        user.Status="Inactive";
        await db.SaveChangesAsync();
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/auth/me")).StatusCode);
        user.Status="Active";
        await db.SaveChangesAsync();
        await AuthTestHelper.LoginAsync(client,user.UserName);
        user.RoleId=(await db.Users.FindAsync(data.ManagerId))!.RoleId;
        await db.SaveChangesAsync();
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/auth/me")).StatusCode);
        await app.StopAsync();
    }

    [Fact]
    public async Task InboundRoutesEnforceRoleAndWarehouseScope()
    {
        var data = await _fixture.SeedAsync();
        await using var app = await new ReceivingHttpTests(_fixture).StartAsync();
        using var client = ReceivingHttpTests.Client(app);
        using var db = _fixture.CreateContext();
        var users=await db.Users.Where(x=>new[]{data.StaffId,data.ManagerId,data.PurchasingUserId,data.OtherUserId}.Contains(x.Id)).ToDictionaryAsync(x=>x.Id,x=>x.UserName);
        await AuthTestHelper.LoginAsync(client,users[data.StaffId]);
        Assert.Equal(HttpStatusCode.Forbidden,(await client.PostAsJsonAsync("/api/inbound-requests",new {warehouseId=data.WarehouseId,items=new[]{new{productVariantId=data.VariantId,expectedQty=2}}})).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden,(await client.PutAsJsonAsync($"/api/inbound-requests/{data.InboundId}/review",new{decision="Approved"})).StatusCode);
        await AuthTestHelper.LoginAsync(client,users[data.ManagerId]);
        Assert.Equal(HttpStatusCode.Forbidden,(await client.PostAsJsonAsync("/api/inbound-requests",new {warehouseId=data.OtherWarehouseId,items=new[]{new{productVariantId=data.VariantId,expectedQty=2}}})).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden,(await client.PutAsJsonAsync($"/api/inbound-requests/{data.InboundId}/supplier-commitment",new{supplierId=1,commitmentStatus="Confirmed"})).StatusCode);
        var create=await client.PostAsJsonAsync("/api/inbound-requests",new {warehouseId=data.WarehouseId,items=new[]{new{productVariantId=data.VariantId,expectedQty=2}}});
        Assert.Equal(HttpStatusCode.Created,create.StatusCode);
        int id=(await create.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetInt32();
        Assert.Equal(data.ManagerId,(await db.InboundRequests.FindAsync(id))!.RequestedByUserId);
        await AuthTestHelper.LoginAsync(client,users[data.StaffId]);
        Assert.Equal(HttpStatusCode.Forbidden,(await client.GetAsync($"/api/inbound-requests/{id}")).StatusCode);
        await AuthTestHelper.LoginAsync(client,users[data.OtherUserId]);
        Assert.Equal(HttpStatusCode.Forbidden,(await client.GetAsync($"/api/inbound-requests/{data.InboundId}")).StatusCode);
        await AuthTestHelper.LoginAsync(client,users[data.PurchasingUserId]);
        Assert.Equal(HttpStatusCode.Forbidden,(await client.GetAsync("/api/goods-receipts")).StatusCode);
        Assert.Equal(HttpStatusCode.OK,(await client.GetAsync("/api/inbound-requests/lookups")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden,(await client.PutAsJsonAsync($"/api/inbound-requests/{id}/review",new{decision="Approved"})).StatusCode);
        await app.StopAsync();
    }

    [Fact]
    public async Task ExpiredDatabaseSessionAndAlteredJwtAreRejected()
    {
        var data = await _fixture.SeedAsync();
        await using var app = await new ReceivingHttpTests(_fixture).StartAsync();
        using var client = ReceivingHttpTests.Client(app);
        using var db = _fixture.CreateContext();
        string userName = (await db.Users.FindAsync(data.StaffId))!.UserName;
        await AuthTestHelper.RefreshCsrfAsync(client);
        var login = await client.PostAsJsonAsync("/api/auth/login", new { userName, password = SqlServerFixture.TestPassword });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        string cookie = Assert.Single(login.Headers.GetValues("Set-Cookie"), x => x.StartsWith(".EWMS.Session="));
        Assert.Contains("httponly", cookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("samesite=strict", cookie, StringComparison.OrdinalIgnoreCase);
        string token = (await login.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("token").GetString()!;
        using var attacker = ReceivingHttpTests.Client(app);
        attacker.DefaultRequestHeaders.Add("Cookie", ".EWMS.Session=" + "x" + token[1..]);
        Assert.Equal(HttpStatusCode.Unauthorized, (await attacker.GetAsync("/api/auth/me")).StatusCode);
        var session = await db.AuthSessions.SingleAsync(x => x.UserId == data.StaffId);
        session.ExpiresAt = DateTime.UtcNow.AddMinutes(-1);
        await db.SaveChangesAsync();
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/auth/me")).StatusCode);
        await app.StopAsync();
    }

    [Fact]
    public async Task RepeatedFailedLoginsAreRateLimited()
    {
        await using var app = await new ReceivingHttpTests(_fixture).StartAsync();
        using var client = ReceivingHttpTests.Client(app);
        await AuthTestHelper.RefreshCsrfAsync(client);
        for (int attempt = 0; attempt < 10; attempt++)
        {
            var response = await client.PostAsJsonAsync("/api/auth/login", new { userName = "missing-user", password = "incorrect" });
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }
        Assert.Equal(HttpStatusCode.TooManyRequests, (await client.PostAsJsonAsync("/api/auth/login",
            new { userName = "missing-user", password = "incorrect" })).StatusCode);
        await app.StopAsync();
    }
}

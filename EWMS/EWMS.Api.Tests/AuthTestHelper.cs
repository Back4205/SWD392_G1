using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace EWMS.Api.Tests;

internal static class AuthTestHelper
{
    public static async Task RefreshCsrfAsync(HttpClient client)
    {
        client.DefaultRequestHeaders.Remove("X-CSRF-TOKEN");
        var response = await client.GetAsync("/api/auth/csrf");
        Assert.True(response.IsSuccessStatusCode, await response.Content.ReadAsStringAsync());
        var result = (await response.Content.ReadFromJsonAsync<JsonElement>());
        client.DefaultRequestHeaders.Add("X-CSRF-TOKEN", result.GetProperty("requestToken").GetString());
    }
    public static async Task LoginAsync(HttpClient client, string userName)
    {
        await RefreshCsrfAsync(client);
        var response = await client.PostAsJsonAsync("/api/auth/login", new { userName, password = SqlServerFixture.TestPassword });
        Assert.True(response.IsSuccessStatusCode, await response.Content.ReadAsStringAsync());
        await RefreshCsrfAsync(client);
    }
}

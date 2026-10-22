using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace WcxTimeOff.Api.Tests;

public class AuthorizationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private const string Key = "DEV-ONLY-wcx-timeoff-exam-jwt-key-change-me!";
    private readonly WebApplicationFactory<Program> _factory;

    public AuthorizationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((_, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Seed:Enabled"] = "false",
                    ["Jwt:Key"] = Key
                });
            });
        });
    }

    [Fact]
    public async Task Admin_endpoints_reject_anonymous_callers()
    {
        var response = await _factory.CreateClient().GetAsync("/api/admin/agents");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Agent_token_cannot_access_admin_credit_management()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", CreateToken("Agent"));
        var response = await client.GetAsync("/api/admin/agents");
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Admin_token_cannot_submit_an_agent_time_off_request()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", CreateToken("Admin"));
        var response = await client.PostAsync(
            "/api/timeoff-requests",
            new StringContent("{}", Encoding.UTF8, "application/json"));
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    private static string CreateToken(string role)
    {
        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Key)),
            SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            issuer: "WcxTimeOff",
            audience: "WcxTimeOff",
            claims:
            [
                new Claim(JwtRegisteredClaimNames.Sub, "user-1"),
                new Claim(ClaimTypes.NameIdentifier, "user-1"),
                new Claim(ClaimTypes.Role, role)
            ],
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: credentials);
        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}

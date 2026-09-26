using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace LabAPIServer.Api.Tests;

public sealed class JwtValidationTests : IClassFixture<JwtTestWebApplicationFactory>
{
    private readonly JwtTestWebApplicationFactory factory;

    public JwtValidationTests(JwtTestWebApplicationFactory factory)
    {
        this.factory = factory;
    }

    [Fact]
    public async Task Session_requires_authentication()
    {
        using var client = factory.CreateClient();

        using var response = await client.GetAsync("/api/v1/session");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Session_accepts_valid_jwt_and_returns_verified_identity()
    {
        using var client = factory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/session");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", factory.CreateValidToken());

        using var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadAsStringAsync();
        using var json = JsonDocument.Parse(body);
        Assert.Equal("test-user", json.RootElement.GetProperty("subject").GetString());
        Assert.Equal("Reader", json.RootElement.GetProperty("role").GetString());
        Assert.True(json.RootElement.TryGetProperty("expiresAt", out var expiresAt));
        Assert.False(string.IsNullOrWhiteSpace(expiresAt.GetString()));
    }

    [Fact]
    public async Task Session_rejects_invalid_signature()
    {
        using var client = factory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/session");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", factory.CreateTokenWithDifferentSigningKey());

        using var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Session_rejects_wrong_issuer()
    {
        using var client = factory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/session");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", factory.CreateToken(issuer: "https://wrong.example"));

        using var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Session_rejects_wrong_audience()
    {
        using var client = factory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/session");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", factory.CreateToken(audience: "WrongAudience"));

        using var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Session_rejects_expired_token()
    {
        using var client = factory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/session");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", factory.CreateToken(
            issuedAt: DateTime.UtcNow.AddMinutes(-30),
            expiresAt: DateTime.UtcNow.AddMinutes(-5)));

        using var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}

public sealed class JwtTestWebApplicationFactory : WebApplicationFactory<Program>
{
    private const string TestIssuer = "https://DC01.lab.local";
    private const string TestAudience = "LabAuthServer.API";
    private readonly RSA signingKey = RSA.Create(2048);
    private readonly RSA alternateSigningKey = RSA.Create(2048);

    public string CreateValidToken() => CreateToken();

    public string CreateTokenWithDifferentSigningKey() => CreateToken(signingKey: alternateSigningKey);

    public string CreateToken(
        string issuer = TestIssuer,
        string audience = TestAudience,
        string subject = "test-user",
        string role = "Reader",
        DateTime? issuedAt = null,
        DateTime? expiresAt = null,
        RSA? signingKey = null)
    {
        signingKey ??= this.signingKey;
        var tokenIssuedAt = issuedAt ?? DateTime.UtcNow.AddMinutes(-1);
        var expiry = expiresAt ?? tokenIssuedAt.AddMinutes(10);

        var claims = new List<Claim>
        {
            new("sub", subject),
            new("role", role),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N")),
            new(JwtRegisteredClaimNames.Iat, EpochTime.GetIntDate(tokenIssuedAt).ToString()),
            new(JwtRegisteredClaimNames.Nbf, EpochTime.GetIntDate(tokenIssuedAt).ToString()),
            new(JwtRegisteredClaimNames.Exp, EpochTime.GetIntDate(expiry).ToString())
        };

        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = issuer,
            Audience = audience,
            Subject = new ClaimsIdentity(claims, "TestAuth"),
            NotBefore = tokenIssuedAt,
            IssuedAt = tokenIssuedAt,
            Expires = expiry,
            SigningCredentials = new SigningCredentials(new RsaSecurityKey(signingKey), SecurityAlgorithms.RsaSha256)
        };

        var handler = new JwtSecurityTokenHandler();
        var token = handler.CreateToken(descriptor);
        return handler.WriteToken(token);
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.ConfigureAppConfiguration((_, configBuilder) =>
        {
            configBuilder.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["JwtValidation:Issuer"] = TestIssuer,
                ["JwtValidation:Audience"] = TestAudience,
                ["JwtValidation:SigningAlgorithm"] = SecurityAlgorithms.RsaSha256,
                ["JwtValidation:ClockSkewSeconds"] = "300",
                ["JwtValidation:PublicKeyPem"] = ExportPublicKeyPem(signingKey)
            });
        });
    }

    private static string ExportPublicKeyPem(RSA rsa)
    {
        var publicKeyBytes = rsa.ExportSubjectPublicKeyInfo();
        return PemEncoding.WriteString("PUBLIC KEY", publicKeyBytes);
    }
}

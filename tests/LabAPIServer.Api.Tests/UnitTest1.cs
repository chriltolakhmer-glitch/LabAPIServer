namespace LabAPIServer.Api.Tests;

using System.Net;
using System.Security.Cryptography;
using LabAPIServer.Api.Data;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

public sealed class ApiFoundationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> factory;

    public ApiFoundationTests(WebApplicationFactory<Program> factory)
    {
        this.factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((_, configBuilder) =>
            {
                configBuilder.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["JwtValidation:Issuer"] = "https://DC01.lab.local",
                    ["JwtValidation:Audience"] = "LabAuthServer.API",
                    ["JwtValidation:SigningAlgorithm"] = SecurityAlgorithms.RsaSha256,
                    ["JwtValidation:ClockSkewSeconds"] = "300",
                    ["JwtValidation:PublicKeyPem"] = CreatePublicKeyPem()
                });
            });
        });
    }

    [Fact]
    public async Task Health_returns_healthy_without_database_configuration()
    {
        using var client = factory.CreateClient();

        using var response = await client.GetAsync("/api/v1/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("{\"status\":\"Healthy\"}", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public void Database_options_reject_trust_server_certificate()
    {
        var validator = new DatabaseOptionsValidator();
        var result = validator.Validate(Options.DefaultName, new DatabaseOptions
        {
            ConnectionString = "Server=localhost;Database=LabApplication_Dev;Encrypt=True;TrustServerCertificate=True;Integrated Security=True"
        });

        Assert.True(result.Failed);
    }

    [Fact]
    public void Connection_factory_requires_an_explicit_target()
    {
        var factory = new SqlConnectionFactory(Options.Create(new DatabaseOptions()));

        var exception = Assert.Throws<InvalidOperationException>(() => factory.CreateConnection());

        Assert.Contains("not configured", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    private static string CreatePublicKeyPem()
    {
        using var rsa = RSA.Create(2048);
        return PemEncoding.WriteString("PUBLIC KEY", rsa.ExportSubjectPublicKeyInfo());
    }
}

using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Security.Cryptography;
using LabAPIServer.Api.OperationalStatuses;
using LabAPIServer.Api.WorkItems;
using Microsoft.Data.SqlClient;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace LabAPIServer.Api.Tests;

public sealed class LocalDbIntegrationTests : IClassFixture<LocalDbWebApplicationFactory>
{
    private readonly LocalDbWebApplicationFactory factory;

    public LocalDbIntegrationTests(LocalDbWebApplicationFactory factory)
    {
        this.factory = factory;
    }

    [Fact]
    public async Task LocalDb_supports_http_work_item_crud_and_reader_denial()
    {
        if (!string.Equals(Environment.GetEnvironmentVariable("LABAPI_RUN_SQL_TESTS"), "1", StringComparison.Ordinal))
        {
            throw Xunit.Sdk.SkipException.ForSkip("Set LABAPI_RUN_SQL_TESTS=1 to run against the approved isolated SQL target.");
        }

        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", factory.CreateToken("Operator"));
        Guid? createdId = null;
        try
        {
            var listResponse = await client.GetAsync("/api/v1/work-items");
            Assert.Equal(HttpStatusCode.OK, listResponse.StatusCode);
            var baseline = await listResponse.Content.ReadFromJsonAsync<WorkItemDto[]>();
            Assert.Equal(6, baseline!.Length);

            using var createResponse = await client.PostAsJsonAsync(
                "/api/v1/work-items",
                new { name = "[TEST RUN] LocalDB CRUD", description = "Synthetic integration row.", status = "Open" });
            Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
            var created = await createResponse.Content.ReadFromJsonAsync<WorkItemDto>();
            Assert.NotNull(created);
            createdId = created!.Id;

            Assert.Equal(HttpStatusCode.OK, (await client.GetAsync($"/api/v1/work-items/{created.Id}")).StatusCode);
            using var updateResponse = await client.PutAsJsonAsync(
                $"/api/v1/work-items/{created.Id}",
                new { name = "[TEST RUN] LocalDB CRUD updated", description = (string?)null, status = "Complete" });
            Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);

            using var reader = factory.CreateClient();
            reader.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", factory.CreateToken("Reader"));
            Assert.Equal(HttpStatusCode.OK, (await reader.GetAsync("/api/v1/work-items")).StatusCode);
            using var forbiddenCreate = await reader.PostAsJsonAsync(
                "/api/v1/work-items",
                new { name = "[TEST RUN] should be denied", status = "Open" });
            Assert.Equal(HttpStatusCode.Forbidden, forbiddenCreate.StatusCode);

            Assert.Equal(HttpStatusCode.NoContent, (await client.DeleteAsync($"/api/v1/work-items/{created.Id}")).StatusCode);
            Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/api/v1/work-items/{created.Id}")).StatusCode);
        }
        finally
        {
            if (createdId is { } id)
            {
                await client.DeleteAsync($"/api/v1/work-items/{id}");
            }
        }
    }

    [Fact]
    public async Task IsolatedSql_supports_lifecycle_history_reader_denial_and_stale_conflict()
    {
        if (!string.Equals(Environment.GetEnvironmentVariable("LABAPI_RUN_SQL_TESTS"), "1", StringComparison.Ordinal))
        {
            throw Xunit.Sdk.SkipException.ForSkip("Set LABAPI_RUN_SQL_TESTS=1 to run against the approved isolated SQL target.");
        }

        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", factory.CreateToken("Operator"));
        var original = await (await client.GetAsync("/api/v1/operational-statuses/phase14-fixture")).Content.ReadFromJsonAsync<OperationalStatusDto>();
        Assert.NotNull(original);
        Assert.Equal(OperationalStatusLifecycle.Open, original!.Status);
        using var updateResponse = await client.PutAsJsonAsync(
                "/api/v1/operational-statuses/phase14-fixture",
                new { status = OperationalStatusLifecycle.InProgress, value = "Phase 14 updated", severity = "Warning", rowVersion = original.RowVersion });
        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);
            var updated = await updateResponse.Content.ReadFromJsonAsync<OperationalStatusDto>();
            Assert.Equal(OperationalStatusLifecycle.InProgress, updated!.Status);
            Assert.Equal("Phase 14 updated", updated.Value);

            using var staleResponse = await client.PutAsJsonAsync(
                "/api/v1/operational-statuses/phase14-fixture",
                new { status = OperationalStatusLifecycle.Complete, value = "Stale update", severity = "Critical", rowVersion = original.RowVersion });
            Assert.Equal(HttpStatusCode.Conflict, staleResponse.StatusCode);

            using var completeResponse = await client.PutAsJsonAsync(
                "/api/v1/operational-statuses/phase14-fixture",
                new { status = OperationalStatusLifecycle.Complete, value = "Phase 14 complete", severity = "Critical", rowVersion = updated.RowVersion });
            Assert.Equal(HttpStatusCode.OK, completeResponse.StatusCode);
            var completed = await completeResponse.Content.ReadFromJsonAsync<OperationalStatusDto>();
            Assert.Equal(OperationalStatusLifecycle.Complete, completed!.Status);

            var history = await (await client.GetAsync("/api/v1/operational-statuses/phase14-fixture/history")).Content.ReadFromJsonAsync<OperationalStatusHistoryDto[]>();
            Assert.Equal([OperationalStatusLifecycle.InProgress, OperationalStatusLifecycle.Complete], history!.Select(entry => entry.NewStatus));

            using var reopeningResponse = await client.PutAsJsonAsync(
                "/api/v1/operational-statuses/phase14-fixture",
                new { status = OperationalStatusLifecycle.Open, value = "Reopen", severity = "Critical", rowVersion = completed.RowVersion });
            Assert.Equal(HttpStatusCode.UnprocessableEntity, reopeningResponse.StatusCode);

            using var reader = factory.CreateClient();
            reader.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", factory.CreateToken("Reader"));
            Assert.Equal(HttpStatusCode.OK, (await reader.GetAsync("/api/v1/operational-statuses/phase14-fixture/history")).StatusCode);
            using var forbidden = await reader.PutAsJsonAsync(
                "/api/v1/operational-statuses/phase14-fixture",
                new { status = OperationalStatusLifecycle.Open, value = "Denied", severity = "Info", rowVersion = completed.RowVersion });
            Assert.Equal(HttpStatusCode.Forbidden, forbidden.StatusCode);
    }
}

public sealed class LocalDbWebApplicationFactory : WebApplicationFactory<Program>
{
    private const string Issuer = "https://DC01.lab.local";
    private const string Audience = "LabAuthServer.API";
    private readonly RSA signingKey = RSA.Create(2048);

    public string CreateToken(string role)
    {
        var now = DateTime.UtcNow;
        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = Issuer,
            Audience = Audience,
            Subject = new ClaimsIdentity([
                new Claim("sub", "localdb-test-user"),
                new Claim("role", role),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N")),
                new Claim(JwtRegisteredClaimNames.Iat, EpochTime.GetIntDate(now).ToString()),
                new Claim(JwtRegisteredClaimNames.Nbf, EpochTime.GetIntDate(now).ToString())
            ], "TestAuth"),
            NotBefore = now,
            IssuedAt = now,
            Expires = now.AddMinutes(10),
            SigningCredentials = new SigningCredentials(new RsaSecurityKey(signingKey), SecurityAlgorithms.RsaSha256)
        };

        var handler = new JwtSecurityTokenHandler();
        return handler.WriteToken(handler.CreateToken(descriptor));
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.ConfigureAppConfiguration((_, configBuilder) =>
        {
            var connectionString = Environment.GetEnvironmentVariable("LABAPI_TEST_CONNECTION_STRING");
            if (string.Equals(Environment.GetEnvironmentVariable("LABAPI_RUN_SQL_TESTS"), "1", StringComparison.Ordinal))
            {
                if (string.IsNullOrWhiteSpace(connectionString))
                {
                    throw new InvalidOperationException("LABAPI_TEST_CONNECTION_STRING must explicitly target DC01/LabAPIServer_Test.");
                }

                var connection = new SqlConnectionStringBuilder(connectionString);
                var server = connection.DataSource.Replace("tcp:", string.Empty, StringComparison.OrdinalIgnoreCase);
                if (!string.Equals(server, "DC01", StringComparison.OrdinalIgnoreCase)
                    && !string.Equals(server, "DC01,1433", StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidOperationException("SQL integration is restricted to DC01.");
                }

                if (!string.Equals(connection.InitialCatalog, "LabAPIServer_Test", StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidOperationException("SQL integration is restricted to LabAPIServer_Test.");
                }
            }

            configBuilder.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Database:ConnectionString"] = connectionString ?? string.Empty,
                ["JwtValidation:Issuer"] = Issuer,
                ["JwtValidation:Audience"] = Audience,
                ["JwtValidation:SigningAlgorithm"] = SecurityAlgorithms.RsaSha256,
                ["JwtValidation:ClockSkewSeconds"] = "300",
                ["JwtValidation:PublicKeyPem"] = PemEncoding.WriteString("PUBLIC KEY", signingKey.ExportSubjectPublicKeyInfo())
            });
        });
    }
}

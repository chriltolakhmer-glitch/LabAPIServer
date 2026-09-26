using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Security.Cryptography;
using LabAPIServer.Api.WorkItems;
using LabAPIServer.Api.OperationalStatuses;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.IdentityModel.Tokens;

namespace LabAPIServer.Api.Tests;

public sealed class WorkItemUnitTests
{
    [Fact]
    public void Request_validator_rejects_unknown_status()
    {
        var errors = WorkItemRequestValidator.Validate(new CreateWorkItemRequest
        {
            Name = "Example",
            Status = "Paused"
        });

        Assert.Contains("Status", errors.Keys);
    }

    [Fact]
    public void Request_validator_rejects_blank_name_and_status()
    {
        var errors = WorkItemRequestValidator.Validate(new CreateWorkItemRequest
        {
            Name = "   ",
            Status = null!
        });

        Assert.Contains("Name", errors.Keys);
        Assert.Contains("Status", errors.Keys);
    }

    [Fact]
    public async Task Service_create_sets_identity_and_timestamps()
    {
        var store = new FakeWorkItemStore();
        var service = new WorkItemService(store, TimeProvider.System);

        var item = await service.CreateAsync(new CreateWorkItemRequest
        {
            Name = "Example",
            Description = "Details",
            Status = WorkItemStatuses.Open
        }, "operator@lab.local", CancellationToken.None);

        Assert.Equal("operator@lab.local", item.CreatedBy);
        Assert.Equal("operator@lab.local", item.UpdatedBy);
        Assert.Equal("Example", item.Name);
        Assert.Equal(WorkItemStatuses.Open, item.Status);
        Assert.Equal(item.CreatedAtUtc, item.UpdatedAtUtc);
    }
}

public sealed class WorkItemApiTests : IClassFixture<WorkItemWebApplicationFactory>
{
    private readonly WorkItemWebApplicationFactory factory;

    public WorkItemApiTests(WorkItemWebApplicationFactory factory)
    {
        this.factory = factory;
    }

    [Fact]
    public async Task Work_items_require_authentication()
    {
        using var client = factory.CreateClient();

        using var response = await client.GetAsync("/api/v1/work-items");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Reader_can_read_work_items()
    {
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", factory.CreateToken("Reader"));

        using var response = await client.GetAsync("/api/v1/work-items");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Reader_cannot_create_work_items()
    {
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", factory.CreateToken("Reader"));
        using var content = JsonContent.Create(new { name = "Example", status = "Open" });

        using var response = await client.PostAsync("/api/v1/work-items", content);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Reader_cannot_update_or_delete_work_items()
    {
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", factory.CreateToken("Reader"));
        var id = Guid.NewGuid();
        using var updateContent = JsonContent.Create(new { name = "Example", status = "Open" });

        using var updateResponse = await client.PutAsync($"/api/v1/work-items/{id}", updateContent);
        using var deleteResponse = await client.DeleteAsync($"/api/v1/work-items/{id}");

        Assert.Equal(HttpStatusCode.Forbidden, updateResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, deleteResponse.StatusCode);
    }

    [Fact]
    public async Task Operator_can_create_work_items()
    {
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", factory.CreateToken("Operator"));
        using var content = JsonContent.Create(new { name = "Example", description = "Details", status = "Open" });

        using var response = await client.PostAsync("/api/v1/work-items", content);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task Authenticated_roles_can_read_and_operator_can_complete_crud()
    {
        using var reader = factory.CreateClient();
        reader.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", factory.CreateToken("Reader"));
        Assert.Equal(HttpStatusCode.OK, (await reader.GetAsync("/api/v1/work-items")).StatusCode);

        using var operatorClient = factory.CreateClient();
        operatorClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", factory.CreateToken("Operator"));
        using var createContent = JsonContent.Create(new { name = "CRUD item", description = "Details", status = "Open" });
        using var createResponse = await operatorClient.PostAsync("/api/v1/work-items", createContent);
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var created = await createResponse.Content.ReadFromJsonAsync<WorkItemDto>();
        Assert.NotNull(created);

        using var getResponse = await operatorClient.GetAsync($"/api/v1/work-items/{created!.Id}");
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);

        using var updateContent = JsonContent.Create(new { name = "CRUD item updated", description = (string?)null, status = "Complete" });
        using var updateResponse = await operatorClient.PutAsync($"/api/v1/work-items/{created.Id}", updateContent);
        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);
        var updated = await updateResponse.Content.ReadFromJsonAsync<WorkItemDto>();
        Assert.Equal("Complete", updated!.Status);
        Assert.Null(updated.Description);

        using var deleteResponse = await operatorClient.DeleteAsync($"/api/v1/work-items/{created.Id}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await operatorClient.GetAsync($"/api/v1/work-items/{created.Id}")).StatusCode);
    }

    [Fact]
    public async Task Administrator_can_perform_work_item_crud_by_explicit_policy()
    {
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", factory.CreateToken("Administrator"));
        using var content = JsonContent.Create(new { name = "Administrator item", status = "InProgress" });

        using var createResponse = await client.PostAsync("/api/v1/work-items", content);
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var created = await createResponse.Content.ReadFromJsonAsync<WorkItemDto>();
        Assert.NotNull(created);

        using var updateContent = JsonContent.Create(new { name = "Administrator item updated", status = "Complete" });
        Assert.Equal(HttpStatusCode.OK, (await client.PutAsync($"/api/v1/work-items/{created!.Id}", updateContent)).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await client.DeleteAsync($"/api/v1/work-items/{created.Id}")).StatusCode);
    }

    [Fact]
    public async Task Invalid_work_item_request_returns_bad_request()
    {
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", factory.CreateToken("Operator"));
        using var content = JsonContent.Create(new { name = "   ", status = "Paused" });

        using var response = await client.PostAsync("/api/v1/work-items", content);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task Missing_work_item_returns_not_found()
    {
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", factory.CreateToken("Reader"));

        using var response = await client.GetAsync($"/api/v1/work-items/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}

public sealed class WorkItemWebApplicationFactory : WebApplicationFactory<Program>
{
    private const string Issuer = "https://DC01.lab.local";
    private const string Audience = "LabAuthServer.API";
    private readonly RSA signingKey = RSA.Create(2048);
    private readonly FakeWorkItemStore store = new();
    private readonly FakeOperationalStatusStore statusStore = new();

    public string CreateToken(string role)
    {
        var now = DateTime.UtcNow;
        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = Issuer,
            Audience = Audience,
            Subject = new ClaimsIdentity([
                new Claim("sub", "test-user"),
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
            configBuilder.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["JwtValidation:Issuer"] = Issuer,
                ["JwtValidation:Audience"] = Audience,
                ["JwtValidation:SigningAlgorithm"] = SecurityAlgorithms.RsaSha256,
                ["JwtValidation:ClockSkewSeconds"] = "300",
                ["JwtValidation:PublicKeyPem"] = PemEncoding.WriteString("PUBLIC KEY", signingKey.ExportSubjectPublicKeyInfo())
            });
        });
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<IWorkItemStore>();
            services.AddSingleton<IWorkItemStore>(store);
            services.RemoveAll<IOperationalStatusStore>();
            services.AddSingleton<IOperationalStatusStore>(statusStore);
        });
    }
}

public sealed class FakeWorkItemStore : IWorkItemStore
{
    private readonly Dictionary<Guid, WorkItemRow> items = new();

    public Task<IReadOnlyList<WorkItemRow>> ListAsync(CancellationToken cancellationToken)
        => Task.FromResult<IReadOnlyList<WorkItemRow>>(items.Values.OrderByDescending(item => item.UpdatedAtUtc).ToArray());

    public Task<WorkItemRow?> GetAsync(Guid id, CancellationToken cancellationToken)
        => Task.FromResult(items.TryGetValue(id, out var item) ? item : null);

    public Task<WorkItemRow> CreateAsync(WorkItemRow item, CancellationToken cancellationToken)
    {
        items[item.Id] = item;
        return Task.FromResult(item);
    }

    public Task<bool> UpdateAsync(WorkItemRow item, CancellationToken cancellationToken)
    {
        if (!items.ContainsKey(item.Id))
        {
            return Task.FromResult(false);
        }

        items[item.Id] = item;
        return Task.FromResult(true);
    }

    public Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken)
        => Task.FromResult(items.Remove(id));
}

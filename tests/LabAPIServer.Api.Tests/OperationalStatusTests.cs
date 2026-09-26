using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using LabAPIServer.Api.OperationalStatuses;

namespace LabAPIServer.Api.Tests;

public sealed class OperationalStatusUnitTests
{
    [Fact]
    public void Validator_rejects_invalid_severity_and_row_version()
    {
        var errors = OperationalStatusRequestValidator.Validate(new UpdateOperationalStatusRequest
        {
            Status = OperationalStatusLifecycle.InProgress,
            Value = "Ready",
            Severity = "Unknown",
            RowVersion = "bad"
        });

        Assert.Contains("Severity", errors.Keys);
        Assert.Contains("RowVersion", errors.Keys);
    }

    [Fact]
    public async Task Service_update_changes_value_and_identity()
    {
        var store = new FakeOperationalStatusStore();
        var service = new OperationalStatusService(store, TimeProvider.System);
        var current = await store.GetAsync("api", CancellationToken.None);

        var result = await service.UpdateAsync("api", new UpdateOperationalStatusRequest
        {
            Status = OperationalStatusLifecycle.InProgress,
            Value = "Ready",
            Severity = OperationalStatusSeverities.Info,
            RowVersion = Convert.ToBase64String(current!.RowVersion)
        }, "operator@lab.local", CancellationToken.None);

        Assert.NotNull(result.Status);
        Assert.False(result.Conflict);
        Assert.Equal("Ready", result.Status!.Value);
        Assert.Equal("operator@lab.local", result.Status.UpdatedBy);
    }

    [Fact]
    public async Task Service_rejects_direct_completion_without_mutation()
    {
        var store = new FakeOperationalStatusStore();
        var service = new OperationalStatusService(store, TimeProvider.System);
        var current = await store.GetAsync("api", CancellationToken.None);

        var result = await service.UpdateAsync("api", new UpdateOperationalStatusRequest
        {
            Status = OperationalStatusLifecycle.Complete,
            Value = "Complete",
            Severity = OperationalStatusSeverities.Info,
            RowVersion = Convert.ToBase64String(current!.RowVersion)
        }, "operator@lab.local", CancellationToken.None);

        Assert.True(result.InvalidTransition);
        Assert.Null(result.Status);
        Assert.Empty(await store.GetHistoryAsync("api", CancellationToken.None));
        Assert.Equal(OperationalStatusLifecycle.Open, (await store.GetAsync("api", CancellationToken.None))!.Status);
    }
}

public sealed class OperationalStatusApiTests : IClassFixture<WorkItemWebApplicationFactory>
{
    private readonly WorkItemWebApplicationFactory factory;

    public OperationalStatusApiTests(WorkItemWebApplicationFactory factory)
    {
        this.factory = factory;
    }

    [Fact]
    public async Task Operational_statuses_require_authentication()
    {
        using var client = factory.CreateClient();

        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/v1/operational-statuses")).StatusCode);
    }

    [Fact]
    public async Task Reader_can_read_but_cannot_update_operational_status()
    {
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", factory.CreateToken("Reader"));

        var listResponse = await client.GetAsync("/api/v1/operational-statuses");
        Assert.Equal(HttpStatusCode.OK, listResponse.StatusCode);
        var statuses = await listResponse.Content.ReadFromJsonAsync<IReadOnlyList<OperationalStatusDto>>();
        var status = Assert.Single(statuses!);

        using var content = JsonContent.Create(new
        {
            status = "InProgress",
            value = "Denied",
            severity = "Warning",
            rowVersion = status.RowVersion
        });
        Assert.Equal(HttpStatusCode.Forbidden, (await client.PutAsync("/api/v1/operational-statuses/api", content)).StatusCode);
    }

    [Fact]
    public async Task Administrator_can_update_operational_status()
    {
        using var isolatedFactory = new WorkItemWebApplicationFactory();
        using var client = isolatedFactory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", isolatedFactory.CreateToken("Administrator"));

        var status = await (await client.GetAsync("/api/v1/operational-statuses/api")).Content.ReadFromJsonAsync<OperationalStatusDto>();
        Assert.NotNull(status);
        using var content = JsonContent.Create(new
        {
            status = OperationalStatusLifecycle.InProgress,
            value = "Administrator update",
            severity = status!.Severity,
            rowVersion = status.RowVersion
        });

        var response = await client.PutAsync("/api/v1/operational-statuses/api", content);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Operator_can_update_and_stale_row_version_conflicts()
    {
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", factory.CreateToken("Operator"));

        var status = await (await client.GetAsync("/api/v1/operational-statuses/api")).Content.ReadFromJsonAsync<OperationalStatusDto>();
        Assert.NotNull(status);
        using var content = JsonContent.Create(new
        {
            status = "InProgress",
            value = "Updated",
            severity = "Warning",
            rowVersion = status!.RowVersion
        });
        var updateResponse = await client.PutAsync("/api/v1/operational-statuses/api", content);
        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);

        using var staleContent = JsonContent.Create(new
        {
            status = "Complete",
            value = "Stale",
            severity = "Critical",
            rowVersion = status.RowVersion
        });
        Assert.Equal(HttpStatusCode.Conflict, (await client.PutAsync("/api/v1/operational-statuses/api", staleContent)).StatusCode);
    }

    [Fact]
    public async Task Invalid_and_missing_operational_status_requests_are_rejected()
    {
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", factory.CreateToken("Operator"));
        using var invalidContent = JsonContent.Create(new { status = "Bad", value = " ", severity = "Bad", rowVersion = "bad" });

        Assert.Equal(HttpStatusCode.BadRequest, (await client.PutAsync("/api/v1/operational-statuses/api", invalidContent)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync("/api/v1/operational-statuses/missing")).StatusCode);
    }

    [Fact]
    public async Task Same_value_transition_is_rejected_without_history_change()
    {
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", factory.CreateToken("Operator"));
        var status = await (await client.GetAsync("/api/v1/operational-statuses/api")).Content.ReadFromJsonAsync<OperationalStatusDto>();
        Assert.NotNull(status);

        using var content = JsonContent.Create(new
        {
            status = status.Status,
            value = "Complete",
            severity = status!.Severity,
            rowVersion = status.RowVersion
        });
        Assert.Equal(HttpStatusCode.UnprocessableEntity, (await client.PutAsync("/api/v1/operational-statuses/api", content)).StatusCode);

        var current = await (await client.GetAsync("/api/v1/operational-statuses/api")).Content.ReadFromJsonAsync<OperationalStatusDto>();
        Assert.Equal(OperationalStatusLifecycle.Open, current!.Status);
        var history = await (await client.GetAsync("/api/v1/operational-statuses/api/history")).Content.ReadFromJsonAsync<OperationalStatusHistoryDto[]>();
        Assert.NotNull(history);
    }
}

public sealed class FakeOperationalStatusStore : IOperationalStatusStore
{
    private readonly List<OperationalStatusHistoryRow> history = [];

    private readonly Dictionary<string, OperationalStatusRow> statuses = new(StringComparer.Ordinal)
    {
        ["api"] = new(
            Guid.Parse("6e9f320b-9c85-47e9-a7ba-6f0fa1a3f8f9"),
            "api",
            OperationalStatusLifecycle.Open,
            "Healthy",
            OperationalStatusSeverities.Info,
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow,
            "seed",
            [0, 0, 0, 0, 0, 0, 0, 1])
    };

    public Task<IReadOnlyList<OperationalStatusRow>> ListAsync(CancellationToken cancellationToken)
        => Task.FromResult<IReadOnlyList<OperationalStatusRow>>(statuses.Values.OrderBy(item => item.Key).ToArray());

    public Task<OperationalStatusRow?> GetAsync(string key, CancellationToken cancellationToken)
        => Task.FromResult(statuses.TryGetValue(key, out var status) ? status : null);

    public Task<OperationalStatusUpdateResult> UpdateAsync(OperationalStatusRow status, byte[] expectedRowVersion, CancellationToken cancellationToken)
    {
        if (!statuses.TryGetValue(status.Key, out var current) || !current.RowVersion.SequenceEqual(expectedRowVersion))
        {
            return Task.FromResult(new OperationalStatusUpdateResult(OperationalStatusUpdateOutcome.Conflict, null));
        }

        var nextVersion = BitConverter.GetBytes(BitConverter.ToInt64(current.RowVersion, 0) + 1);
        var updated = status with { RowVersion = nextVersion };
        statuses[status.Key] = updated;
        history.Add(new(Guid.NewGuid(), current.Id, current.Status, updated.Status, updated.UpdatedBy, updated.UpdatedAtUtc));
        return Task.FromResult(new OperationalStatusUpdateResult(OperationalStatusUpdateOutcome.Updated, updated));
    }

    public Task<IReadOnlyList<OperationalStatusHistoryRow>> GetHistoryAsync(string key, CancellationToken cancellationToken)
        => Task.FromResult<IReadOnlyList<OperationalStatusHistoryRow>>(history
            .Where(entry => statuses.TryGetValue(key, out var status) && entry.StatusId == status.Id)
            .OrderBy(entry => entry.ChangedAtUtc)
            .ThenBy(entry => entry.Id)
            .ToArray());
}
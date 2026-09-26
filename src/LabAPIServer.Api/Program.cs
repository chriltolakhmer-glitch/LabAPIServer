using System.IdentityModel.Tokens.Jwt;
using System.Security.Cryptography;
using LabAPIServer.Api;
using LabAPIServer.Api.Auth;
using LabAPIServer.Api.Data;
using LabAPIServer.Api.OperationalStatuses;
using LabAPIServer.Api.WorkItems;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

var externalConfigurationPath = builder.Configuration["LABAPI_CONFIG_PATH"];
if (!string.IsNullOrWhiteSpace(externalConfigurationPath))
{
    builder.Configuration.AddJsonFile(externalConfigurationPath, optional: false, reloadOnChange: false);
}

builder.Services.AddOpenApi();
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

builder.Services
    .AddOptions<DatabaseOptions>()
    .Bind(builder.Configuration.GetSection(DatabaseOptions.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services.AddSingleton<IValidateOptions<DatabaseOptions>, DatabaseOptionsValidator>();
builder.Services.AddSingleton<ISqlConnectionFactory, SqlConnectionFactory>();
builder.Services.AddSingleton<IWorkItemStore, SqlWorkItemStore>();
builder.Services.AddSingleton<WorkItemService>();
builder.Services.AddSingleton<IOperationalStatusStore, SqlOperationalStatusStore>();
builder.Services.AddSingleton<OperationalStatusService>();

builder.Services
    .AddOptions<JwtValidationOptions>()
    .Bind(builder.Configuration.GetSection(JwtValidationOptions.SectionName))
    .ValidateOnStart();
builder.Services.AddSingleton<IValidateOptions<JwtValidationOptions>, JwtValidationOptionsValidator>();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        var jwtOptions = builder.Configuration.GetSection(JwtValidationOptions.SectionName).Get<JwtValidationOptions>() ?? new JwtValidationOptions();
        var publicKeyPem = JwtValidationOptions.LoadPublicKeyPem(jwtOptions);

        if (string.IsNullOrWhiteSpace(publicKeyPem))
        {
            throw new InvalidOperationException("JWT validation requires JwtValidation:PublicKeyPem or JwtValidation:PublicKeyPath.");
        }

        options.RequireHttpsMetadata = true;
        options.MapInboundClaims = false;
        options.SaveToken = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtOptions.Issuer,
            ValidateAudience = true,
            ValidAudience = jwtOptions.Audience,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(jwtOptions.ClockSkewSeconds),
            RequireExpirationTime = true,
            RequireSignedTokens = true,
            ValidateIssuerSigningKey = true,
            ValidateActor = false,
            NameClaimType = "sub",
            RoleClaimType = "role",
            ValidAlgorithms = [jwtOptions.SigningAlgorithm],
            IssuerSigningKey = new RsaSecurityKey(JwtValidationOptions.CreatePublicRsa(publicKeyPem))
        };

        options.Events = new JwtBearerEvents
        {
            OnAuthenticationFailed = context =>
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                context.NoResult();
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorizationBuilder()
    .AddPolicy("WorkItemsRead", policy => policy.RequireRole("Reader", "Operator", "Administrator"))
    .AddPolicy("WorkItemsWrite", policy => policy.RequireRole("Operator", "Administrator"))
    .AddPolicy("OperationalStatusesRead", policy => policy.RequireRole("Reader", "Operator", "Administrator"))
    .AddPolicy("OperationalStatusesWrite", policy => policy.RequireRole("Operator", "Administrator"));

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.UseExceptionHandler();

app.MapGet("/api/v1/health", () => Results.Ok(new { status = "Healthy" }));

app.MapGet("/api/v1/session", [Authorize] (HttpContext httpContext) =>
{
    var subject = httpContext.User.FindFirst("sub")?.Value ?? httpContext.User.Identity?.Name;
    var role = httpContext.User.FindFirst("role")?.Value;
    var expiresAtClaim = httpContext.User.FindFirst(JwtRegisteredClaimNames.Exp)?.Value;

    if (string.IsNullOrWhiteSpace(subject) || string.IsNullOrWhiteSpace(role) || string.IsNullOrWhiteSpace(expiresAtClaim))
    {
        return Results.Unauthorized();
    }

    var expiresAt = DateTimeOffset.FromUnixTimeSeconds(long.Parse(expiresAtClaim, System.Globalization.CultureInfo.InvariantCulture));

    return Results.Ok(new
    {
        subject,
        role,
        expiresAt = expiresAt.UtcDateTime.ToString("O")
    });
});

var workItems = app.MapGroup("/api/v1/work-items");
workItems.MapGet("", async (WorkItemService service, CancellationToken cancellationToken) =>
    Results.Ok(await service.ListAsync(cancellationToken)))
    .RequireAuthorization("WorkItemsRead");

workItems.MapGet("/{id:guid}", async (Guid id, WorkItemService service, CancellationToken cancellationToken) =>
{
    var item = await service.GetAsync(id, cancellationToken);
    return item is null ? Results.NotFound() : Results.Ok(item);
}).RequireAuthorization("WorkItemsRead");

workItems.MapPost("", async (CreateWorkItemRequest request, HttpContext httpContext, WorkItemService service, CancellationToken cancellationToken) =>
{
    var errors = WorkItemRequestValidator.Validate(request);
    if (errors.Count > 0)
    {
        return Results.ValidationProblem(errors);
    }

    var subject = httpContext.User.FindFirst("sub")?.Value;
    if (string.IsNullOrWhiteSpace(subject))
    {
        return Results.Unauthorized();
    }

    var item = await service.CreateAsync(request, subject, cancellationToken);
    return Results.Created($"/api/v1/work-items/{item.Id}", item);
}).RequireAuthorization("WorkItemsWrite");

workItems.MapPut("/{id:guid}", async (Guid id, UpdateWorkItemRequest request, HttpContext httpContext, WorkItemService service, CancellationToken cancellationToken) =>
{
    var errors = WorkItemRequestValidator.Validate(request);
    if (errors.Count > 0)
    {
        return Results.ValidationProblem(errors);
    }

    var subject = httpContext.User.FindFirst("sub")?.Value;
    if (string.IsNullOrWhiteSpace(subject))
    {
        return Results.Unauthorized();
    }

    var item = await service.UpdateAsync(id, request, subject, cancellationToken);
    return item is null ? Results.NotFound() : Results.Ok(item);
}).RequireAuthorization("WorkItemsWrite");

workItems.MapDelete("/{id:guid}", async (Guid id, WorkItemService service, CancellationToken cancellationToken) =>
    await service.DeleteAsync(id, cancellationToken) ? Results.NoContent() : Results.NotFound())
    .RequireAuthorization("WorkItemsWrite");

var operationalStatuses = app.MapGroup("/api/v1/operational-statuses");
operationalStatuses.MapGet("", async (OperationalStatusService service, CancellationToken cancellationToken) =>
    Results.Ok(await service.ListAsync(cancellationToken)))
    .RequireAuthorization("OperationalStatusesRead");

operationalStatuses.MapGet("/{key}/history", async (string key, OperationalStatusService service, CancellationToken cancellationToken) =>
{
    var result = await service.GetHistoryAsync(key, cancellationToken);
    return result.NotFound ? Results.NotFound() : Results.Ok(result.History);
}).RequireAuthorization("OperationalStatusesRead");

operationalStatuses.MapGet("/{key}", async (string key, OperationalStatusService service, CancellationToken cancellationToken) =>
{
    var status = await service.GetAsync(key, cancellationToken);
    return status is null ? Results.NotFound() : Results.Ok(status);
}).RequireAuthorization("OperationalStatusesRead");

operationalStatuses.MapPut("/{key}", async (string key, UpdateOperationalStatusRequest request, HttpContext httpContext, OperationalStatusService service, CancellationToken cancellationToken) =>
{
    var errors = OperationalStatusRequestValidator.Validate(request);
    if (errors.Count > 0)
    {
        return Results.ValidationProblem(errors);
    }

    var subject = httpContext.User.FindFirst("sub")?.Value;
    if (string.IsNullOrWhiteSpace(subject))
    {
        return Results.Unauthorized();
    }

    var result = await service.UpdateAsync(key, request, subject, cancellationToken);
    if (result.NotFound)
    {
        return Results.NotFound();
    }

    if (result.InvalidTransition)
    {
        return Results.Problem(
            statusCode: StatusCodes.Status422UnprocessableEntity,
            title: "Invalid operational status transition.");
    }

    return result.Conflict ? Results.Conflict() : Results.Ok(result.Status);
}).RequireAuthorization("OperationalStatusesWrite");

app.Run();

public partial class Program
{
}

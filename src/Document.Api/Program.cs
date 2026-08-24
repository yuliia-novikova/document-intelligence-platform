using Document.Api.HealthChecks;
using Document.Infrastructure.Persistence;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var documentDbConnectionString = builder.Configuration.GetConnectionString("DocumentDb")
    ?? throw new InvalidOperationException(
        "Missing ConnectionStrings:DocumentDb configuration. Set it via `dotnet user-secrets` for local " +
        "development, or the ConnectionStrings__DocumentDb environment variable in other environments.");

builder.Services.AddDbContext<DocumentDbContext>(options => options.UseNpgsql(documentDbConnectionString));

builder.Services.AddHealthChecks()
    .AddDbContextCheck<DocumentDbContext>(
        name: "postgresql",
        failureStatus: HealthStatus.Unhealthy,
        tags: ["ready"]);

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

// Overall status: runs every registered check. Useful for dashboards/manual inspection.
app.MapHealthChecks("/health", new HealthCheckOptions
{
    ResponseWriter = HealthCheckResponseWriter.WriteJsonAsync
});

// Liveness: no checks run (empty predicate => always healthy if the process can respond at all).
// Orchestrators use this to decide whether to restart the container - it must never fail just
// because a downstream dependency like PostgreSQL is temporarily unavailable.
app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = _ => false,
    ResponseWriter = HealthCheckResponseWriter.WriteJsonAsync
});

// Readiness: runs checks tagged "ready" (currently just PostgreSQL). Orchestrators use this to
// decide whether to route traffic to this instance.
app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready"),
    ResponseWriter = HealthCheckResponseWriter.WriteJsonAsync
});

app.Run();

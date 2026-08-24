using System.Text.Json;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Document.Api.HealthChecks;

public static class HealthCheckResponseWriter
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public static Task WriteJsonAsync(HttpContext context, HealthReport report)
    {
        context.Response.ContentType = "application/json; charset=utf-8";

        var payload = new
        {
            status = report.Status.ToString(),
            totalDurationMs = report.TotalDuration.TotalMilliseconds,
            checks = report.Entries.Select(entry => new
            {
                name = entry.Key,
                status = entry.Value.Status.ToString(),
                description = entry.Value.Description,
                durationMs = entry.Value.Duration.TotalMilliseconds,
                tags = entry.Value.Tags
            })
        };

        // Deliberately omit entry.Value.Exception: /health is typically reachable without
        // authentication by infra probes, and exception details (e.g. connection info) must
        // not leak to unauthenticated callers.
        return context.Response.WriteAsync(JsonSerializer.Serialize(payload, JsonOptions));
    }
}

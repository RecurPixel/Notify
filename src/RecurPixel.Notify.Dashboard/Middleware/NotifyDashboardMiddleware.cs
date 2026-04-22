using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace RecurPixel.Notify.Dashboard;

/// <summary>
/// ASP.NET Core middleware that serves the RecurPixel.Notify dashboard.
/// Intercepts all requests whose path starts with the configured
/// <see cref="DashboardOptions.RoutePrefix"/> and routes them to:
/// <list type="bullet">
///   <item><c>GET /&lt;prefix&gt;</c> — embedded HTML dashboard</item>
///   <item><c>GET /&lt;prefix&gt;/api/logs</c> — paged notification log (JSON)</item>
///   <item><c>GET /&lt;prefix&gt;/api/logs/batch/{batchId}</c> — single bulk batch (JSON)</item>
///   <item><c>GET /&lt;prefix&gt;/api/stats</c> — today's summary stats (JSON)</item>
/// </list>
/// All routes enforce <see cref="DashboardOptions.RequireRole"/> or
/// <see cref="DashboardOptions.PolicyName"/> when configured.
/// </summary>
public sealed class NotifyDashboardMiddleware
{
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNamingPolicy        = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition      = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented               = false
    };

    private readonly RequestDelegate _next;
    private readonly DashboardOptions _options;
    private readonly ILogger<NotifyDashboardMiddleware> _logger;
    private readonly string _prefix;   // e.g. "/notify-dashboard"
    private readonly string _apiBase;  // e.g. "/notify-dashboard/api"
    private readonly string _html;     // pre-built HTML with placeholders replaced

    /// <summary>
    /// Initialises the middleware. Reads and pre-processes the embedded HTML once.
    /// </summary>
    public NotifyDashboardMiddleware(
        RequestDelegate next,
        DashboardOptions options,
        ILogger<NotifyDashboardMiddleware> logger)
    {
        _next    = next;
        _options = options;
        _logger  = logger;
        _prefix  = "/" + options.RoutePrefix.Trim('/');
        _apiBase = _prefix + "/api";
        _html    = BuildHtml(options, _apiBase);
    }

    /// <inheritdoc />
    public async Task InvokeAsync(HttpContext context)
    {
        var path = context.Request.Path.Value ?? string.Empty;

        if (!path.Equals(_prefix, StringComparison.OrdinalIgnoreCase) &&
            !path.StartsWith(_prefix + "/", StringComparison.OrdinalIgnoreCase))
        {
            await _next(context);
            return;
        }

        if (!await IsAuthorizedAsync(context))
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsync("Unauthorized");
            return;
        }

        var relative = path.Substring(_prefix.Length).TrimStart('/');

        if (string.IsNullOrEmpty(relative) || relative.Equals("index.html", StringComparison.OrdinalIgnoreCase))
        {
            await ServeHtmlAsync(context);
            return;
        }

        if (relative.StartsWith("api/", StringComparison.OrdinalIgnoreCase))
        {
            await ServeApiAsync(context, relative.Substring(4));
            return;
        }

        context.Response.StatusCode = StatusCodes.Status404NotFound;
    }

    // ── Authorization ─────────────────────────────────────────────────────────

    private async Task<bool> IsAuthorizedAsync(HttpContext context)
    {
        if (_options.PolicyName is null && _options.RequireRole is null)
            return true;

        if (_options.PolicyName is not null)
        {
            var authService = context.RequestServices.GetService<IAuthorizationService>();
            if (authService is null)
            {
                _logger.LogWarning(
                    "Dashboard PolicyName '{Policy}' is configured but IAuthorizationService is not registered. " +
                    "Call builder.Services.AddAuthorization() in Program.cs.",
                    _options.PolicyName);
                return false;
            }

            var result = await authService.AuthorizeAsync(context.User, _options.PolicyName);
            return result.Succeeded;
        }

        // RequireRole check — user must be authenticated and in the required role
        return context.User.Identity?.IsAuthenticated == true &&
               context.User.IsInRole(_options.RequireRole!);
    }

    // ── HTML serving ──────────────────────────────────────────────────────────

    private async Task ServeHtmlAsync(HttpContext context)
    {
        context.Response.ContentType = "text/html; charset=utf-8";
        context.Response.StatusCode  = StatusCodes.Status200OK;
        await context.Response.WriteAsync(_html);
    }

    // ── API routing ───────────────────────────────────────────────────────────

    private async Task ServeApiAsync(HttpContext context, string apiPath)
    {
        var store = context.RequestServices.GetService<INotificationLogStore>();
        if (store is null)
        {
            context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
            await WriteJsonAsync(context, new { error = "INotificationLogStore is not registered. Call AddNotifyDashboardEfCore() or register your own INotificationLogStore." });
            return;
        }

        context.Response.StatusCode = StatusCodes.Status200OK;

        // GET /api/stats
        if (apiPath.Equals("stats", StringComparison.OrdinalIgnoreCase))
        {
            var stats = await store.GetTodayStatsAsync(context.RequestAborted);
            await WriteJsonAsync(context, stats);
            return;
        }

        // GET /api/logs
        if (apiPath.Equals("logs", StringComparison.OrdinalIgnoreCase))
        {
            var query = ParseQuery(context.Request.Query);
            var results = await store.QueryAsync(query, context.RequestAborted);
            await WriteJsonAsync(context, results);
            return;
        }

        // GET /api/logs/batch/{batchId}
        const string batchPrefix = "logs/batch/";
        if (apiPath.StartsWith(batchPrefix, StringComparison.OrdinalIgnoreCase))
        {
            var batchId = apiPath.Substring(batchPrefix.Length);
            if (string.IsNullOrEmpty(batchId))
            {
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                return;
            }
            var batch = await store.GetBatchAsync(batchId, context.RequestAborted);
            await WriteJsonAsync(context, batch);
            return;
        }

        context.Response.StatusCode = StatusCodes.Status404NotFound;
    }

    // ── Query parsing ─────────────────────────────────────────────────────────

    private static NotificationLogQuery ParseQuery(IQueryCollection q)
    {
        return new NotificationLogQuery
        {
            Channel   = NullIfEmpty(q["channel"]),
            Provider  = NullIfEmpty(q["provider"]),
            Success   = q.TryGetValue("status", out var status)
                ? status == "success" ? true : status == "failed" ? false : null
                : null,
            From      = q.TryGetValue("from", out var from) && DateTime.TryParse(from, out var fromDt)
                ? fromDt.ToUniversalTime()
                : null,
            To        = q.TryGetValue("to", out var to) && DateTime.TryParse(to, out var toDt)
                ? toDt.ToUniversalTime()
                : null,
            Recipient = NullIfEmpty(q["recipient"]),
            EventName = NullIfEmpty(q["eventName"]),
            IsBulk    = q.TryGetValue("isBulk", out var isBulk) && bool.TryParse(isBulk, out var bulkBool)
                ? bulkBool
                : null,
            Page      = q.TryGetValue("page",     out var page)     && int.TryParse(page,     out var pageNum) ? pageNum : 1,
            PageSize  = q.TryGetValue("pageSize",  out var pageSize) && int.TryParse(pageSize,  out var ps)    ? ps      : 50,
        };
    }

    private static string? NullIfEmpty(Microsoft.Extensions.Primitives.StringValues v)
        => string.IsNullOrEmpty(v) ? null : v.ToString();

    // ── JSON helper ───────────────────────────────────────────────────────────

    private static async Task WriteJsonAsync(HttpContext context, object data)
    {
        context.Response.ContentType = "application/json; charset=utf-8";
        await context.Response.WriteAsync(JsonSerializer.Serialize(data, JsonOpts));
    }

    // ── HTML builder ──────────────────────────────────────────────────────────

    private static string BuildHtml(DashboardOptions options, string apiBase)
    {
        var raw = ReadEmbeddedHtml();
        return raw
            .Replace("PAGE_TITLE", System.Net.WebUtility.HtmlEncode(options.PageTitle))
            .Replace("API_BASE",   apiBase)
            .Replace("PAGE_SIZE",  options.PageSize.ToString());
    }

    private static string? _cachedRawHtml;
    private static readonly object _htmlLock = new();

    internal static string ReadEmbeddedHtml()
    {
        if (_cachedRawHtml is not null) return _cachedRawHtml;
        lock (_htmlLock)
        {
            if (_cachedRawHtml is not null) return _cachedRawHtml;
            var assembly = typeof(NotifyDashboardMiddleware).Assembly;
            const string resourceName = "RecurPixel.Notify.Dashboard.Resources.dashboard.html";
            using var stream = assembly.GetManifestResourceStream(resourceName)
                ?? throw new InvalidOperationException(
                    $"Embedded resource '{resourceName}' not found. Ensure 'dashboard.html' is marked as EmbeddedResource in the csproj.");
            using var reader = new StreamReader(stream);
            _cachedRawHtml = reader.ReadToEnd();
        }
        return _cachedRawHtml;
    }
}

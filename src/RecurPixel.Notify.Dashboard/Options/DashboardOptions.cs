namespace RecurPixel.Notify;

/// <summary>
/// Configuration for the RecurPixel.Notify dashboard middleware and UI.
/// Pass an <see cref="Action{DashboardOptions}"/> to <c>AddNotifyDashboard()</c>.
/// </summary>
public sealed class DashboardOptions
{
    /// <summary>
    /// The URL path prefix at which the dashboard is served.
    /// Defaults to <c>"notify-dashboard"</c> — dashboard at <c>/notify-dashboard</c>.
    /// Must not include a leading or trailing slash.
    /// </summary>
    public string RoutePrefix { get; set; } = "notify-dashboard";

    /// <summary>Browser tab title for the dashboard page. Defaults to <c>"Notifications"</c>.</summary>
    public string PageTitle { get; set; } = "Notifications";

    /// <summary>
    /// Require this ASP.NET Core role to access the dashboard.
    /// Set to <c>null</c> to allow unauthenticated access (only safe in Development).
    /// Ignored when <see cref="PolicyName"/> is set — <see cref="PolicyName"/> takes precedence.
    /// </summary>
    public string? RequireRole { get; set; }

    /// <summary>
    /// Require this named ASP.NET Core authorization policy to access the dashboard.
    /// Takes precedence over <see cref="RequireRole"/> when both are set.
    /// Set to <c>null</c> to fall back to <see cref="RequireRole"/>.
    /// </summary>
    public string? PolicyName { get; set; }

    /// <summary>Number of log entries shown per page in the dashboard table. Defaults to 50.</summary>
    public int PageSize { get; set; } = 50;
}

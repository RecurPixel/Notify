using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RecurPixel.Notify.Dashboard;

namespace RecurPixel.Notify;

/// <summary>
/// Extension methods for wiring the dashboard into the ASP.NET Core request pipeline.
/// </summary>
public static class DashboardApplicationBuilderExtensions
{
    /// <summary>
    /// Adds the RecurPixel.Notify dashboard to the middleware pipeline.
    /// <para>
    /// The dashboard is served at the path configured by <see cref="DashboardOptions.RoutePrefix"/>
    /// (default: <c>/notify-dashboard</c>).
    /// </para>
    /// <para>
    /// Requires <c>AddNotifyDashboard()</c> to have been called during service registration.
    /// </para>
    /// <para>
    /// <strong>Security:</strong> Configure <see cref="DashboardOptions.RequireRole"/> or
    /// <see cref="DashboardOptions.PolicyName"/> to restrict access. If neither is set the
    /// dashboard is publicly accessible — a startup warning is logged in non-Development
    /// environments.
    /// </para>
    /// </summary>
    /// <param name="app">The application builder.</param>
    public static IApplicationBuilder UseNotifyDashboard(this IApplicationBuilder app)
    {
        var options = app.ApplicationServices.GetService<DashboardOptions>();
        if (options is null)
            throw new InvalidOperationException(
                "DashboardOptions is not registered. Call AddNotifyDashboard() before UseNotifyDashboard().");

        var logger = app.ApplicationServices.GetRequiredService<ILogger<NotifyDashboardMiddleware>>();
        var env    = app.ApplicationServices.GetService<IWebHostEnvironment>();

        WarnIfUnsecured(options, logger, env);

        return app.UseMiddleware<NotifyDashboardMiddleware>();
    }

    private static void WarnIfUnsecured(
        DashboardOptions options,
        ILogger logger,
        IWebHostEnvironment? env)
    {
        if (options.RequireRole is not null || options.PolicyName is not null)
            return;

        // If the environment cannot be determined, assume non-Development (safe default).
        var isDevelopment = env?.EnvironmentName.Equals("Development", StringComparison.OrdinalIgnoreCase) ?? false;
        if (isDevelopment)
            return;

        logger.LogWarning(
            "RecurPixel.Notify dashboard at /{Prefix} is running WITHOUT authentication. " +
            "Set DashboardOptions.RequireRole or DashboardOptions.PolicyName to secure it " +
            "before deploying to a non-Development environment.",
            options.RoutePrefix);
    }
}

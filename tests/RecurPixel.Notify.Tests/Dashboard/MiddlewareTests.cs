using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using RecurPixel.Notify.Dashboard;

namespace RecurPixel.Notify.Tests.Dashboard;

public class MiddlewareTests
{
    // ── Test server builder ───────────────────────────────────────────────────

    private static Task<IHost> BuildHostAsync(
        DashboardOptions? options = null,
        Mock<INotificationLogStore>? storeMock = null)
    {
        options ??= new DashboardOptions();
        storeMock ??= DefaultStore();

        return new HostBuilder()
            .ConfigureWebHost(web =>
            {
                web.UseTestServer()
                   .ConfigureServices(services =>
                   {
                       services.AddLogging();
                       services.AddRouting();
                       services.AddSingleton(options);
                       services.AddScoped<INotificationLogStore>(_ => storeMock.Object);
                       services.AddAuthorization();
                       services.AddAuthentication();
                   })
                   .UseEnvironment("Development")
                   .Configure(app => app.UseNotifyDashboard());
            })
            .StartAsync();
    }

    private static Mock<INotificationLogStore> DefaultStore()
    {
        var m = new Mock<INotificationLogStore>();
        m.Setup(s => s.GetTodayStatsAsync(It.IsAny<CancellationToken>()))
         .ReturnsAsync(new NotificationLogStats { TotalSent = 10, SuccessCount = 8, FailureCount = 2, ActiveChannelCount = 3 });
        m.Setup(s => s.QueryAsync(It.IsAny<NotificationLogQuery>(), It.IsAny<CancellationToken>()))
         .ReturnsAsync(new List<NotificationLog>
         {
             new() { Id=1, Channel="email", Provider="sendgrid", Recipient="a@b.com", Success=true, SentAt=DateTime.UtcNow }
         });
        m.Setup(s => s.GetBatchAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
         .ReturnsAsync(new List<NotificationLog>
         {
             new() { Id=1, Channel="email", Provider="sendgrid", Recipient="a@b.com", Success=true, BulkBatchId="batch-1", IsBulk=true, SentAt=DateTime.UtcNow },
             new() { Id=2, Channel="email", Provider="sendgrid", Recipient="c@d.com", Success=false, BulkBatchId="batch-1", IsBulk=true, Error="Bounced", SentAt=DateTime.UtcNow }
         });
        return m;
    }

    // ── HTML page ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task Get_DashboardRoot_ReturnsHtml()
    {
        using var host = await BuildHostAsync();
        var client = host.GetTestClient();

        var response = await client.GetAsync("/notify-dashboard");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("text/html; charset=utf-8", response.Content.Headers.ContentType?.ToString());
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("<!DOCTYPE html>", body);
    }

    [Fact]
    public async Task Get_DashboardRoot_ContainsConfiguredTitle()
    {
        using var host = await BuildHostAsync(new DashboardOptions { PageTitle = "My Alerts" });
        var response = await host.GetTestClient().GetAsync("/notify-dashboard");
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("My Alerts", body);
    }

    [Fact]
    public async Task Get_DashboardRoot_ContainsApiBase()
    {
        using var host = await BuildHostAsync(new DashboardOptions { RoutePrefix = "notify-dashboard" });
        var response = await host.GetTestClient().GetAsync("/notify-dashboard");
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("/notify-dashboard/api", body);
    }

    [Fact]
    public async Task Get_DashboardRoot_ContainsPageSize()
    {
        using var host = await BuildHostAsync(new DashboardOptions { PageSize = 75 });
        var response = await host.GetTestClient().GetAsync("/notify-dashboard");
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("75", body);
    }

    // ── Unrelated path passes through ─────────────────────────────────────────

    [Fact]
    public async Task Get_UnrelatedPath_PassesToNext_Returns404()
    {
        // The test server has no other middleware, so unhandled requests → 404
        using var host = await BuildHostAsync();
        var response = await host.GetTestClient().GetAsync("/some-other-path");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // ── API: stats ────────────────────────────────────────────────────────────

    [Fact]
    public async Task Get_ApiStats_ReturnsJson()
    {
        using var host = await BuildHostAsync();
        var response = await host.GetTestClient().GetAsync("/notify-dashboard/api/stats");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.StartsWith("application/json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task Get_ApiStats_ContainsRequiredFields()
    {
        using var host = await BuildHostAsync();
        var body = await host.GetTestClient().GetStringAsync("/notify-dashboard/api/stats");
        var doc = JsonDocument.Parse(body);

        Assert.True(doc.RootElement.TryGetProperty("totalSent",         out _));
        Assert.True(doc.RootElement.TryGetProperty("successCount",      out _));
        Assert.True(doc.RootElement.TryGetProperty("failureCount",      out _));
        Assert.True(doc.RootElement.TryGetProperty("successRate",       out _));
        Assert.True(doc.RootElement.TryGetProperty("activeChannelCount",out _));
    }

    [Fact]
    public async Task Get_ApiStats_ValuesMatchStore()
    {
        using var host = await BuildHostAsync();
        var body = await host.GetTestClient().GetStringAsync("/notify-dashboard/api/stats");
        var doc = JsonDocument.Parse(body);

        Assert.Equal(10, doc.RootElement.GetProperty("totalSent").GetInt32());
        Assert.Equal(8,  doc.RootElement.GetProperty("successCount").GetInt32());
        Assert.Equal(2,  doc.RootElement.GetProperty("failureCount").GetInt32());
        Assert.Equal(3,  doc.RootElement.GetProperty("activeChannelCount").GetInt32());
    }

    // ── API: logs ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task Get_ApiLogs_ReturnsJsonArray()
    {
        using var host = await BuildHostAsync();
        var response = await host.GetTestClient().GetAsync("/notify-dashboard/api/logs");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(body);
        Assert.Equal(JsonValueKind.Array, doc.RootElement.ValueKind);
    }

    [Fact]
    public async Task Get_ApiLogs_FirstItemContainsExpectedFields()
    {
        using var host = await BuildHostAsync();
        var body = await host.GetTestClient().GetStringAsync("/notify-dashboard/api/logs");
        var arr = JsonDocument.Parse(body).RootElement;

        Assert.True(arr.GetArrayLength() >= 1);
        var item = arr[0];
        Assert.True(item.TryGetProperty("channel",  out _));
        Assert.True(item.TryGetProperty("provider", out _));
        Assert.True(item.TryGetProperty("success",  out _));
        Assert.True(item.TryGetProperty("sentAt",   out _));
    }

    [Fact]
    public async Task Get_ApiLogs_QueryParamsPassed()
    {
        var storeMock = DefaultStore();
        storeMock.Setup(s => s.QueryAsync(
            It.Is<NotificationLogQuery>(q => q.Channel == "email" && q.Success == true),
            It.IsAny<CancellationToken>()))
         .ReturnsAsync(new List<NotificationLog>());

        using var host = await BuildHostAsync(storeMock: storeMock);
        var response = await host.GetTestClient()
            .GetAsync("/notify-dashboard/api/logs?channel=email&status=success");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        storeMock.Verify(
            s => s.QueryAsync(It.Is<NotificationLogQuery>(q => q.Channel == "email" && q.Success == true), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    // ── API: batch ────────────────────────────────────────────────────────────

    [Fact]
    public async Task Get_ApiBatch_ReturnsJsonArray()
    {
        using var host = await BuildHostAsync();
        var response = await host.GetTestClient().GetAsync("/notify-dashboard/api/logs/batch/batch-1");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var arr = JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement;
        Assert.Equal(JsonValueKind.Array, arr.ValueKind);
        Assert.Equal(2, arr.GetArrayLength());
    }

    [Fact]
    public async Task Get_ApiBatch_PassesBatchIdToStore()
    {
        var storeMock = DefaultStore();
        using var host = await BuildHostAsync(storeMock: storeMock);
        await host.GetTestClient().GetAsync("/notify-dashboard/api/logs/batch/my-batch-id");

        storeMock.Verify(
            s => s.GetBatchAsync("my-batch-id", It.IsAny<CancellationToken>()),
            Times.Once);
    }

    // ── Unknown API path ──────────────────────────────────────────────────────

    [Fact]
    public async Task Get_UnknownApiPath_Returns404()
    {
        using var host = await BuildHostAsync();
        var response = await host.GetTestClient().GetAsync("/notify-dashboard/api/unknown-endpoint");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // ── No store registered ───────────────────────────────────────────────────

    [Fact]
    public async Task Get_ApiStats_NoStore_Returns503()
    {
        using var host = await new HostBuilder()
            .ConfigureWebHost(web =>
            {
                web.UseTestServer()
                   .ConfigureServices(services =>
                   {
                       services.AddLogging();
                       services.AddRouting();
                       services.AddSingleton(new DashboardOptions());
                       // No INotificationLogStore registered
                   })
                   .UseEnvironment("Development")
                   .Configure(app => app.UseNotifyDashboard());
            })
            .StartAsync();

        var response = await host.GetTestClient().GetAsync("/notify-dashboard/api/stats");

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
    }

    // ── Authorization: RequireRole ────────────────────────────────────────────

    [Fact]
    public async Task Get_WithRequireRole_UnauthenticatedRequest_Returns401()
    {
        var options = new DashboardOptions { RequireRole = "Admin" };
        using var host = await BuildHostAsync(options);
        // Request with no authentication cookies/headers → unauthenticated
        var response = await host.GetTestClient().GetAsync("/notify-dashboard");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Get_WithRequireRole_AuthenticatedWithRole_Returns200()
    {
        var options = new DashboardOptions { RequireRole = "Admin" };

        using var host = await new HostBuilder()
            .ConfigureWebHost(web =>
            {
                web.UseTestServer()
                   .ConfigureServices(services =>
                   {
                       services.AddLogging();
                       services.AddRouting();
                       services.AddSingleton(options);
                       services.AddScoped<INotificationLogStore>(_ => DefaultStore().Object);
                       services.AddAuthorization();
                       services.AddAuthentication(TestAuthHandler.SchemeName)
                           .AddScheme<TestAuthOptions, TestAuthHandler>(TestAuthHandler.SchemeName, _ => { });
                   })
                   .UseEnvironment("Development")
                   .Configure(app =>
                   {
                       app.UseRouting();
                       app.UseAuthentication();
                       app.UseAuthorization();
                       app.UseNotifyDashboard();
                   });
            })
            .StartAsync();

        var client = host.GetTestClient();
        client.DefaultRequestHeaders.Add(TestAuthHandler.RoleHeader, "Admin");

        var response = await client.GetAsync("/notify-dashboard");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Get_WithRequireRole_AuthenticatedWrongRole_Returns401()
    {
        var options = new DashboardOptions { RequireRole = "Admin" };

        using var host = await new HostBuilder()
            .ConfigureWebHost(web =>
            {
                web.UseTestServer()
                   .ConfigureServices(services =>
                   {
                       services.AddLogging();
                       services.AddRouting();
                       services.AddSingleton(options);
                       services.AddScoped<INotificationLogStore>(_ => DefaultStore().Object);
                       services.AddAuthorization();
                       services.AddAuthentication(TestAuthHandler.SchemeName)
                           .AddScheme<TestAuthOptions, TestAuthHandler>(TestAuthHandler.SchemeName, _ => { });
                   })
                   .UseEnvironment("Development")
                   .Configure(app =>
                   {
                       app.UseRouting();
                       app.UseAuthentication();
                       app.UseAuthorization();
                       app.UseNotifyDashboard();
                   });
            })
            .StartAsync();

        var client = host.GetTestClient();
        client.DefaultRequestHeaders.Add(TestAuthHandler.RoleHeader, "Viewer"); // wrong role

        var response = await host.GetTestClient().GetAsync("/notify-dashboard");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // ── Custom route prefix ───────────────────────────────────────────────────

    [Fact]
    public async Task Get_CustomRoutePrefix_ServesAtConfiguredPath()
    {
        var options = new DashboardOptions { RoutePrefix = "admin/notifications" };
        using var host = await BuildHostAsync(options);

        var response = await host.GetTestClient().GetAsync("/admin/notifications");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("text/html; charset=utf-8", response.Content.Headers.ContentType?.ToString());
    }

    [Fact]
    public async Task Get_CustomRoutePrefix_DefaultPrefixNotServed()
    {
        var options = new DashboardOptions { RoutePrefix = "admin/notifications" };
        using var host = await BuildHostAsync(options);

        var response = await host.GetTestClient().GetAsync("/notify-dashboard");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // ── HTML resource loading ─────────────────────────────────────────────────

    [Fact]
    public void ReadEmbeddedHtml_ReturnsNonEmptyString()
    {
        var html = NotifyDashboardMiddleware.ReadEmbeddedHtml();
        Assert.NotEmpty(html);
        Assert.Contains("<!DOCTYPE html>", html);
    }

    [Fact]
    public void ReadEmbeddedHtml_ContainsPlaceholders()
    {
        var html = NotifyDashboardMiddleware.ReadEmbeddedHtml();
        Assert.Contains("PAGE_TITLE", html);
        Assert.Contains("API_BASE",   html);
        Assert.Contains("PAGE_SIZE",  html);
    }
}

---
name: pivot-scheduling-hangfire
description: Pivot.Framework.Infrastructure.Scheduling — Hangfire integration (SQL Server storage, server, dashboard with JWT-based authorization filter) and the IRecurringJobService<TIdentifier,TParams,TValue>/RecurrenceConfig abstraction. Use when adding background or recurring jobs, exposing the Hangfire dashboard, converting recurrence settings to cron, or changing Src/Infrastructure/Pivot.Framework.Infrastructure.Scheduling. For Keycloak browser login on the dashboard use pivot-auth-hangfire.
---

# Pivot.Framework.Infrastructure.Scheduling

Location: `Src/Infrastructure/Pivot.Framework.Infrastructure.Scheduling`. References Domain, Infrastructure.Abstraction, `Hangfire` 1.8 (metapackage: Core + SqlServer + AspNetCore).

## Registration

```csharp
// ConnectionStrings:HangfireConnection (default key) — SQL Server storage only
services.AddHangfireWithDashboard(configuration);                       // AddHangfire(UseSqlServerStorage) + AddHangfireServer
services.AddHangfireWithDashboard(configuration, "Jobs");               // other connection-string key
services.AddRecurringJobManager<string>();                              // IRecurringJobService<,,> → RecurringJobService<,,> (scoped, open generic)

app.UseAuthentication();
app.UseAuthorization();
app.UseHangfireDashboardWithOptions();                                  // /hangfire, requires authenticated user
app.UseHangfireDashboardWithOptions(o =>
{
	o.Authorization = [new HangfireDashboardAuthorizationFilter(requiredRole: "ops")];
	o.DarkModeEnabled = true;
});
```
- Despite its name `AddHangfireWithDashboard` does not map the dashboard; `UseHangfireDashboardWithOptions` does.
- Storage is hard-wired to SQL Server. For PostgreSQL call `services.AddHangfire(c => c.UsePostgreSqlStorage(…))` + `AddHangfireServer()` yourself (Hangfire.PostgreSql package) and still use the dashboard/job-service helpers.
- A missing connection string yields `null` → Hangfire throws at startup; validate config.
- `AddRecurringJobManager<TIdentifier>`'s type parameter is unused (it registers the open generic).

## Dashboard authorization

`HangfireDashboardAuthorizationFilter(string? requiredRole = null)`: denies unless `HttpContext.User` is authenticated
and (optionally) in the role (flattened `ClaimTypes.Role`, as produced by `pivot-auth-aspnetcore`).
With JWT bearer only, a browser won't send a token to `/hangfire` → 401. For interactive browser access use
`AddHangfireKeycloakBrowserAuth` + `UseHangfireDashboardWithKeycloakAuth` (`pivot-auth-hangfire`) instead of this method — don't call both (two dashboards on `/hangfire`).

## IRecurringJobService<TIdentifier, TParams, TValue>

```csharp
Result CreateJob(TIdentifier id, RecurrenceConfig config, Func<Task<Result<TValue>>> job);
Result CreateJobWithParams(TIdentifier id, RecurrenceConfig config, TParams p, Func<TParams, Task<Result<TValue>>> job);
Result ModifyJobWithParams(...);    // same as CreateJobWithParams (AddOrUpdate)
Result DeleteJob(TIdentifier id);   // RecurringJob.RemoveIfExists
Result RunJobNow(TIdentifier id);   // enqueues a job that calls RecurringJob.TriggerJob(id)
```
- Job id = `identifier.ToString()`.
- `RecurrenceConfig { Type = Hourly|Daily|Weekly|Monthly|Yearly, Interval }` → cron via `ToCronExpression()` (Hourly `0 */N * * *`, Daily `0 0 */N * *` — midnight UTC, Weekly Sunday 00:00 only with N=1, Monthly `0 0 1 */N *`, Yearly Jan 1 with N=1; otherwise `NotSupportedException`, which the service catches and returns as a failure).
- ⚠ The implementation passes captured delegates (`() => jobFunction()`) to `RecurringJob.AddOrUpdate`. Hangfire serializes the *method call expression*, so the delegate target (a closure) must be re-creatable by Hangfire's job activator on the server — which generally fails for lambdas/closures. **Prefer the idiomatic Hangfire form for real jobs**:
  ```csharp
  public sealed class DailyReportJob(ISender sender)
  {
  	public Task RunAsync(CancellationToken ct) => sender.Send(new GenerateDailyReportCommand(), ct);
  }
  services.AddScoped<DailyReportJob>();
  // at startup (after app.Build()):
  var jobs = app.Services.GetRequiredService<IRecurringJobManager>();
  jobs.AddOrUpdate<DailyReportJob>("daily-report", j => j.RunAsync(CancellationToken.None),
  	new RecurrenceConfig { Type = RecurrenceType.Daily, Interval = 1 }.ToCronExpression());
  ```
  (Hangfire resolves `DailyReportJob` from DI via its ASP.NET Core activator.) If you fix `RecurringJobService`, switch it to `IRecurringJobManager` + `AddOrUpdate<TJob>(expression)` and add tests.
- Error codes returned by the service are human sentences (`"Failed to create job."`) rather than dotted codes — normalise if you touch it.

## Jobs that touch the write side
Background jobs have no `HttpContext`: `HttpContextCurrentUserProvider` returns `"System"` for audit fields, and
`TransactionMiddleware` does not apply — open transactions via `ITransactionManager<T>` if needed, and create a scope per
execution (Hangfire does this for DI-activated jobs). Good candidates: outbox cleanup (delete processed rows), stuck-saga
recovery (`pivot-event-store-sagas`), projection rebuilds.

## Changing this package
- `AssemblyReference.cs` here wrongly uses namespace `Pivot.Framework.Infrastructure.Persistence.EntityFrameworkCore` — fix to `Pivot.Framework.Infrastructure.Scheduling` (breaking only for code that referenced it through the wrong namespace).
- `Pivot.Framework.Authentication.Hangfire` depends on this package; keep `DashboardOptions`-based extension shapes compatible.
- No test project exists; `Tests/Pivot.Framework.Infrastructure.Abstraction.Tests/Scheduling` covers `RecurrenceConfig`.

using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;

namespace RaidManager.ApiService.Tests.Support;

/// <summary>Sets the client address of a test request from the <see cref="HeaderName"/> header.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-04<br/>
/// Purpose: The test server has no client address, so per-address rate limits would put every test in one partition;
/// this gives each test its own address, as the forwarded headers do behind the shared Caddy.
/// </remarks>
public sealed class ClientAddressStartupFilter : IStartupFilter
{
    #region Constants
    /// <summary>Defines the header carrying the address a test request comes from.</summary>
    public const string HeaderName = "X-Test-Client-Address";
    #endregion Constants

    #region Public Methods
    /// <inheritdoc />
    public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next) => app =>
    {
        app.Use(async (context, nextMiddleware) =>
        {
            if (context.Request.Headers.TryGetValue(HeaderName, out var address) && IPAddress.TryParse(address, out var parsed))
            {
                context.Connection.RemoteIpAddress = parsed;
            }

            await nextMiddleware(context);
        });
        next(app);
    };
    #endregion Public Methods
}

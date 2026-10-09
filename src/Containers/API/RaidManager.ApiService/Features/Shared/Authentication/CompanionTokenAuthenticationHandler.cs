using System.Globalization;
using System.Security.Claims;
using System.Text.Encodings.Web;
using MediatR;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.Net.Http.Headers;
using Pivot.Framework.Domain.Shared;
using RaidManager.Application.Features.Companions.Commands.AuthenticateCompanion;
using RaidManager.Domain.Features.Companions.Errors;

namespace RaidManager.ApiService.Features.Shared.Authentication;

/// <summary>Authenticates a companion by the device token in its <c>Authorization: Bearer</c> header.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-04<br/>
/// Purpose: Checks the token on every request, so revocation applies at once, and answers 401 with the reason the companion shows (ADR-0030, mockup state 8). The token is never logged.
/// </remarks>
internal sealed class CompanionTokenAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    #region Constants
    /// <summary>Defines the key under which the refusal reason waits for the challenge.</summary>
    private const string FailureItemKey = "RaidManager.CompanionTokenFailure";
    #endregion Constants

    #region Constructors
    /// <summary>Initializes a new instance of the <see cref="CompanionTokenAuthenticationHandler"/> class.</summary>
    /// <param name="options">The scheme options.</param>
    /// <param name="logger">The logger factory.</param>
    /// <param name="encoder">The URL encoder.</param>
    public CompanionTokenAuthenticationHandler(IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder)
        : base(options, logger, encoder)
    {
    }
    #endregion Constructors

    #region Overrides
    /// <inheritdoc />
    /// <exception cref="InvalidOperationException">Thrown when the check fails for a reason other than the token, such as the database.</exception>
    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        string? header = Request.Headers.Authorization;
        if (header is null || !header.StartsWith(CompanionTokenDefaults.BearerPrefix, StringComparison.OrdinalIgnoreCase))
        {
            return AuthenticateResult.NoResult();
        }

        var sender = Context.RequestServices.GetRequiredService<ISender>();
        var result = await sender.Send(new AuthenticateCompanionCommand(header[CompanionTokenDefaults.BearerPrefix.Length..].Trim()), Context.RequestAborted);
        if (result.IsFailure)
        {
            // Only a refused token is the companion's problem; anything else must not make it forget its token.
            if (result.ResultExceptionType != ResultExceptionType.AuthenticationRequired)
            {
                throw new InvalidOperationException($"The companion token check failed: {result.Error.Code}.");
            }

            Context.Items[FailureItemKey] = result.Error;
            return AuthenticateResult.Fail(result.Error.Code);
        }

        var companion = result.Value;
        List<Claim> claims =
        [
            new Claim(CompanionTokenDefaults.CompanionClaim, companion.CompanionId.ToString()),
            new Claim(CompanionTokenDefaults.UserClaim, companion.UserId.ToString()),
            new Claim(CompanionTokenDefaults.LabelClaim, companion.Label),
        ];
        if (companion.SyncAgainRequestedAtUtc is { } requested)
        {
            claims.Add(new Claim(CompanionTokenDefaults.SyncAgainClaim, requested.ToString("O", CultureInfo.InvariantCulture)));
        }

        var identity = new ClaimsIdentity(claims, Scheme.Name);
        return AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(identity), Scheme.Name));
    }

    /// <inheritdoc />
    protected override async Task HandleChallengeAsync(AuthenticationProperties properties)
    {
        var error = Context.Items[FailureItemKey] as Error ?? CompanionErrors.TokenUnknown;
        Response.StatusCode = StatusCodes.Status401Unauthorized;
        Response.Headers[HeaderNames.WWWAuthenticate] = "Bearer error=\"invalid_token\"";
        await Response.WriteAsJsonAsync(
            new ProblemDetails { Status = StatusCodes.Status401Unauthorized, Title = error.Code, Detail = error.Message },
            options: null,
            contentType: "application/problem+json",
            cancellationToken: Context.RequestAborted);
    }
    #endregion Overrides
}

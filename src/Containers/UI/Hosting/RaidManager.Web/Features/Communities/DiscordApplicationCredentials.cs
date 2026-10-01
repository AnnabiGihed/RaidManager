namespace RaidManager.Web.Features.Communities;

/// <summary>Holds the Discord application's OAuth2 credentials, which the website already uses for sign-in.</summary>
/// <param name="ClientId">The application's client id.</param>
/// <param name="ClientSecret">The application's client secret.</param>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-01<br/>
/// Purpose: Lets the install flow use the same credentials as sign-in without reading configuration again.
/// </remarks>
public sealed record DiscordApplicationCredentials(string ClientId, string ClientSecret);

using MediatR;
using Microsoft.AspNetCore.Mvc;
using RaidManager.ApiService.Features.Shared.Authentication;
using RaidManager.ApiService.Features.Shared.Http;
using RaidManager.Application.Features.Communities.Commands.LinkCommunity;
using RaidManager.Application.Features.Communities.Commands.MapCommunityRole;
using RaidManager.Application.Features.Communities.Commands.RefreshCommunityName;
using RaidManager.Application.Features.Communities.Commands.UnmapCommunityRole;
using RaidManager.Application.Features.Communities.Queries.GetCommunity;
using RaidManager.Application.Features.Communities.Queries.GetCommunityByDiscordServer;
using RaidManager.Application.Features.Communities.Queries.GetCommunityMembers;
using RaidManager.Application.Features.Communities.Queries.GetCommunityRoleSettings;
using RaidManager.Application.Features.Communities.Queries.GetUserCommunities;
using RaidManager.Application.Features.Communities.Queries.ListCommunitiesByDiscordServers;

namespace RaidManager.ApiService.Features.Communities;

/// <summary>Maps the website-only endpoints that link and read communities.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-01<br/>
/// Purpose: Lets the website finish adding RaidManager to a Discord server and show the linked community (story #14).
/// </remarks>
public static class CommunityEndpoints
{
    #region Constants
    /// <summary>Defines the route of the communities collection.</summary>
    public const string CommunitiesRoute = "/internal/communities";

    /// <summary>Defines the route of a user's communities.</summary>
    public const string UserCommunitiesRoute = "/internal/users/{userId:guid}/communities";

    /// <summary>Defines the route of a user's view of one community.</summary>
    public const string UserCommunityRoute = "/internal/users/{userId:guid}/communities/{communityId:guid}";

    /// <summary>Defines the OpenAPI tag of these endpoints.</summary>
    private const string Tag = "Communities";
    #endregion Constants

    #region Public Methods
    /// <summary>Maps the community endpoints, each requiring the website's service key.</summary>
    /// <param name="endpoints">The endpoint route builder.</param>
    /// <returns>The same endpoint route builder.</returns>
    public static IEndpointRouteBuilder MapCommunityEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var communities = endpoints.MapGroup(CommunitiesRoute)
            .RequireAuthorization(WebsiteServiceDefaults.Policy)
            .WithTags(Tag)
            .ProducesProblem(StatusCodes.Status401Unauthorized);

        communities.MapPost("/", LinkAsync)
            .WithName("LinkCommunity")
            .WithSummary("Link a Discord server as a community")
            .WithDescription("Called by the website once the bot was added to a server and the Administrator chose the realm. The signed-in user becomes the Administrator. A server that is already linked returns 409 and nothing changes.")
            .Produces<LinkCommunityResponse>(StatusCodes.Status201Created)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        communities.MapGet("/{communityId:guid}", GetAsync)
            .WithName("GetCommunity")
            .WithSummary("Get a community")
            .WithDescription("Returns the community's Discord server, realm and Administrator.")
            .Produces<CommunitySummary>()
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound);

        communities.MapGet("/by-discord-server/{discordGuildId}", GetByDiscordServerAsync)
            .WithName("GetCommunityByDiscordServer")
            .WithSummary("Get the community a Discord server links to")
            .WithDescription("Tells the website, right after the bot was added to a server, whether that server is already linked. Returns 404 when it isn't.")
            .Produces<CommunitySummary>()
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound);

        communities.MapPost("/by-discord-servers", ListByDiscordServersAsync)
            .WithName("ListCommunitiesByDiscordServers")
            .WithSummary("List the communities a user's Discord servers link to")
            .WithDescription("Called by the website at sign-in with the servers Discord lists for the user. Returns the linked ones, by name; servers that aren't linked are left out.")
            .Produces<IReadOnlyList<CommunitySummary>>()
            .ProducesValidationProblem();

        endpoints.MapGet(UserCommunitiesRoute, GetUserCommunitiesAsync)
            .RequireAuthorization(WebsiteServiceDefaults.Policy)
            .WithTags(Tag)
            .WithName("GetUserCommunities")
            .WithSummary("List a user's communities")
            .WithDescription("Returns the communities the user administers, then those listed in memberOf, the communities the user's Discord servers matched at sign-in; each group by name. An empty list means the user has no community yet.")
            .Produces<IReadOnlyList<CommunitySummary>>()
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status401Unauthorized);

        var userCommunity = endpoints.MapGroup(UserCommunityRoute)
            .RequireAuthorization(WebsiteServiceDefaults.Policy)
            .WithTags(Tag)
            .ProducesProblem(StatusCodes.Status401Unauthorized);

        userCommunity.MapGet("/roles", GetRoleSettingsAsync)
            .WithName("GetCommunityRoleSettings")
            .WithSummary("Get a community's roles")
            .WithDescription("Reads the Discord server's roles and members with the bot and applies the community's mappings: the mappable roles, and rows for the Administrator, each of the community's roles with what it allows, and Member, each with its Discord roles and member count. Only a current member of the server may ask; only the Administrator can edit. Also refreshes the stored server name. Returns 503 when Discord can't answer.")
            .Produces<CommunityRoleSettings>()
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);

        userCommunity.MapGet("/members", GetMembersAsync)
            .WithName("GetCommunityMembers")
            .WithSummary("List the people in a community's Discord server")
            .WithDescription("Reads the Discord server's people with the bot, without bots, and gives each the roles they have: Administrator for the Administrator, otherwise every role their Discord roles give, otherwise Member. Only a current member of the server may ask. Also refreshes the stored server name. Returns 503 when Discord can't answer.")
            .Produces<CommunityMembers>()
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);

        userCommunity.MapPut("/role-mappings/{roleId:guid}/{discordRoleId}", MapRoleAsync)
            .WithName("MapCommunityRole")
            .WithSummary("Map a Discord role to one of the community's roles")
            .WithDescription("Only the community's Administrator, still in the server, may. The role must be one of the community's roles (404 otherwise), and the Discord role one of the server's mappable roles. One Discord role can give several roles; mapping it to one keeps the others.")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);

        userCommunity.MapDelete("/role-mappings/{roleId:guid}/{discordRoleId}", UnmapRoleAsync)
            .WithName("UnmapCommunityRole")
            .WithSummary("Stop a Discord role giving one of the community's roles")
            .WithDescription("Only the community's Administrator, still in the server, may. Any other role the Discord role gives stays; removing a mapping that doesn't exist changes nothing.")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);

        return endpoints;
    }
    #endregion Public Methods

    #region Private Helpers
    /// <summary>Links a Discord server as a community.</summary>
    /// <param name="request">The link request.</param>
    /// <param name="sender">The MediatR sender.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>201 with the community's identifier, or a problem.</returns>
    private static async Task<IResult> LinkAsync(LinkCommunityRequest request, ISender sender, CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new LinkCommunityCommand(request.DiscordGuildId, request.Name, request.Realm, request.AdministratorUserId),
            cancellationToken);
        return result.ToHttpResult(communityId =>
            TypedResults.Created($"{CommunitiesRoute}/{communityId}", new LinkCommunityResponse(communityId)));
    }

    /// <summary>Gets a community.</summary>
    /// <param name="communityId">The community.</param>
    /// <param name="sender">The MediatR sender.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>200 with the community, or a problem.</returns>
    private static async Task<IResult> GetAsync(Guid communityId, ISender sender, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetCommunityQuery(communityId), cancellationToken);
        return result.ToHttpResult(community => TypedResults.Ok(CommunitySummary.From(community)));
    }

    /// <summary>Gets the community a Discord server links to.</summary>
    /// <param name="discordGuildId">The Discord server snowflake.</param>
    /// <param name="sender">The MediatR sender.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>200 with the community, or a problem.</returns>
    private static async Task<IResult> GetByDiscordServerAsync(string discordGuildId, ISender sender, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetCommunityByDiscordServerQuery(discordGuildId), cancellationToken);
        return result.ToHttpResult(community => TypedResults.Ok(CommunitySummary.From(community)));
    }

    /// <summary>Gets a community's officer roles, refreshing the stored server name from Discord's answer.</summary>
    /// <param name="userId">The signed-in user.</param>
    /// <param name="communityId">The community.</param>
    /// <param name="sender">The MediatR sender.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>200 with the roles card, or a problem.</returns>
    private static async Task<IResult> GetRoleSettingsAsync(Guid userId, Guid communityId, ISender sender, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetCommunityRoleSettingsQuery(communityId, userId), cancellationToken);
        if (result.IsSuccess)
        {
            // Reading the server is when RaidManager learns its current name (owner decision on #14).
            await sender.Send(new RefreshCommunityNameCommand(communityId, result.Value.ServerName), cancellationToken);
        }

        return result.ToHttpResult(settings => TypedResults.Ok(CommunityRoleSettings.From(settings)));
    }

    /// <summary>Lists the people in a community's Discord server, refreshing the stored server name from Discord's answer.</summary>
    /// <param name="userId">The signed-in user.</param>
    /// <param name="communityId">The community.</param>
    /// <param name="sender">The MediatR sender.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>200 with the members, or a problem.</returns>
    private static async Task<IResult> GetMembersAsync(Guid userId, Guid communityId, ISender sender, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetCommunityMembersQuery(communityId, userId), cancellationToken);
        if (result.IsSuccess)
        {
            await sender.Send(new RefreshCommunityNameCommand(communityId, result.Value.ServerName), cancellationToken);
        }

        return result.ToHttpResult(members => TypedResults.Ok(CommunityMembers.From(members)));
    }

    /// <summary>Maps a Discord role to one of the community's roles.</summary>
    /// <param name="userId">The signed-in user.</param>
    /// <param name="communityId">The community.</param>
    /// <param name="roleId">The community role.</param>
    /// <param name="discordRoleId">The Discord role snowflake.</param>
    /// <param name="sender">The MediatR sender.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>204, or a problem.</returns>
    private static async Task<IResult> MapRoleAsync(
        Guid userId,
        Guid communityId,
        Guid roleId,
        string discordRoleId,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new MapCommunityRoleCommand(communityId, userId, discordRoleId, roleId), cancellationToken);
        return result.ToHttpResult(TypedResults.NoContent);
    }

    /// <summary>Stops a Discord role giving one of the community's roles.</summary>
    /// <param name="userId">The signed-in user.</param>
    /// <param name="communityId">The community.</param>
    /// <param name="roleId">The community role.</param>
    /// <param name="discordRoleId">The Discord role snowflake.</param>
    /// <param name="sender">The MediatR sender.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>204, or a problem.</returns>
    private static async Task<IResult> UnmapRoleAsync(Guid userId, Guid communityId, Guid roleId, string discordRoleId, ISender sender, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new UnmapCommunityRoleCommand(communityId, userId, discordRoleId, roleId), cancellationToken);
        return result.ToHttpResult(TypedResults.NoContent);
    }

    /// <summary>Lists the communities a user's Discord servers link to.</summary>
    /// <param name="request">The servers.</param>
    /// <param name="sender">The MediatR sender.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>200 with the communities, or a problem.</returns>
    private static async Task<IResult> ListByDiscordServersAsync(FindCommunitiesByDiscordServersRequest request, ISender sender, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new ListCommunitiesByDiscordServersQuery(request.DiscordGuildIds ?? []), cancellationToken);
        return result.ToHttpResult(communities => TypedResults.Ok(communities.Select(CommunitySummary.From).ToList()));
    }

    /// <summary>Lists a user's communities.</summary>
    /// <param name="userId">The user.</param>
    /// <param name="memberOf">The communities the user's Discord servers matched at sign-in.</param>
    /// <param name="sender">The MediatR sender.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>200 with the communities, or a problem.</returns>
    private static async Task<IResult> GetUserCommunitiesAsync(Guid userId, [FromQuery] Guid[]? memberOf, ISender sender, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetUserCommunitiesQuery(userId, memberOf ?? []), cancellationToken);
        return result.ToHttpResult(communities => TypedResults.Ok(communities.Select(CommunitySummary.From).ToList()));
    }
    #endregion Private Helpers
}

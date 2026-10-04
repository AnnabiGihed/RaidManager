namespace RaidManager.Companion.Client.Features.Pairing;

/// <summary>The part of an API problem the companion reads: its error code, in the title.</summary>
/// <param name="Title">The error code, such as <c>CompanionPairing.Pending</c>.</param>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-04<br/>
/// Purpose: Reads the error code the API puts in the problem title.
/// </remarks>
internal sealed record ProblemResponse(string? Title);

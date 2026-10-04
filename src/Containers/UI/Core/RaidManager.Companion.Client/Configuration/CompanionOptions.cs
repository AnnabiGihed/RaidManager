using System.ComponentModel.DataAnnotations;

namespace RaidManager.Companion.Client.Configuration;

/// <summary>Holds the addresses the companion talks to.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-04<br/>
/// Purpose: Binds the <c>Companion</c> section of the environment's settings file, so a publish selects its environment
/// and the player never types an address (ADR-0032).
/// </remarks>
public sealed class CompanionOptions
{
    #region Constants
    /// <summary>Defines the configuration section the options are bound from.</summary>
    public const string SectionName = "Companion";
    #endregion Constants

    #region Public Properties
    /// <summary>Gets or sets the public API host, which serves only the <c>/companion</c> routes.</summary>
    [Required]
    public Uri? ApiBaseUrl { get; set; }

    /// <summary>Gets or sets the website, where the player confirms a pairing code.</summary>
    [Required]
    public Uri? WebsiteBaseUrl { get; set; }
    #endregion Public Properties
}

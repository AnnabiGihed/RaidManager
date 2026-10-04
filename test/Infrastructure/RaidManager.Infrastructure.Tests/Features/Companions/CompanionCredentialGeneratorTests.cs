using Microsoft.Extensions.DependencyInjection;
using RaidManager.Application.Features.Companions.Abstractions;
using RaidManager.Domain.Features.Companions.ValueObjects;
using RaidManager.Infrastructure.Features.Companions;
using Shouldly;
using Xunit;

namespace RaidManager.Infrastructure.Tests.Features.Companions;

/// <summary>Verifies the cryptographic generator of companion secrets and pairing codes.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-04<br/>
/// Purpose: Proves the formats ADR-0030 decides: 32 random bytes as URL-safe Base64, and codes from the 31-symbol alphabet.
/// </remarks>
public sealed class CompanionCredentialGeneratorTests
{
    #region Constants
    /// <summary>Defines how many values each test draws.</summary>
    private const int Draws = 200;
    #endregion Constants

    #region Tests
    /// <summary>Draws secrets.</summary>
    [Fact]
    public void SecretsAre32RandomBytesInUrlSafeBase64()
    {
        var generator = new CompanionCredentialGenerator();

        var secrets = Enumerable.Range(0, Draws).Select(_ => generator.NewSecret()).ToList();

        secrets.ShouldAllBe(secret => secret.Length == 43 && secret.All(symbol => char.IsAsciiLetterOrDigit(symbol) || symbol == '-' || symbol == '_'));
        secrets.Distinct().Count().ShouldBe(Draws);
    }

    /// <summary>Draws pairing codes.</summary>
    [Fact]
    public void PairingCodesUseOnlyTheAlphabet()
    {
        var generator = new CompanionCredentialGenerator();

        var codes = Enumerable.Range(0, Draws).Select(_ => generator.NewPairingCode()).ToList();

        codes.ShouldAllBe(code => code.Value.Length == PairingCode.Length && code.Value.All(symbol => PairingCode.Alphabet.Contains(symbol)));
        codes.Select(code => code.Value).Distinct().Count().ShouldBeGreaterThan(Draws - 2);
    }

    /// <summary>Registers the generator.</summary>
    [Fact]
    public void RegistrationProvidesTheGenerator()
    {
        using var provider = new ServiceCollection().AddRaidManagerCompanions().BuildServiceProvider();

        provider.GetRequiredService<ICompanionCredentialGenerator>().ShouldBeOfType<CompanionCredentialGenerator>();
    }
    #endregion Tests
}

using Pivot.Framework.Domain.Exceptions;
using Reqnroll;
using Shouldly;
using RaidManager.Domain.Features.Companions.ValueObjects;

namespace RaidManager.Domain.Tests.Features.Companions.ValueObjects;

/// <summary>Defines business-readable steps for pairing codes and secret hashes.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-04<br/>
/// Purpose: Verifies the code format and the hashing of ADR-0030, which the website and the database rely on.
/// </remarks>
[Binding]
[Scope(Feature = "Companion credentials")]
public sealed class CompanionCredentialsStepDefinitions
{
    #region Fields
    /// <summary>Stores the code read, when the value was one.</summary>
    private PairingCode? _code;

    /// <summary>Stores whether the latest value was refused.</summary>
    private bool _refused;

    /// <summary>Stores the first hash.</summary>
    private CredentialHash _first = null!;

    /// <summary>Stores the second hash.</summary>
    private CredentialHash _second = null!;
    #endregion Fields

    #region When Steps
    /// <summary>Reads a typed code with both factories.</summary>
    /// <param name="typed">The typed value.</param>
    [When("the code {string} is read")]
    public void WhenTheCodeIsRead(string typed)
    {
        _refused = !PairingCode.TryCreate(typed, out _code);
        if (_refused)
        {
            Should.Throw<DomainException>(() => PairingCode.Create(typed));
        }
    }

    /// <summary>Hashes a secret twice.</summary>
    /// <param name="secret">The secret.</param>
    [When("the secret {string} is hashed twice")]
    public void WhenTheSecretIsHashedTwice(string secret)
    {
        _first = CredentialHash.Of(secret);
        _second = CredentialHash.Of(secret);
    }

    /// <summary>Restores a stored hash.</summary>
    /// <param name="stored">The stored value.</param>
    [When("the stored hash {string} is restored")]
    public void WhenTheStoredHashIsRestored(string stored)
    {
        try
        {
            CredentialHash.FromStored(stored);
        }
        catch (DomainException)
        {
            _refused = true;
        }
    }
    #endregion When Steps

    #region Then Steps
    /// <summary>Asserts how the code reads.</summary>
    /// <param name="shown">The expected code with its dash.</param>
    [Then("the code reads as {string}")]
    public void ThenTheCodeReadsAs(string shown)
    {
        _code.ShouldNotBeNull().ToString().ShouldBe(shown);
        PairingCode.Create(shown).ShouldBe(_code);
    }

    /// <summary>Asserts that the value isn't a code.</summary>
    [Then("the code is refused")]
    public void ThenTheCodeIsRefused() => _refused.ShouldBeTrue();

    /// <summary>Asserts that hashing is deterministic.</summary>
    [Then("both hashes are equal")]
    public void ThenBothHashesAreEqual()
    {
        _first.ShouldBe(_second);
        _first.GetHashCode().ShouldBe(_second.GetHashCode());
    }

    /// <summary>Asserts the stored form.</summary>
    /// <param name="length">The expected length.</param>
    [Then("the hash has {int} lower-case hexadecimal characters")]
    public void ThenTheHashHasLowerCaseHexadecimalCharacters(int length)
    {
        _first.Value.Length.ShouldBe(length);
        _first.ToString().ShouldBe(_first.Value.ToLowerInvariant());
    }

    /// <summary>Asserts that the stored form restores the same hash.</summary>
    [Then("the hash is restored from its stored form")]
    public void ThenTheHashIsRestoredFromItsStoredForm() => CredentialHash.FromStored(_first.Value).ShouldBe(_first);

    /// <summary>Asserts that the stored value was refused.</summary>
    [Then("the stored hash is refused")]
    public void ThenTheStoredHashIsRefused() => _refused.ShouldBeTrue();
    #endregion Then Steps
}

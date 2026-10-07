using RaidManager.Companion.Client.Features.Sync.SavedVariables;
using RaidManager.Companion.Client.Tests.Support;
using Reqnroll;
using Shouldly;

namespace RaidManager.Companion.Client.Tests.Features.Sync;

/// <summary>Defines business-readable steps for reading the addon's file.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-07<br/>
/// Purpose: Verifies #550's reader on the addon's fixtures: a complete file gives every character, a cut file keeps
/// the characters before the cut and names the cut one, and only schema 1 gives characters.
/// </remarks>
[Binding]
[Scope(Feature = "Companion snapshot reading")]
public sealed class CompanionSnapshotReadingStepDefinitions
{
    #region Fields
    /// <summary>Stores the file's text.</summary>
    private string _text = string.Empty;

    /// <summary>Stores what the companion read.</summary>
    private SavedVariablesFile? _read;
    #endregion Fields

    #region Private Properties
    /// <summary>Gets what the companion read.</summary>
    private SavedVariablesFile Read => _read.ShouldNotBeNull();
    #endregion Private Properties

    #region Given Steps
    /// <summary>Loads a fixture's file.</summary>
    /// <param name="fixture">The fixture.</param>
    /// <param name="account">The account folder.</param>
    [Given("the addon file of the {string} fixture for the account {string}")]
    public void GivenTheAddonFileOfTheFixtureForTheAccount(string fixture, string account) => _text = AddonFixtures.Read(fixture, account);

    /// <summary>Cuts the file a little after a character's key, as a crash during the write would.</summary>
    /// <param name="name">The character's name.</param>
    [Given("the file ends inside the character {string}")]
    public void GivenTheFileEndsInsideTheCharacter(string name)
    {
        var key = _text.IndexOf($"|{name}\"]", StringComparison.Ordinal);
        key.ShouldBeGreaterThan(0);
        _text = _text[..(key + 300)];
    }

    /// <summary>Keeps only the first lines of the file.</summary>
    /// <param name="lines">The lines kept.</param>
    [Given("the file ends after {int} lines")]
    public void GivenTheFileEndsAfterLines(int lines) => _text = string.Join('\n', _text.Split('\n').Take(lines));

    /// <summary>Removes the capture time of every character.</summary>
    [Given("the character entries lack their capture time")]
    public void GivenTheCharacterEntriesLackTheirCaptureTime() =>
        _text = string.Join('\n', _text.Split('\n').Where(line => !line.Contains("[\"capturedAt\"]", StringComparison.Ordinal)));

    /// <summary>Changes the file's schema version.</summary>
    /// <param name="version">The version.</param>
    [Given("the file's schema version is {int}")]
    public void GivenTheFilesSchemaVersionIs(int version) =>
        _text = _text.Replace("[\"schemaVersion\"] = 1,", $"[\"schemaVersion\"] = {version},", StringComparison.Ordinal);

    /// <summary>Uses a file with any content.</summary>
    /// <param name="content">The content.</param>
    [Given("a file that holds {string}")]
    public void GivenAFileThatHolds(string content) => _text = content;
    #endregion Given Steps

    #region When Steps
    /// <summary>Reads the file.</summary>
    [When("the companion reads the file")]
    public void WhenTheCompanionReadsTheFile() => _read = SavedVariablesReader.Read(_text);
    #endregion When Steps

    #region Then Steps
    /// <summary>Checks the file's status.</summary>
    /// <param name="status">The status.</param>
    [Then("the file reads as {string}")]
    public void ThenTheFileReadsAs(string status) => Read.Status.ShouldBe(Enum.Parse<SavedVariablesReadStatus>(status));

    /// <summary>Checks the complete characters, in the file's order.</summary>
    /// <param name="characters">The characters as <c>Name (Realm)</c>, comma-separated, or <c>none</c>.</param>
    [Then("the characters read are {string}")]
    public void ThenTheCharactersReadAre(string characters) =>
        Describe(Read.Characters.Select(snapshot => snapshot.Character)).ShouldBe(characters);

    /// <summary>Checks the incomplete characters.</summary>
    /// <param name="characters">The characters as <c>Name (Realm)</c>, comma-separated, or <c>none</c>.</param>
    [Then("the incomplete characters are {string}")]
    public void ThenTheIncompleteCharactersAre(string characters) => Describe(Read.IncompleteCharacters).ShouldBe(characters);

    /// <summary>Checks a character's capture time.</summary>
    /// <param name="name">The character's name.</param>
    /// <param name="capturedAt">The capture time.</param>
    [Then("the character {string} was captured at {int}")]
    public void ThenTheCharacterWasCapturedAt(string name, int capturedAt) => Character(name).CapturedAt.ShouldBe(capturedAt);

    /// <summary>Checks that a character's entry is kept as the addon wrote it.</summary>
    /// <param name="name">The character's name.</param>
    /// <param name="characterClass">The class token.</param>
    [Then("the character {string} keeps its class {string}")]
    public void ThenTheCharacterKeepsItsClass(string name, string characterClass) =>
        Character(name).Body["identity"]!["class"]!.GetValue<string>().ShouldBe(characterClass);
    #endregion Then Steps

    #region Private Helpers
    /// <summary>Describes characters as the scenarios write them.</summary>
    /// <param name="characters">The characters.</param>
    /// <returns>The description.</returns>
    private static string Describe(IEnumerable<CharacterKey> characters)
    {
        var described = string.Join(", ", characters.Select(character => $"{character.Name} ({character.Realm})"));
        return described.Length == 0 ? "none" : described;
    }

    /// <summary>Finds a character read.</summary>
    /// <param name="name">The character's name.</param>
    /// <returns>The snapshot.</returns>
    private CharacterSnapshot Character(string name) => Read.Characters.Single(snapshot => snapshot.Character.Name == name);
    #endregion Private Helpers
}

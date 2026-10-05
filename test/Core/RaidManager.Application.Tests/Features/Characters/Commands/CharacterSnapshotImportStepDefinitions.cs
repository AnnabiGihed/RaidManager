using Microsoft.Extensions.Time.Testing;
using Moq;
using Pivot.Framework.Domain.Repositories;
using Pivot.Framework.Domain.Shared;
using Reqnroll;
using Shouldly;
using RaidManager.Application.Features.Characters.Commands.ImportCharacterSnapshot;
using RaidManager.Domain.Features.Characters.Aggregates;
using RaidManager.Domain.Features.Characters.Enums;
using RaidManager.Domain.Features.Characters.Repositories;
using RaidManager.Domain.Features.Characters.ValueObjects;
using RaidManager.Domain.Features.Companions.Aggregates;
using RaidManager.Domain.Features.Companions.Repositories;
using RaidManager.Domain.Features.Companions.ValueObjects;
using RaidManager.Domain.Features.Shared.Enums;
using RaidManager.Domain.Features.Shared.Identifiers;

namespace RaidManager.Application.Tests.Features.Characters.Commands;

/// <summary>Defines business-readable steps for importing character snapshots through the application layer.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-05<br/>
/// Purpose: Verifies that the import command finds the companion and the character, imports a new character with a
/// claim for the companion's player, commits only what must be kept, and that the validator refuses a snapshot that
/// breaks the addon contract (#384).
/// </remarks>
[Binding]
[Scope(Feature = "Character snapshot import")]
public sealed class CharacterSnapshotImportStepDefinitions
{
    #region Fields
    /// <summary>Stores the clock the handler and the validator read; it starts at the real time, which the Character aggregate checks.</summary>
    private readonly FakeTimeProvider _clock = new(DateTimeOffset.UtcNow);

    /// <summary>Stores one stable user identifier per player name used in the scenario.</summary>
    private readonly Dictionary<string, UserId> _players = [];

    /// <summary>Stores the characters the repository double knows, by name.</summary>
    private readonly Dictionary<string, Character> _known = [];

    /// <summary>Stores the character repository double.</summary>
    private readonly Mock<ICharacterRepository> _characters = new();

    /// <summary>Stores the companion repository double.</summary>
    private readonly Mock<ICompanionRepository> _companions = new();

    /// <summary>Stores the unit-of-work double that records commits.</summary>
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    /// <summary>Stores the companion under test.</summary>
    private Companion? _companion;

    /// <summary>Stores the result the next commit returns.</summary>
    private Result _commitResult = Result.Success();

    /// <summary>Stores the latest snapshot sent.</summary>
    private SnapshotCharacter? _sent;

    /// <summary>Stores the result of the latest upload.</summary>
    private Result<SnapshotImportOutcome>? _upload;

    /// <summary>Stores the result of the latest validation.</summary>
    private FluentValidation.Results.ValidationResult? _validation;
    #endregion Fields

    #region Properties
    /// <summary>Gets the latest upload's result, failing the scenario if none ran.</summary>
    private Result<SnapshotImportOutcome> Upload => _upload ?? throw new InvalidOperationException("No snapshot was uploaded in this scenario.");

    /// <summary>Gets the latest validation's result, failing the scenario if none ran.</summary>
    private FluentValidation.Results.ValidationResult Validation => _validation ?? throw new InvalidOperationException("No snapshot was validated in this scenario.");
    #endregion Properties

    #region Given Steps
    /// <summary>Pairs a companion for a player and wires the repository doubles.</summary>
    /// <param name="player">The player name.</param>
    [Given("the companion of {string} is paired")]
    public void GivenTheCompanionOfIsPaired(string player)
    {
        var pairedAtUtc = _clock.GetUtcNow().AddDays(-1);
        var pairing = CompanionPairing.Start(CredentialHash.Of("device-code"), PairingCode.Create("K7M4QX"), "BRYN-DESKTOP", pairedAtUtc);
        pairing.Confirm(PlayerId(player), pairedAtUtc).IsSuccess.ShouldBeTrue();
        _companion = pairing.Complete(CredentialHash.Of("device-token"), pairedAtUtc).Value;
        var companionId = _companion.Id;
        _companions
            .Setup(repository => repository.FindByIdAsync(It.IsAny<CompanionId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((CompanionId id, CancellationToken _) => id == companionId ? _companion : null);
        _characters
            .Setup(repository => repository.FindByRealmAndNameAsync(It.IsAny<WarmaneRealm>(), It.IsAny<CharacterName>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((WarmaneRealm _, CharacterName name, CancellationToken _) => _known.GetValueOrDefault(name.Value));
        _characters
            .Setup(repository => repository.AddAsync(It.IsAny<Character>(), It.IsAny<CancellationToken>()))
            .Callback((Character character, CancellationToken _) => _known[character.Name.Value] = character)
            .Returns(Task.CompletedTask);
        _unitOfWork
            .Setup(unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => _commitResult);
    }

    /// <summary>Makes a player the owner of a known character.</summary>
    /// <param name="player">The player name.</param>
    /// <param name="name">The character name.</param>
    [Given("{string} owns {string}")]
    public void GivenOwns(string player, string name)
    {
        var character = Character.Import(WarmaneRealm.Icecrown, CharacterName.Create(name), WowClass.DeathKnight, WowRace.Undead, Faction.Horde, 80);
        character.RequestClaim(PlayerId(player), _clock.GetUtcNow()).IsSuccess.ShouldBeTrue();
        character.ApproveClaim(PlayerId(player), _clock.GetUtcNow()).IsSuccess.ShouldBeTrue();
        _known[name] = character;
    }

    /// <summary>Uploads a snapshot as an arrangement step, then forgets the commits it made.</summary>
    /// <param name="name">The character name.</param>
    /// <param name="hours">Hours since the snapshot was captured.</param>
    /// <returns>A task that completes when the snapshot is uploaded.</returns>
    [Given("the companion uploaded a snapshot of {string} captured {int} hours ago")]
    public async Task GivenTheCompanionUploadedASnapshotOfCapturedHoursAgo(string name, int hours)
    {
        (await SendAsync(SnapshotSamples.Character(name, _clock.GetUtcNow().AddHours(-hours)))).Value.ShouldBe(SnapshotImportOutcome.Imported);
        _unitOfWork.Invocations.Clear();
        _clock.Advance(TimeSpan.FromMinutes(5));
    }

    /// <summary>Makes the companion unknown to the repository, as after its deletion.</summary>
    [Given("the companion no longer exists")]
    public void GivenTheCompanionNoLongerExists() =>
        _companions
            .Setup(repository => repository.FindByIdAsync(It.IsAny<CompanionId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Companion?)null);

    /// <summary>Makes the next commit fail.</summary>
    [Given("the commit will fail")]
    public void GivenTheCommitWillFail() =>
        _commitResult = Result.Failure(new Error("Commit.Failed", "The database rejected the change."));
    #endregion Given Steps

    #region When Steps
    /// <summary>Uploads a complete snapshot of a character.</summary>
    /// <param name="name">The character name.</param>
    /// <param name="hours">Hours since the snapshot was captured.</param>
    /// <returns>A task that completes when the upload has been handled.</returns>
    [When("the companion uploads a snapshot of {string} captured {int} hours ago")]
    public async Task WhenTheCompanionUploadsASnapshotOfCapturedHoursAgo(string name, int hours) =>
        _upload = await SendAsync(SnapshotSamples.Character(name, _clock.GetUtcNow().AddHours(-hours)));

    /// <summary>Uploads a snapshot whose identity the game didn't answer.</summary>
    /// <param name="name">The character name.</param>
    /// <returns>A task that completes when the upload has been handled.</returns>
    [When("the companion uploads a snapshot of {string} without its identity")]
    public async Task WhenTheCompanionUploadsASnapshotOfWithoutItsIdentity(string name) =>
        _upload = await SendAsync(SnapshotSamples.Character(name, _clock.GetUtcNow().AddHours(-1)) with
        {
            Identity = new SnapshotIdentity(SnapshotSamples.Unavailable, null, null, null, null, null),
        });

    /// <summary>Uploads the latest snapshot sent once more.</summary>
    /// <returns>A task that completes when the upload has been handled.</returns>
    [When("the companion uploads the same snapshot again")]
    public async Task WhenTheCompanionUploadsTheSameSnapshotAgain() => _upload = await SendAsync(_sent!);

    /// <summary>Validates a complete snapshot.</summary>
    [When("a snapshot without changes is validated")]
    public void WhenASnapshotWithoutChangesIsValidated() => _validation = Validate(Command());

    /// <summary>Validates a snapshot with one change that breaks the contract.</summary>
    /// <param name="change">The change, as the examples name it.</param>
    [When("a snapshot with {string} is validated")]
    public void WhenASnapshotWithIsValidated(string change) => _validation = Validate(Broken(change));
    #endregion When Steps

    #region Then Steps
    /// <summary>Asserts the upload's outcome.</summary>
    /// <param name="outcome">The expected outcome.</param>
    [Then("the upload is {string}")]
    public void ThenTheUploadIs(string outcome)
    {
        Upload.IsSuccess.ShouldBeTrue(Upload.IsFailure ? Upload.Error.Message : null);
        Upload.Value.ShouldBe(Enum.Parse<SnapshotImportOutcome>(outcome));
    }

    /// <summary>Asserts the upload's failure code and its HTTP-relevant type.</summary>
    /// <param name="code">The expected error code.</param>
    /// <param name="type">The expected <see cref="ResultExceptionType"/> name.</param>
    [Then("the upload fails with {string} as {word}")]
    public void ThenTheUploadFailsWithAs(string code, string type)
    {
        Upload.IsFailure.ShouldBeTrue();
        Upload.Error.Code.ShouldBe(code);
        Upload.ResultExceptionType.ShouldBe(Enum.Parse<ResultExceptionType>(type));
    }

    /// <summary>Asserts that a new character was imported with a player's claim in a state.</summary>
    /// <param name="name">The character name.</param>
    /// <param name="state">The expected claim state.</param>
    /// <param name="player">The player name.</param>
    [Then("{string} is imported with a {string} claim for {string}")]
    public void ThenIsImportedWithAClaimFor(string name, string state, string player)
    {
        _characters.Verify(repository => repository.AddAsync(It.Is<Character>(character => character.Name.Value == name), It.IsAny<CancellationToken>()), Times.Once);
        ThenHasAClaimFor(name, state, player);
    }

    /// <summary>Asserts the state of a player's claim on a character.</summary>
    /// <param name="name">The character name.</param>
    /// <param name="state">The expected claim state.</param>
    /// <param name="player">The player name.</param>
    [Then("{string} has a {string} claim for {string}")]
    public void ThenHasAClaimFor(string name, string state, string player) =>
        _known[name].Claims.Single(claim => claim.RequestedByUserId == PlayerId(player)).State.ShouldBe(Enum.Parse<CharacterClaimState>(state));

    /// <summary>Asserts a character's owner.</summary>
    /// <param name="player">The player name.</param>
    /// <param name="name">The character name.</param>
    [Then("{string} owns {string}")]
    public void ThenOwns(string player, string name) => _known[name].OwnerId.ShouldBe(PlayerId(player));

    /// <summary>Asserts that the companion's last upload is now.</summary>
    [Then("the companion's last upload is recorded")]
    public void ThenTheCompanionsLastUploadIsRecorded() => _companion!.LastUploadAtUtc.ShouldBe(_clock.GetUtcNow());

    /// <summary>Asserts that the import was committed once.</summary>
    [Then("the import is committed")]
    public void ThenTheImportIsCommitted() =>
        _unitOfWork.Verify(unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);

    /// <summary>Asserts that nothing was committed.</summary>
    [Then("nothing is committed")]
    public void ThenNothingIsCommitted() =>
        _unitOfWork.Verify(unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);

    /// <summary>Asserts that the validation passed.</summary>
    [Then("the validation passes")]
    public void ThenTheValidationPasses() => Validation.IsValid.ShouldBeTrue(string.Join("; ", Validation.Errors));

    /// <summary>Asserts that the validation failed on a property.</summary>
    /// <param name="property">The expected property path.</param>
    [Then("the validation fails on {string}")]
    public void ThenTheValidationFailsOn(string property) =>
        Validation.Errors.Select(error => error.PropertyName).ShouldContain(property, string.Join("; ", Validation.Errors.Select(error => error.PropertyName)));
    #endregion Then Steps

    #region Private Helpers
    /// <summary>Gets the stable identifier of a player name.</summary>
    /// <param name="player">The player name.</param>
    /// <returns>The identifier.</returns>
    private UserId PlayerId(string player)
    {
        if (!_players.TryGetValue(player, out var id))
        {
            id = new UserId(Guid.NewGuid());
            _players[player] = id;
        }

        return id;
    }

    /// <summary>Sends an upload of a snapshot by the companion under test.</summary>
    /// <param name="snapshot">The snapshot.</param>
    /// <returns>The result.</returns>
    private Task<Result<SnapshotImportOutcome>> SendAsync(SnapshotCharacter snapshot)
    {
        _sent = snapshot;
        return new ImportCharacterSnapshotCommandHandler(_characters.Object, _companions.Object, _unitOfWork.Object, _clock)
            .Handle(new ImportCharacterSnapshotCommand(_companion!.Id.Value, 1, snapshot), CancellationToken.None);
    }

    /// <summary>Builds a valid command around a complete snapshot.</summary>
    /// <param name="character">The snapshot, or the complete sample when omitted.</param>
    /// <returns>The command.</returns>
    private ImportCharacterSnapshotCommand Command(SnapshotCharacter? character = null) =>
        new(Guid.NewGuid(), 1, character ?? SnapshotSamples.Character("Arthasdk", _clock.GetUtcNow().AddHours(-1)));

    /// <summary>Validates a command.</summary>
    /// <param name="command">The command.</param>
    /// <returns>The result.</returns>
    private FluentValidation.Results.ValidationResult Validate(ImportCharacterSnapshotCommand command) =>
        new ImportCharacterSnapshotCommandValidator(_clock).Validate(command);

    /// <summary>Builds a command with one change that breaks the addon contract.</summary>
    /// <param name="change">The change, as the examples name it.</param>
    /// <returns>The command.</returns>
    private ImportCharacterSnapshotCommand Broken(string change)
    {
        var sample = Command().Character!;
        var talents = sample.Talents!;
        var group = talents.Groups![0];
        return change switch
        {
            "schema version 2" => Command() with { SchemaVersion = 2 },
            "no companion" => Command() with { CompanionId = Guid.Empty },
            "no character" => Command() with { Character = null },
            "realm Northrend" => Command(sample with { Realm = "Northrend" }),
            "realm 1" => Command(sample with { Realm = "1" }),
            "name with a digit" => Command(sample with { Name = "Arthas2" }),
            "capture tomorrow" => Command(sample with { CapturedAt = _clock.GetUtcNow().AddDays(1).ToUnixTimeSeconds() }),
            "unknown section status" => Command(sample with { Guild = sample.Guild! with { Status = "partial" } }),
            "observed guild without time" => Command(sample with { Guild = sample.Guild! with { ObservedAt = null } }),
            "unknown class" => Command(sample with { Identity = sample.Identity! with { Class = "MONK" } }),
            "unknown race" => Command(sample with { Identity = sample.Identity! with { Race = "Goblin" } }),
            "unknown faction" => Command(sample with { Identity = sample.Identity! with { Faction = "Neutral" } }),
            "level 81" => Command(sample with { Identity = sample.Identity! with { Level = 81 } }),
            "guild without a name" => Command(sample with { Guild = sample.Guild! with { Name = null } }),
            "profession above its maximum" => Command(sample with { Professions = sample.Professions! with { Items = [new SnapshotProfession("Mining", 451, 450)] } }),
            "slot 20" => Command(sample with { Equipped = sample.Equipped! with { Slots = [new SnapshotGearSlot(20, true, null, null, null)] } }),
            "slot listed twice" => Command(sample with { Equipped = sample.Equipped! with { Slots = [new SnapshotGearSlot(4, true, null, null, null), new SnapshotGearSlot(4, true, null, null, null)] } }),
            "item without an id" => Command(sample with { Equipped = sample.Equipped! with { Slots = [new SnapshotGearSlot(1, null, null, "item:51312", null)] } }),
            "third talent group" => Command(sample with { Talents = talents with { ActiveGroup = 3 } }),
            "two talent trees" => Command(sample with { Talents = talents with { Groups = [group with { Tabs = [.. group.Tabs!.Take(2)] }] } }),
            "ranks with a letter" => Command(sample with { Talents = talents with { Groups = [group with { Tabs = [group.Tabs![0] with { Ranks = "30a0" }, group.Tabs[1], group.Tabs[2]] }] } }),
            "scan without completeness" => Command(sample with { Lockouts = sample.Lockouts! with { Complete = null } }),
            "reset in 61 days" => Command(sample with { Lockouts = sample.Lockouts! with { Items = [sample.Lockouts.Items![0] with { ResetSeconds = 61L * 24 * 60 * 60 }] } }),
            _ => throw new ArgumentOutOfRangeException(nameof(change), change, "Unknown change."),
        };
    }
    #endregion Private Helpers
}

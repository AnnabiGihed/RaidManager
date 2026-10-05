using Pivot.Framework.Application.Abstractions.Messaging.Commands;
using Pivot.Framework.Domain.Repositories;
using Pivot.Framework.Domain.Shared;
using RaidManager.Domain.Features.Characters.Aggregates;
using RaidManager.Domain.Features.Characters.Errors;
using RaidManager.Domain.Features.Characters.Repositories;
using RaidManager.Domain.Features.Characters.ValueObjects;
using RaidManager.Domain.Features.Companions.Errors;
using RaidManager.Domain.Features.Companions.Repositories;
using RaidManager.Domain.Features.Shared.Identifiers;

namespace RaidManager.Application.Features.Characters.Commands.ImportCharacterSnapshot;

/// <summary>Handles <see cref="ImportCharacterSnapshotCommand"/>: imports or loads the character, applies the snapshot, and commits it.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-05<br/>
/// Purpose: Implements the character workflow of the sync sequence (ADR-0002, story #17): a new character is imported, the
/// companion's player gets a pending claim (an owner keeps the character), the aggregate applies only a newer snapshot,
/// and the companion's last upload is recorded for the website's paired companions list.
/// </remarks>
internal sealed class ImportCharacterSnapshotCommandHandler : ICommandHandler<ImportCharacterSnapshotCommand, SnapshotImportOutcome>
{
    #region Fields
    /// <summary>Stores the character repository.</summary>
    private readonly ICharacterRepository _characters;

    /// <summary>Stores the companion repository.</summary>
    private readonly ICompanionRepository _companions;

    /// <summary>Stores the unit of work that commits the import.</summary>
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>Stores the clock that timestamps the claim and the upload.</summary>
    private readonly TimeProvider _timeProvider;
    #endregion Fields

    #region Constructors
    /// <summary>Initializes a new instance of the <see cref="ImportCharacterSnapshotCommandHandler"/> class.</summary>
    /// <param name="characters">The character repository.</param>
    /// <param name="companions">The companion repository.</param>
    /// <param name="unitOfWork">The unit of work that commits the import.</param>
    /// <param name="timeProvider">The clock that timestamps the claim and the upload.</param>
    public ImportCharacterSnapshotCommandHandler(
        ICharacterRepository characters,
        ICompanionRepository companions,
        IUnitOfWork unitOfWork,
        TimeProvider timeProvider)
    {
        _characters = characters;
        _companions = companions;
        _unitOfWork = unitOfWork;
        _timeProvider = timeProvider;
    }
    #endregion Constructors

    #region Public Methods
    /// <summary>Imports the snapshot and commits it.</summary>
    /// <param name="request">The validated command.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>
    /// <see cref="SnapshotImportOutcome.Imported"/> or <see cref="SnapshotImportOutcome.AlreadyCurrent"/>;
    /// <see cref="CompanionErrors.NotFound"/> when the companion is gone, <see cref="CharacterErrors.IdentityUnavailable"/>
    /// (<see cref="ResultExceptionType.ValidationError"/>) for an unknown character without its identity, or the commit's
    /// failure.
    /// </returns>
    public async Task<Result<SnapshotImportOutcome>> Handle(ImportCharacterSnapshotCommand request, CancellationToken cancellationToken)
    {
        var companion = await _companions.FindByIdAsync(new CompanionId(request.CompanionId), cancellationToken);
        if (companion is null)
        {
            return Result.Failure<SnapshotImportOutcome>(CompanionErrors.NotFound, ResultExceptionType.NotFound);
        }

        var snapshot = request.Character!;
        var realm = AddonSnapshotMapper.ParseRealm(snapshot.Realm!);
        var name = CharacterName.Create(snapshot.Name!);
        var character = await _characters.FindByRealmAndNameAsync(realm, name, cancellationToken);
        var isNew = character is null;
        if (character is null)
        {
            if (AddonSnapshotMapper.ToIdentity(snapshot.Identity) is not { } identity)
            {
                return Result.Failure<SnapshotImportOutcome>(CharacterErrors.IdentityUnavailable, ResultExceptionType.ValidationError);
            }

            character = Character.Import(realm, name, identity.Class, identity.Race, identity.Faction, identity.Level);
        }

        var nowUtc = _timeProvider.GetUtcNow();

        // A conflict claim on a character another player owns is kept for review, so the result is the claim itself.
        _ = character.RequestClaim(companion.UserId, nowUtc);
        var applied = character.SynchronizeAddonSnapshot(AddonSnapshotMapper.ToAddonSnapshot(snapshot));
        companion.RecordUpload(nowUtc);

        await (isNew ? _characters.AddAsync(character, cancellationToken) : _characters.UpdateAsync(character, cancellationToken));
        await _companions.UpdateAsync(companion, cancellationToken);
        var saved = await _unitOfWork.SaveChangesAsync(cancellationToken);
        if (saved.IsFailure)
        {
            return Result.Failure<SnapshotImportOutcome>(saved.Error, saved.ResultExceptionType);
        }

        return applied ? SnapshotImportOutcome.Imported : SnapshotImportOutcome.AlreadyCurrent;
    }
    #endregion Public Methods
}

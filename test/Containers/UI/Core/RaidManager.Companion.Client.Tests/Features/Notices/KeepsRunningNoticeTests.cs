using RaidManager.Companion.Client.Features.Notices;
using Shouldly;
using Xunit;

namespace RaidManager.Companion.Client.Tests.Features.Notices;

/// <summary>Verifies the first-close rule of the keeps-running notice.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-04<br/>
/// Purpose: Technical tests of <see cref="KeepsRunningNotice"/>: the first close shows the notice, later closes and
/// later starts don't, and a marker that can't be written leaves the companion working (#528).
/// </remarks>
public sealed class KeepsRunningNoticeTests : IDisposable
{
    #region Fields
    /// <summary>Stores the test's temporary folder.</summary>
    private readonly string _folder = Path.Combine(Path.GetTempPath(), $"companion-notice-tests-{Guid.NewGuid():N}");
    #endregion Fields

    #region Private Properties
    /// <summary>Gets the marker's location, in a folder that doesn't exist yet.</summary>
    private NoticeMarkerLocation Location => new(Path.Combine(_folder, "RaidManager", "keeps-running-notice.shown"));
    #endregion Private Properties

    #region Public Methods
    /// <summary>Deletes the temporary folder.</summary>
    public void Dispose()
    {
        if (Directory.Exists(_folder))
        {
            Directory.Delete(_folder, recursive: true);
        }
    }
    #endregion Public Methods

    #region Tests
    /// <summary>The first close shows the notice; once recorded, no later close or start does.</summary>
    [Fact]
    public void OnlyTheFirstCloseShowsTheNotice()
    {
        var notice = new KeepsRunningNotice(Location);
        notice.IsFirstClose.ShouldBeTrue();

        notice.RecordShown();

        notice.IsFirstClose.ShouldBeFalse();
        new KeepsRunningNotice(Location).IsFirstClose.ShouldBeFalse();
    }

    /// <summary>A marker that can't be written is ignored; the notice may show again, nothing fails.</summary>
    [Fact]
    public void UnwritableMarkerIsIgnored()
    {
        Directory.CreateDirectory(_folder);
        var blocker = Path.Combine(_folder, "RaidManager");
        File.WriteAllText(blocker, "a file where the folder should be");
        var notice = new KeepsRunningNotice(Location);

        Should.NotThrow(notice.RecordShown);

        notice.IsFirstClose.ShouldBeTrue();
    }

    /// <summary>The current user's marker sits next to the token file.</summary>
    [Fact]
    public void CurrentUserMarkerIsInTheLocalApplicationData() =>
        NoticeMarkerLocation.ForCurrentUser().FilePath.ShouldBe(Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "RaidManager", "keeps-running-notice.shown"));

    /// <summary>The notice needs a location.</summary>
    [Fact]
    public void ConstructorWithoutALocationThrows() => Should.Throw<ArgumentNullException>(() => new KeepsRunningNotice(null!));
    #endregion Tests
}

using System.Runtime.Versioning;
using RaidManager.Companion.Features.Shared;
using RaidManager.Companion.Features.Tokens;
using Shouldly;
using Xunit;

namespace RaidManager.Companion.Tests.Features;

/// <summary>Verifies the companion's Windows-only code; on any other system these tests are skipped.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-04<br/>
/// Purpose: The DPAPI token protector round-trips the token for the current user, and the single instance lets only
/// one companion run and wakes it when another start is refused (avalonia-tests §4). Run them on Windows before
/// handover: CI on Linux reports them as skipped.
/// </remarks>
public sealed class WindowsOnlyTests
{
    #region Constants
    /// <summary>Defines why the tests are skipped elsewhere.</summary>
    private const string WindowsOnly = "DPAPI and named events exist only on Windows.";
    #endregion Constants

    #region Tests
    /// <summary>The protector round-trips the token, and the protected bytes don't contain it.</summary>
    [Fact]
    [SupportedOSPlatform("windows")]
    public void DpapiProtectorRoundTripsTheToken()
    {
        Assert.SkipUnless(OperatingSystem.IsWindows(), WindowsOnly);
        var protector = new DpapiTokenProtector();
        var token = "device-token"u8.ToArray();

        var protectedToken = protector.Protect(token);

        protectedToken.ShouldNotBe(token);
        protector.Unprotect(protectedToken).ShouldBe(token);
    }

    /// <summary>A second start is refused, and its signal wakes the first.</summary>
    [Fact]
    [SupportedOSPlatform("windows")]
    public void SecondStartWakesTheFirst()
    {
        Assert.SkipUnless(OperatingSystem.IsWindows(), WindowsOnly);
        var name = $"RaidManager.Companion.Tests.{Guid.NewGuid():N}";
        using var woken = new ManualResetEventSlim();

        using var running = SingleInstance.Acquire(name);
        running.Listen(woken.Set);
        var second = RunOnOtherThread(() =>
        {
            using var refused = SingleInstance.Acquire(name);
            if (!refused.IsFirst)
            {
                refused.ActivateFirst();
            }

            return refused.IsFirst;
        });

        running.IsFirst.ShouldBeTrue();
        second.ShouldBeFalse();
        woken.Wait(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken).ShouldBeTrue();
    }
    #endregion Tests

    #region Private Helpers
    /// <summary>Runs a function on its own thread, as a second start would hold the mutex from its own process.</summary>
    /// <param name="function">The function.</param>
    /// <returns>Its result.</returns>
    private static bool RunOnOtherThread(Func<bool> function)
    {
        var result = false;
        var thread = new Thread(() => result = function());
        thread.Start();
        thread.Join();
        return result;
    }
    #endregion Private Helpers
}

using RaidManager.Companion.Client.Features.Shared;
using Shouldly;
using Xunit;

namespace RaidManager.Companion.Client.Tests.Features.Shared;

/// <summary>Verifies the companion's two command types.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-04<br/>
/// Purpose: Technical tests of <see cref="RelayCommand"/> and <see cref="AsyncRelayCommand"/>: an asynchronous command
/// runs once at a time, tells bound controls when it starts and ends, and hands a failure to its owner instead of
/// letting it reach the dispatcher.
/// </remarks>
public sealed class CommandTests
{
    #region Tests
    /// <summary>A relay command runs its action and can always run.</summary>
    [Fact]
    public void RelayCommandExecuteRunsTheAction()
    {
        var runs = 0;
        var command = new RelayCommand(() => runs++);
        var notified = 0;
        command.CanExecuteChanged += (_, _) => notified++;

        command.Execute(null);
        command.NotifyCanExecuteChanged();

        runs.ShouldBe(1);
        command.CanExecute(null).ShouldBeTrue();
        notified.ShouldBe(1);
    }

    /// <summary>An asynchronous command is disabled while it runs, and ignores a second run meanwhile.</summary>
    /// <returns>A task that completes when the test is done.</returns>
    [Fact]
    public async Task AsyncRelayCommandWhileRunningIsDisabledAndRunsOnce()
    {
        var release = new TaskCompletionSource();
        var runs = 0;
        var command = new AsyncRelayCommand(
            async () =>
            {
                runs++;
                await release.Task;
            },
            _ => { });
        var changes = new List<bool>();
        command.CanExecuteChanged += (_, _) => changes.Add(command.CanExecute(null));

        var first = command.ExecuteAsync();
        await command.ExecuteAsync();
        command.CanExecute(null).ShouldBeFalse();
        release.SetResult();
        await first;

        runs.ShouldBe(1);
        changes.ShouldBe([false, true]);
    }

    /// <summary>A failure of the action goes to the owner, and the command can run again.</summary>
    [Fact]
    public void AsyncRelayCommandWhenTheActionFailsHandsTheFailureOver()
    {
        Exception? handed = null;
        var failure = new InvalidOperationException("Broken.");
        var command = new AsyncRelayCommand(() => Task.FromException(failure), exception => handed = exception);

        command.Execute(null);

        handed.ShouldBeSameAs(failure);
        command.CanExecute(null).ShouldBeTrue();
    }

    /// <summary>Both commands refuse a missing action.</summary>
    [Fact]
    public void ConstructorsWithoutAnActionThrow()
    {
        Should.Throw<ArgumentNullException>(() => new RelayCommand(null!));
        Should.Throw<ArgumentNullException>(() => new AsyncRelayCommand(null!, _ => { }));
        Should.Throw<ArgumentNullException>(() => new AsyncRelayCommand(() => Task.CompletedTask, null!));
    }
    #endregion Tests
}

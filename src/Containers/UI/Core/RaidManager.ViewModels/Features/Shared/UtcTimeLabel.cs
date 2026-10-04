using System.Globalization;

namespace RaidManager.ViewModels.Features.Shared;

/// <summary>Labels a moment in UTC the way the website shows times: today, yesterday, or the date.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-04<br/>
/// Purpose: Gives every page the same time wording, such as <c>Today, 14:05 UTC</c> or <c>28 Sep, 21:40 UTC</c>, first used on the character review page.
/// </remarks>
public static class UtcTimeLabel
{
    #region Public Methods
    /// <summary>Labels a moment relative to the current day, in UTC.</summary>
    /// <param name="moment">The moment to label.</param>
    /// <param name="now">The current time.</param>
    /// <returns>For example <c>Today, 14:05 UTC</c>, <c>Yesterday, 21:40 UTC</c>, <c>28 Sep, 21:40 UTC</c> or <c>28 Sep 2025, 21:40 UTC</c>.</returns>
    public static string Format(DateTimeOffset moment, DateTimeOffset now)
    {
        var at = moment.ToUniversalTime();
        var today = now.UtcDateTime.Date;
        var time = at.ToString("HH:mm", CultureInfo.InvariantCulture);
        var day = at.UtcDateTime.Date;
        if (day == today)
        {
            return $"Today, {time} UTC";
        }

        if (day == today.AddDays(-1))
        {
            return $"Yesterday, {time} UTC";
        }

        var format = at.Year == today.Year ? "d MMM" : "d MMM yyyy";
        return $"{at.ToString(format, CultureInfo.InvariantCulture)}, {time} UTC";
    }
    #endregion Public Methods
}

namespace RaidManager.Web.Features.Shared.Components;

/// <summary>Describes one option of a <see cref="ChoiceList{TValue}"/> or an <see cref="OptionSelect{TValue}"/>.</summary>
/// <typeparam name="TValue">The type of the option's value.</typeparam>
/// <param name="Value">The value chosen with this option.</param>
/// <param name="Label">The visible label.</param>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-01<br/>
/// Purpose: Feeds a choice list its options, so the list holds no wording.
/// </remarks>
public sealed record ChoiceOption<TValue>(TValue Value, string Label);

using Shouldly;
using Xunit;
using RaidManager.ViewModels.Features.Characters;

namespace RaidManager.ViewModels.Tests.Features.Characters;

/// <summary>Verifies the words the character pages use for the names the API sends.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-05<br/>
/// Purpose: Pins the wording of the character profile mockup (story #19) and the fallbacks for unknown names.
/// </remarks>
public sealed class CharacterLabelsTests
{
    #region Tests
    /// <summary>Words classes, roles, raids, difficulties, slots and sources, and passes unknown names through.</summary>
    [Fact]
    public void NamesAreWordedAsTheProfileShowsThem()
    {
        CharacterLabels.Class("DeathKnight").ShouldBe("Death Knight");
        CharacterLabels.Class("Mage").ShouldBe("Mage");
        CharacterLabels.Role("RangedDamage").ShouldBe("Ranged damage");
        CharacterLabels.Instance("TrialOfTheCrusader").ShouldBe("Trial of the Crusader");
        CharacterLabels.Difficulty("TenPlayer").ShouldBe("10 players");
        CharacterLabels.Slot("TrinketTwo").ShouldBe("Trinket");
        CharacterLabels.Slot("RangedOrRelic").ShouldBe("Ranged");
        CharacterLabels.Slot("Head").ShouldBe("Head");
        CharacterLabels.Source("WarmaneArmory").ShouldBe("from the Warmane Armory");
        CharacterLabels.Role("Unknown").ShouldBe("Unknown");
        CharacterLabels.GearScore(5712).ShouldBe("GearScore 5,712");
    }

    /// <summary>Reads the item name from an in-game link, or falls back to the item identifier.</summary>
    /// <param name="link">The item link.</param>
    /// <param name="name">The expected name.</param>
    [Theory]
    [InlineData("|cffa335ee|Hitem:50737:0:0:0:0:0:0:0:80|h[Havoc's Call]|h|r", "Havoc's Call")]
    [InlineData("|Hitem:50737|h|h", "Item 50737")]
    public void ItemNameComesFromTheLink(string link, string name) =>
        CharacterLabels.ItemName(new CharacterGearItem("MainHand", 50737, link, 264)).ShouldBe(name);
    #endregion Tests
}

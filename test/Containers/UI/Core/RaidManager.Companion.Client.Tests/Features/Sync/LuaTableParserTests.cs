using System.Text.Json.Nodes;
using RaidManager.Companion.Client.Features.Sync.SavedVariables;
using Shouldly;
using Xunit;

namespace RaidManager.Companion.Client.Tests.Features.Sync;

/// <summary>Verifies the SavedVariables reader on the Lua that WoW writes, and on files it must refuse.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-07<br/>
/// Purpose: Technical tests of <see cref="LuaTableParser"/>: keyed and positional tables, strings with escapes,
/// numbers, booleans, <c>nil</c>, comments, a cut file and text that isn't SavedVariables (#550). Nothing is executed.
/// </remarks>
public sealed class LuaTableParserTests
{
    #region Tests
    /// <summary>Keyed entries become an object and positional entries an array, whatever the spacing and comments.</summary>
    [Fact]
    public void KeyedAndPositionalTablesBecomeObjectsAndArrays()
    {
        var document = LuaTableParser.Parse(
            "RaidManagerDB = {\n\t[\"name\"] = \"Arthasdk\",\n\tlevel = 80,\n\t[\"items\"] = {\n"
            + "\t\t{ [\"slot\"] = 1 }, -- [1]\n\t\t{ [\"slot\"] = 2; }, -- [2]\n\t},\n}\n");

        var database = document.Globals["RaidManagerDB"].ShouldBeOfType<JsonObject>();
        database["name"]!.GetValue<string>().ShouldBe("Arthasdk");
        database["level"]!.GetValue<long>().ShouldBe(80);
        database["items"].ShouldBeOfType<JsonArray>().Select(item => item!["slot"]!.GetValue<long>()).ShouldBe([1L, 2L]);
        document.Truncated.ShouldBeFalse();
    }

    /// <summary>An empty table is an empty list, as the contract's empty <c>items</c> are.</summary>
    [Fact]
    public void AnEmptyTableIsAnEmptyArray() =>
        Value("{}").ShouldBeOfType<JsonArray>().Count.ShouldBe(0);

    /// <summary>Numeric keys that aren't positions keep their key as text.</summary>
    [Fact]
    public void NumericKeysBecomeObjectKeys() =>
        Value("{ [5] = \"five\", [1] = \"one\" }").ShouldBeOfType<JsonObject>()["5"]!.GetValue<string>().ShouldBe("five");

    /// <summary>Lua escapes are decoded, and UTF-8 text such as an accented name is kept.</summary>
    [Fact]
    public void StringEscapesAreDecoded() =>
        Value(@"""Jaína \""x\"" a\\b\n\065\9\'""").GetValue<string>().ShouldBe("Jaína \"x\" a\\b\nA\t'");

    /// <summary>Single-quoted strings and the remaining escapes are read, a backslash before a line break included.</summary>
    [Fact]
    public void SingleQuotedStringsAndControlEscapesAreRead() =>
        Value("'\\r\\a\\b\\f\\v\\t\\\n'").GetValue<string>().ShouldBe("\r\a\b\f\v\t\n");

    /// <summary>Fractions and exponents become doubles.</summary>
    /// <param name="text">The Lua number.</param>
    /// <param name="expected">The value.</param>
    [Theory]
    [InlineData("0.5", 0.5d)]
    [InlineData("1.5e3", 1500d)]
    [InlineData("2E-2", 0.02d)]
    public void NumbersAreRead(string text, double expected) =>
        Value(text).GetValue<double>().ShouldBe(expected);

    /// <summary>A whole number, negative ones included, is read as an integer.</summary>
    /// <param name="text">The Lua number.</param>
    /// <param name="expected">The value.</param>
    [Theory]
    [InlineData("1791043200", 1791043200L)]
    [InlineData("-1218372352", -1218372352L)]
    public void WholeNumbersAreIntegers(string text, long expected) => Value(text).GetValue<long>().ShouldBe(expected);

    /// <summary>Booleans are read; a <c>nil</c> entry is left out.</summary>
    [Fact]
    public void BooleansAreReadAndNilIsLeftOut()
    {
        var table = Value("{ on = true, off = false, gone = nil }").ShouldBeOfType<JsonObject>();

        table["on"]!.GetValue<bool>().ShouldBeTrue();
        table["off"]!.GetValue<bool>().ShouldBeFalse();
        table.ContainsKey("gone").ShouldBeFalse();
    }

    /// <summary>Positional booleans are values, not keys.</summary>
    [Fact]
    public void PositionalKeywordsAreValues() =>
        Value("{ true, false }").ShouldBeOfType<JsonArray>().Select(item => item!.GetValue<bool>()).ShouldBe([true, false]);

    /// <summary>Line and long comments are skipped anywhere.</summary>
    [Fact]
    public void CommentsAreSkipped() =>
        Value("--[[ long\ncomment ]] { -- line\n [\"a\"] = 1 --[[ inline ]] }").ShouldBeOfType<JsonObject>()["a"]!.GetValue<long>().ShouldBe(1);

    /// <summary>A file cut inside an entry keeps the entries before it and names the keys down to the cut.</summary>
    [Fact]
    public void ACutFileKeepsEarlierEntriesAndRecordsTheCutPath()
    {
        var document = LuaTableParser.Parse("DB = { [\"done\"] = { [\"a\"] = 1 }, [\"cut\"] = { [\"b\"] = \"unfin");

        document.Truncated.ShouldBeTrue();
        document.CutPath.ShouldBe(["DB", "cut", "b"]);
        document.Globals["DB"]!["done"]!["a"]!.GetValue<long>().ShouldBe(1);
    }

    /// <summary>The end of the text anywhere but between statements means the file was cut.</summary>
    /// <param name="text">The cut text.</param>
    [Theory]
    [InlineData("DB")]
    [InlineData("DB =")]
    [InlineData("DB = { [\"a")]
    [InlineData("DB = { [\"a\"")]
    [InlineData("DB = { [\"a\"]")]
    [InlineData("DB = { [\"a\"] = 12")]
    [InlineData("DB = { [\"a\"] = tru")]
    [InlineData("DB = { [\"a\"] = 1")]
    [InlineData("DB = { [\"a\"] = 1,")]
    [InlineData("DB = { [\"a\"] = \"x\\")]
    [InlineData("DB = { --[[ open comment")]
    public void TextEndingEarlyIsTruncated(string text) => LuaTableParser.Parse(text).Truncated.ShouldBeTrue();

    /// <summary>Text that isn't SavedVariables Lua is refused rather than guessed.</summary>
    /// <param name="text">The text.</param>
    [Theory]
    [InlineData("DB = { [\"a\"] = print(1) }")]
    [InlineData("DB = { [\"a\"] 1 }")]
    [InlineData("DB = { [{}] = 1 }")]
    [InlineData("DB = { [\"a\"] = 1 [\"b\"] = 2 }")]
    [InlineData("DB = \"line\nbreak\"")]
    [InlineData("DB = \"\\q\"")]
    [InlineData("DB = 0x1F ")]
    [InlineData("DB = @")]
    [InlineData("= 1")]
    [InlineData("DB : 1")]
    public void TextThatIsNotSavedVariablesIsRefused(string text) =>
        Should.Throw<FormatException>(() => LuaTableParser.Parse(text));
    #endregion Tests

    #region Private Helpers
    /// <summary>Parses one value assigned to a global.</summary>
    /// <param name="lua">The value's Lua.</param>
    /// <returns>The value as JSON.</returns>
    private static JsonNode Value(string lua) => LuaTableParser.Parse($"Value = {lua}\n").Globals["Value"]!;
    #endregion Private Helpers
}

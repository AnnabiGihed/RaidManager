-- Author: Gihed Annabi
-- Purpose: Specs for the professions capture, compared with the one-character fixture of the snapshot contract.
local addon = dofile("test/Addon/helpers/addon.lua")
local world = dofile("test/Addon/helpers/world.lua")

describe("the professions capture", function()
    local fixture = addon.FixtureCharacter()

    local function capture(overrides)
        local api = world.Arthasdk(overrides)
        return addon.Load(api).ns.CaptureProfessions(api, world.NOW)
    end

    it("records the professions as the fixture shows them, and nothing from other headers", function()
        assert.are.same(fixture.professions, capture())
    end)

    it("records a character with no profession as observed with no items", function()
        local lines = {
            { "Professions", true, true },
            { "Secondary Skills", true, true },
            { "Weapon Skills", true, true },
            { "Axes", false, false, 60, 0, 0, 60 },
        }

        assert.are.same({ status = "observed", observedAt = world.NOW, items = {} }, capture(world.SkillList(lines)))
    end)

    it("is unavailable when a professions header is collapsed, since its rows are hidden", function()
        local lines = { { "Professions", true, false }, { "Secondary Skills", true, true } }

        assert.are.same(
            { status = "unavailable", reason = "collapsed", attemptedAt = world.NOW },
            capture(world.SkillList(lines))
        )
    end)

    it("is unavailable while the game hasn't loaded the skill list", function()
        assert.are.same(
            { status = "unavailable", reason = "not-loaded", attemptedAt = world.NOW },
            capture(world.SkillList({}))
        )
    end)

    it("finds the headers by the client's localized names", function()
        local lines = {
            { "Berufe", true, true },
            { "Schmiedekunst", false, false, 450, 0, 0, 450 },
            { "Waffenfertigkeiten", true, true },
            { "Äxte", false, false, 400, 0, 0, 400 },
        }
        local overrides = world.SkillList(lines)
        overrides.TRADE_SKILLS = "Berufe"
        overrides.SECONDARY_SKILLS = "Sekundäre Fertigkeiten:"

        local section = capture(overrides)

        assert.are.same({ { name = "Schmiedekunst", header = "Berufe", rank = 450, maxRank = 450 } }, section.items)
    end)

    it("falls back to the English headers when the client's strings are missing", function()
        local section = capture({ TRADE_SKILLS = false, SECONDARY_SKILLS = false })

        assert.are.equal(5, #section.items)
    end)
end)

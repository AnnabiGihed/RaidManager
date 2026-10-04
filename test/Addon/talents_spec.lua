-- Author: Gihed Annabi
-- Purpose: Specs for the talents and glyphs capture, compared with the snapshot contract's fixtures.
local addon = dofile("test/Addon/helpers/addon.lua")
local world = dofile("test/Addon/helpers/world.lua")

describe("the talents capture", function()
    local fixture = addon.FixtureCharacter()

    local function load(overrides)
        local api = world.Arthasdk(overrides)
        return addon.Load(api).ns, api
    end

    it("records both talent groups, their ranks and glyphs as the fixture shows them", function()
        local ns, api = load()

        assert.are.same(fixture.talents, ns.CaptureTalents(api, world.NOW))
    end)

    it("records an unlocked socket without a glyph as empty", function()
        local ns, api = load()

        assert.are.same({ socket = 3, type = 2, enabled = true, empty = true }, ns.CaptureGlyph(api, 3, 2))
    end)

    it("records the locked sockets of a low-level character as not enabled", function()
        local db = addon.LoadSavedVariables(addon.FIXTURES .. addon.FIXTURE_FILES[3])
        local talents = db.characters["Icecrown|Bankalt"].talents
        local ns, api = load(world.TalentFrame(talents))

        assert.are.same(talents, ns.CaptureTalents(api, talents.observedAt))
    end)

    it("is unavailable while the game hasn't loaded the talent trees", function()
        local ns, api = load({
            GetNumTalentTabs = function()
                return 0
            end,
        })

        assert.are.same(
            { status = "unavailable", reason = "not-loaded", attemptedAt = world.NOW },
            ns.CaptureTalents(api, world.NOW)
        )
    end)

    it("is unavailable while a tree's name is missing", function()
        local ns, api = load({ GetTalentTabInfo = world.Nothing })

        assert.are.same(
            { status = "unavailable", reason = "not-loaded", attemptedAt = world.NOW },
            ns.CaptureTalents(api, world.NOW)
        )
    end)
end)

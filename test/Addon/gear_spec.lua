-- Author: Gihed Annabi
-- Purpose: Specs for the equipped gear capture, compared with the one-character fixture of the snapshot contract.
local addon = dofile("test/Addon/helpers/addon.lua")
local world = dofile("test/Addon/helpers/world.lua")

describe("the gear capture", function()
    local fixture = addon.FixtureCharacter()

    local function load(overrides)
        local api = world.Arthasdk(overrides)
        return addon.Load(api).ns, api
    end

    it("records the equipped gear as the fixture shows it, empty slots included", function()
        local ns, api = load()

        assert.are.same(fixture.equipped, ns.CaptureEquipped(api, world.NOW))
    end)

    it("reads an item string with enchant, gems, a negative suffix and a unique id", function()
        local ns = load()

        local item = ns.ParseItemLink(world.Link("item:50693:3817:40117:40118:0:0:-39:1074591232:80"))

        assert.are.same({
            itemString = "item:50693:3817:40117:40118:0:0:-39:1074591232:80",
            itemId = 50693,
            enchantId = 3817,
            gems = { 40117, 40118, 0, 0 },
            suffixId = -39,
            uniqueId = 1074591232,
        }, item)
    end)

    it("refuses a link without a complete item string", function()
        local ns = load()

        assert.is_nil(ns.ParseItemLink(nil))
        assert.is_nil(ns.ParseItemLink("|cff9d9d9d|Hspell:12345|h[Spell]|h|r"))
        assert.is_nil(ns.ParseItemLink(world.Link("item:50693:0:0")))
    end)

    it("marks a slot unavailable when it holds an item the client hasn't described yet", function()
        local ns, api = load({
            GetInventoryItemLink = world.Nothing,
            GetInventoryItemTexture = function()
                return "Interface\\Icons\\INV_Misc_QuestionMark"
            end,
        })

        assert.are.same({ slot = 13, status = "unavailable", reason = "not-cached" }, ns.CaptureSlot(api, 13))
    end)

    it("marks a slot unavailable when its link can't be read", function()
        local ns, api = load({
            GetInventoryItemLink = function()
                return world.Link("item:50693")
            end,
        })

        assert.are.same({ slot = 1, status = "unavailable", reason = "unreadable-link" }, ns.CaptureSlot(api, 1))
    end)

    it("records an empty slot as empty, not unavailable", function()
        local ns, api = load()

        assert.are.same({ slot = 4, empty = true }, ns.CaptureSlot(api, 4))
    end)
end)

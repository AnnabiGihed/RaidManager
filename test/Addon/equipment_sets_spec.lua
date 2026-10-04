-- Author: Gihed Annabi
-- Purpose: Specs for the equipment sets capture, compared with the snapshot contract's fixtures.
local addon = dofile("test/Addon/helpers/addon.lua")
local world = dofile("test/Addon/helpers/world.lua")

describe("the equipment sets capture", function()
    local fixture = addon.FixtureCharacter()

    local function load(overrides)
        local api = world.Arthasdk(overrides)
        return addon.Load(api).ns, api
    end

    it("records the sets as the fixture shows them, items worn and in the bags, ignored slots included", function()
        local ns, api = load()

        assert.are.same(fixture.equipmentSets, ns.CaptureEquipmentSets(api, world.NOW))
    end)

    it("records a set item in the bank as unavailable with the item id the set records", function()
        local incomplete = addon.LoadSavedVariables(addon.FIXTURES .. addon.FIXTURE_FILES[4])
        local sets = incomplete.characters["Icecrown|Arthasdk"].equipmentSets
        local ns, api = load(world.EquipmentManager(sets.items))

        assert.are.same(sets.items, ns.CaptureEquipmentSets(api, sets.observedAt).items)
    end)

    it("records a character without sets as observed with no items", function()
        local ns, api = load(world.EquipmentManager({}))

        assert.are.same(
            { status = "observed", observedAt = world.NOW, items = {} },
            ns.CaptureEquipmentSets(api, world.NOW)
        )
    end)

    it("marks an empty slot empty and a missing item unavailable", function()
        local ns, api = load()

        assert.are.same({ slot = 4, empty = true }, ns.CaptureSetSlot(api, 4, 0, 0))
        assert.are.same(
            { slot = 2, status = "unavailable", reason = "missing", itemId = 50728 },
            ns.CaptureSetSlot(api, 2, 50728, -1)
        )
    end)

    it("marks an item unavailable when its link can't be read", function()
        local ns, api = load({ GetInventoryItemLink = world.Nothing })

        assert.are.same(
            { slot = 1, status = "unavailable", reason = "not-cached", itemId = 51312 },
            ns.CaptureSetSlot(api, 1, 51312, { player = true, slot = 1 })
        )
    end)

    it("is unavailable while the game hasn't loaded a set's name", function()
        local ns, api = load({ GetEquipmentSetInfo = world.Nothing })

        assert.are.same(
            { status = "unavailable", reason = "not-loaded", attemptedAt = world.NOW },
            ns.CaptureEquipmentSets(api, world.NOW)
        )
    end)
end)

-- Author: Gihed Annabi
-- Purpose: Specs for the raid saves capture, compared with the snapshot contract's fixtures.
local addon = dofile("test/Addon/helpers/addon.lua")
local world = dofile("test/Addon/helpers/world.lua")

describe("the raid saves capture", function()
    local function fixtureLockouts(file, key)
        return addon.LoadSavedVariables(addon.FIXTURES .. addon.FIXTURE_FILES[file]).characters[key].lockouts
    end

    local function load(overrides)
        local api = world.Arthasdk(overrides)
        return addon.Load(api).ns, api
    end

    it("records the saves with their reset data as the raid-save fixture shows them", function()
        local lockouts = fixtureLockouts(5, "Icecrown|Arthasdk")
        local ns, api = load(world.SavedInstances(lockouts))

        assert.are.same(lockouts, ns.CaptureLockouts(api, lockouts.observedAt))
    end)

    it("records a character without saves as a complete scan with no items", function()
        local ns, api = load(world.SavedInstances({}))

        assert.are.same(
            { status = "observed", observedAt = world.NOW, complete = true, items = {} },
            ns.CaptureLockouts(api, world.NOW)
        )
    end)

    it("marks the scan incomplete when a counted instance can't be read", function()
        local incomplete = fixtureLockouts(4, "Icecrown|Sylvanash")
        local ns, api = load(world.SavedInstances({ incomplete.items[1], false }))

        assert.are.same(incomplete, ns.CaptureLockouts(api, incomplete.observedAt))
    end)

    it("asks the game at most once every few seconds", function()
        local saves = world.SavedInstances({})
        local ns, api = load(saves)

        assert.is_true(ns.RequestRaidInfo(api, world.NOW))
        assert.is_false(ns.RequestRaidInfo(api, world.NOW + 4))
        assert.is_true(ns.RequestRaidInfo(api, world.NOW + 5))
        assert.are.equal(2, saves.requests)
    end)
end)

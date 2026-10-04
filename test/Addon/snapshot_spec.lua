-- Author: Gihed Annabi
-- Purpose: Specs for the snapshot writer: the saved table, new characters and sections.
local addon = dofile("test/Addon/helpers/addon.lua")
local world = dofile("test/Addon/helpers/world.lua")

describe("the snapshot writer", function()
    local ns

    before_each(function()
        ns = addon.Load(world.Arthasdk()).ns
    end)

    it("prepares an empty saved table with the schema and addon versions", function()
        local db = ns.InitializeDatabase(nil)

        assert.are.same({ schemaVersion = 1, addonVersion = "0.1.0", characters = {} }, db)
    end)

    it("keeps the characters already saved", function()
        local saved = { schemaVersion = 1, characters = { ["Icecrown|Bankalt"] = { realm = "Icecrown" } } }

        local db = ns.InitializeDatabase(saved)

        assert.are.equal(saved.characters["Icecrown|Bankalt"], db.characters["Icecrown|Bankalt"])
    end)

    it("starts a new character with every section unavailable until it is captured", function()
        local db = ns.InitializeDatabase(nil)

        local character = ns.CharacterSnapshot(db, "Icecrown", "Arthasdk", world.NOW)

        assert.are.equal(character, db.characters["Icecrown|Arthasdk"])
        for _, section in ipairs(ns.SECTIONS) do
            assert.are.same(
                { status = "unavailable", reason = "not-captured", attemptedAt = world.NOW },
                character[section]
            )
        end
    end)

    it("returns the same snapshot for a character already saved", function()
        local db = ns.InitializeDatabase(nil)
        local first = ns.CharacterSnapshot(db, "Icecrown", "Arthasdk", world.NOW)

        local again = ns.CharacterSnapshot(db, "Icecrown", "Arthasdk", world.NOW + 60)

        assert.are.equal(first, again)
    end)

    it("replaces a section and records when the snapshot changed", function()
        local character = ns.CharacterSnapshot(ns.InitializeDatabase(nil), "Icecrown", "Arthasdk", world.NOW)

        ns.StoreSection(character, "guild", ns.Observed(world.NOW + 5, { inGuild = false }), world.NOW + 5)

        assert.are.same({ status = "observed", observedAt = world.NOW + 5, inGuild = false }, character.guild)
        assert.are.equal(world.NOW + 5, character.capturedAt)
    end)
end)

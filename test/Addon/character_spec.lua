-- Author: Gihed Annabi
-- Purpose: Specs for capturing the logged-in character into its snapshot.
local addon = dofile("test/Addon/helpers/addon.lua")
local contract = dofile("test/Addon/helpers/contract.lua")
local world = dofile("test/Addon/helpers/world.lua")

local NOT_YET = { "professions", "equipped", "equipmentSets", "talents", "lockouts" }

describe("capturing the character", function()
    local fixture = addon.FixtureCharacter()

    local function load(overrides)
        local api = world.Arthasdk(overrides)
        local ns = addon.Load(api).ns
        return ns, api, ns.InitializeDatabase(nil)
    end

    it("writes the realm, name, client, server time, identity and guild of the fixture", function()
        local ns, api, db = load()

        local character = ns.CaptureCharacter(api, db, world.NOW)

        for _, field in ipairs({ "realm", "name", "capturedAt", "client", "serverTime", "identity", "guild" }) do
            assert.are.same(fixture[field], character[field], field)
        end
        assert.are.equal(character, db.characters["Icecrown|Arthasdk"])
    end)

    it("marks the sections this version doesn't capture as unavailable, never empty", function()
        local ns, api, db = load()

        local character = ns.CaptureCharacter(api, db, world.NOW)

        for _, section in ipairs(NOT_YET) do
            assert.are.same(
                { status = "unavailable", reason = "not-captured", attemptedAt = world.NOW },
                character[section]
            )
        end
    end)

    it("writes a snapshot that complies with the contract", function()
        local ns, api, db = load()

        ns.CaptureCharacter(api, db, world.NOW)

        assert.are.same({}, contract.Problems(db))
    end)

    it("leaves the other characters of the account alone", function()
        local ns, api, db = load()
        local other = { realm = "Lordaeron", name = "Jaína" }
        db.characters["Lordaeron|Jaína"] = other

        ns.CaptureCharacter(api, db, world.NOW)

        assert.are.equal(other, db.characters["Lordaeron|Jaína"])
    end)

    it("waits while the game doesn't know the character's name", function()
        local ns, api, db = load({ UnitName = world.Nothing })

        assert.is_nil(ns.CaptureCharacter(api, db, world.NOW))
        assert.are.same({}, db.characters)
    end)

    it("captures the guild again once the game has loaded it", function()
        local guildLoaded = false
        local ns, api, db = load({
            GetGuildInfo = function()
                if guildLoaded then
                    return "Dark Templars", "Officer", 1
                end
            end,
        })
        ns.CaptureCharacter(api, db, world.NOW)
        guildLoaded = true

        local character = ns.CaptureGuildAgain(api, db, world.NOW + 5)

        assert.are.equal("observed", character.guild.status)
        assert.are.equal("Dark Templars", character.guild.name)
        assert.are.equal(world.NOW + 5, character.capturedAt)
    end)

    it("doesn't capture the guild of a character not captured yet", function()
        local ns, api, db = load()

        assert.is_nil(ns.CaptureGuildAgain(api, db, world.NOW))
    end)

    it("describes each section's status for the slash command", function()
        local ns, api, db = load()
        local character = ns.CaptureCharacter(api, db, world.NOW)

        local lines = ns.DescribeSnapshot(character)

        assert.are.equal("Icecrown|Arthasdk", lines[1])
        assert.are.equal("identity: observed", lines[2])
        assert.are.equal("professions: unavailable (not-captured)", lines[4])
    end)
end)

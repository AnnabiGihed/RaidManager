-- Author: Gihed Annabi
-- Purpose: Specs for capturing the logged-in character into its snapshot.
local addon = dofile("test/Addon/helpers/addon.lua")
local contract = dofile("test/Addon/helpers/contract.lua")
local world = dofile("test/Addon/helpers/world.lua")

local NOT_YET = { "lockouts" }

describe("capturing the character", function()
    local fixture = addon.FixtureCharacter()

    local function load(overrides)
        local api = world.Arthasdk(overrides)
        local ns = addon.Load(api).ns
        return ns, api, ns.InitializeDatabase(nil)
    end

    it("writes the realm, name, client, server time, identity, guild, professions and gear of the fixture", function()
        local ns, api, db = load()

        local character = ns.CaptureCharacter(api, db, world.NOW)

        local fields = { "realm", "name", "capturedAt", "client", "serverTime", "identity", "guild", "professions" }
        fields[#fields + 1] = "equipped"
        fields[#fields + 1] = "equipmentSets"
        fields[#fields + 1] = "talents"
        for _, field in ipairs(fields) do
            assert.are.same(fixture[field], character[field], field)
        end
        assert.are.equal(character, db.characters["Icecrown|Arthasdk"])
    end)

    it("marks the saves unavailable until the game answers, never empty", function()
        local ns, api, db = load()

        local character = ns.CaptureCharacter(api, db, world.NOW)

        for _, section in ipairs(NOT_YET) do
            assert.are.same(
                { status = "unavailable", reason = "not-answered", attemptedAt = world.NOW },
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

        local character = ns.CaptureAgain(api, db, world.NOW + 5, "guild")

        assert.are.equal("observed", character.guild.status)
        assert.are.equal("Dark Templars", character.guild.name)
        assert.are.equal(world.NOW + 5, character.capturedAt)
    end)

    it("doesn't capture the guild of a character not captured yet", function()
        local ns, api, db = load()

        assert.is_nil(ns.CaptureAgain(api, db, world.NOW, "guild"))
    end)

    it("doesn't capture again a section this version doesn't know", function()
        local ns, api, db = load()
        ns.CaptureCharacter(api, db, world.NOW)

        assert.is_nil(ns.CaptureAgain(api, db, world.NOW, "achievements"))
    end)

    it("keeps the saves already answered when the character is captured again", function()
        local ns, api, db = load()
        local character = ns.CaptureCharacter(api, db, world.NOW)
        ns.CaptureAgain(api, db, world.NOW + 5, "lockouts")

        ns.CaptureCharacter(api, db, world.NOW + 60)

        assert.are.equal("observed", character.lockouts.status)
        assert.are.equal(world.NOW + 5, character.lockouts.observedAt)
    end)

    it("describes each section's status for the slash command", function()
        local ns, api, db = load()
        local character = ns.CaptureCharacter(api, db, world.NOW)

        local lines = ns.DescribeSnapshot(character)

        assert.are.equal("Icecrown|Arthasdk", lines[1])
        assert.are.equal("identity: observed", lines[2])
        assert.are.equal("professions: observed", lines[4])
        assert.are.equal("equipmentSets: observed", lines[6])
        assert.are.equal("lockouts: unavailable (not-answered)", lines[8])
    end)
end)

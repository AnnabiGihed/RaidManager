-- Author: Gihed Annabi
-- Purpose: Specs for the addon's events and slash command, with every file loaded in TOC order.
local addon = dofile("test/Addon/helpers/addon.lua")
local world = dofile("test/Addon/helpers/world.lua")

describe("the loaded addon", function()
    local function loaded(overrides)
        local game = addon.Load(world.Arthasdk(overrides))
        game.Fire("ADDON_LOADED", "RaidManager")
        return game
    end

    it("loads every file the TOC lists", function()
        assert.are.same({
            "Core.lua",
            "Snapshot.lua",
            "Capture/Identity.lua",
            "Capture/Professions.lua",
            "Capture/Gear.lua",
            "Character.lua",
            "Events.lua",
        }, addon.TocFiles())
    end)

    it("prepares the saved table when the client loads it, and stops listening", function()
        local game = addon.Load(world.Arthasdk())

        game.Fire("ADDON_LOADED", "SomeOtherAddon")
        assert.is_nil(game.env.RaidManagerDB)
        game.Fire("ADDON_LOADED", "RaidManager")

        assert.are.equal(1, game.env.RaidManagerDB.schemaVersion)
        assert.is_nil(game.frame.events.ADDON_LOADED)
    end)

    it("captures the character on entering the world", function()
        local game = loaded()

        game.Fire("PLAYER_ENTERING_WORLD")

        assert.are.equal("observed", game.env.RaidManagerDB.characters["Icecrown|Arthasdk"].identity.status)
    end)

    it("captures the guild when the game reports it for the player", function()
        local guildLoaded = false
        local game = loaded({
            GetGuildInfo = function()
                if guildLoaded then
                    return "Dark Templars", "Officer", 1
                end
            end,
        })
        game.Fire("PLAYER_ENTERING_WORLD")
        guildLoaded = true

        game.Fire("PLAYER_GUILD_UPDATE", "party1")
        local before = game.env.RaidManagerDB.characters["Icecrown|Arthasdk"].guild.status
        game.Fire("PLAYER_GUILD_UPDATE", "player")

        assert.are.equal("unavailable", before)
        assert.are.equal("observed", game.env.RaidManagerDB.characters["Icecrown|Arthasdk"].guild.status)
    end)

    it("captures the professions again when the skill list changes", function()
        local lines = { { "Professions", true, true } }
        local game = loaded({
            GetNumSkillLines = function()
                return #lines
            end,
            GetSkillLineInfo = function(index)
                return unpack(lines[index])
            end,
        })
        game.Fire("PLAYER_ENTERING_WORLD")
        lines[2] = { "Tailoring", false, false, 1, 0, 0, 75 }

        game.Fire("SKILL_LINES_CHANGED")

        local items = game.env.RaidManagerDB.characters["Icecrown|Arthasdk"].professions.items
        assert.are.same({ { name = "Tailoring", header = "Professions", rank = 1, maxRank = 75 } }, items)
    end)

    it("captures the gear again when the player's inventory changes, not another unit's", function()
        local wearsShirt = false
        local game = loaded({
            GetInventoryItemLink = function(_, slot)
                if slot == 4 and wearsShirt then
                    return world.Link("item:45:0:0:0:0:0:0:0:80")
                end
            end,
            GetInventoryItemTexture = world.Nothing,
        })
        game.Fire("PLAYER_ENTERING_WORLD")
        wearsShirt = true

        game.Fire("UNIT_INVENTORY_CHANGED", "target")
        local before = game.env.RaidManagerDB.characters["Icecrown|Arthasdk"].equipped.slots[4]
        game.Fire("UNIT_INVENTORY_CHANGED", "player")

        assert.is_true(before.empty)
        assert.are.equal(45, game.env.RaidManagerDB.characters["Icecrown|Arthasdk"].equipped.slots[4].itemId)
    end)

    it("shows the snapshot's sections with /rm", function()
        local game = loaded()
        game.Fire("PLAYER_ENTERING_WORLD")

        game.env.SlashCmdList.RAIDMANAGER("  ")

        assert.are.equal("RaidManager: Icecrown|Arthasdk", game.env.printed[1])
        assert.are.equal(8, #game.env.printed)
    end)

    it("captures again with /rm sync", function()
        local game = loaded()

        game.env.SlashCmdList.RAIDMANAGER("sync")

        assert.are.equal("observed", game.env.RaidManagerDB.characters["Icecrown|Arthasdk"].guild.status)
        assert.are.equal("RaidManager: Icecrown|Arthasdk", game.env.printed[1])
    end)

    it("says when the character isn't captured yet", function()
        local game = loaded()

        game.env.SlashCmdList.RAIDMANAGER("")

        assert.are.same({ "RaidManager: this character isn't captured yet; type /rm sync." }, game.env.printed)
    end)

    it("registers /rm and /raidmanager", function()
        local game = loaded()

        assert.are.equal("/raidmanager", game.env.SLASH_RAIDMANAGER1)
        assert.are.equal("/rm", game.env.SLASH_RAIDMANAGER2)
    end)
end)

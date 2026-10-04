-- Author: Gihed Annabi
-- Purpose: Specs that every fixture of the snapshot contract complies with it, and that the check finds breaks.
local addon = dofile("test/Addon/helpers/addon.lua")
local contract = dofile("test/Addon/helpers/contract.lua")

describe("the snapshot contract", function()
    for _, file in ipairs(addon.FIXTURE_FILES) do
        it("holds for the fixture " .. file, function()
            local db = addon.LoadSavedVariables(addon.FIXTURES .. file)

            assert.are.same({}, contract.Problems(db))
        end)
    end

    it("finds a section left out, a missing complete flag and ranks that don't add up", function()
        local db = addon.LoadSavedVariables(addon.FIXTURES .. addon.FIXTURE_FILES[5])
        local character = db.characters["Icecrown|Arthasdk"]
        character.professions = nil
        character.lockouts.complete = nil
        character.talents.groups[1].tabs[3].pointsSpent = 53

        local problems = contract.Problems(db)

        assert.are.same({
            "Icecrown|Arthasdk.professions: missing",
            "Icecrown|Arthasdk.talents: Unholy ranks add up to 54",
            "Icecrown|Arthasdk.lockouts: complete is missing",
        }, problems)
    end)

    it("finds an unavailable section that carries data", function()
        local db = addon.LoadSavedVariables(addon.FIXTURES .. addon.FIXTURE_FILES[4])
        db.characters["Icecrown|Arthasdk"].guild.name = "Dark Templars"

        assert.are.same({ "Icecrown|Arthasdk.guild: an unavailable section with name" }, contract.Problems(db))
    end)
end)

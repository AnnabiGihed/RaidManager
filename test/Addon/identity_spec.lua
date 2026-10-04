-- Author: Gihed Annabi
-- Purpose: Specs for the identity capture, compared with the one-character fixture of the snapshot contract.
local addon = dofile("test/Addon/helpers/addon.lua")
local world = dofile("test/Addon/helpers/world.lua")

describe("the identity capture", function()
    local fixture = addon.FixtureCharacter()

    local function capture(overrides)
        local api = world.Arthasdk(overrides)
        return addon.Load(api).ns, api
    end

    it("records the identity as the fixture shows it", function()
        local ns, api = capture()

        assert.are.same(fixture.identity, ns.CaptureIdentity(api, world.NOW))
    end)

    it("is unavailable while the game doesn't know the character yet", function()
        local ns, api = capture({ UnitGUID = world.Nothing })

        assert.are.same(
            { status = "unavailable", reason = "not-loaded", attemptedAt = world.NOW },
            ns.CaptureIdentity(api, world.NOW)
        )
    end)

    it("records the guild as the fixture shows it", function()
        local ns, api = capture()

        assert.are.same(fixture.guild, ns.CaptureGuild(api, world.NOW))
    end)

    it("records a character in no guild as observed, not unavailable", function()
        local ns, api = capture({ IsInGuild = world.Nothing, GetGuildInfo = world.Nothing })

        assert.are.same(
            { status = "observed", observedAt = world.NOW, inGuild = false },
            ns.CaptureGuild(api, world.NOW)
        )
    end)

    it("leaves the guild unavailable while the game hasn't loaded it after login", function()
        local ns, api = capture({ GetGuildInfo = world.Nothing })

        assert.are.same(
            { status = "unavailable", reason = "not-loaded", attemptedAt = world.NOW },
            ns.CaptureGuild(api, world.NOW)
        )
    end)

    it("records the client and the server's clock as the fixture shows them", function()
        local ns, api = capture()

        assert.are.same(fixture.client, ns.CaptureClient(api))
        assert.are.same(fixture.serverTime, ns.CaptureServerTime(api, world.NOW))
    end)
end)

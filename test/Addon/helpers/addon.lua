-- Author: Gihed Annabi
-- Purpose: Loads the addon's files in TOC order into a sandbox, as the client does, with a stubbed WoW API.
local M = {}

M.ROOT = "src/Addon/RaidManager/"
M.ADDON_NAME = "RaidManager"

--- Lists the Lua files the TOC loads, in its order.
---@return string[]
function M.TocFiles()
    local files = {}
    for line in io.lines(M.ROOT .. M.ADDON_NAME .. ".toc") do
        local file = line:match("^%s*([^#%s][^\r]*%.lua)%s*$")
        if file then
            files[#files + 1] = file:gsub("\\", "/")
        end
    end
    return files
end

local function trim(text)
    return (text:gsub("^%s+", ""):gsub("%s+$", ""))
end

--- Loads the addon with the given WoW API functions and returns its namespace, globals and event frame.
---@param world table the WoW API functions the client would provide
---@return table addon with ns, env, frame and Fire(event, ...)
function M.Load(world)
    local env = setmetatable({}, { __index = _G })
    for key, value in pairs(world or {}) do
        env[key] = value
    end
    env._G = env
    env.printed = {}
    env.print = function(text)
        env.printed[#env.printed + 1] = text
    end
    env.strtrim = trim
    env.SlashCmdList = {}

    local frame = { events = {} }
    function frame:SetScript(_, handler)
        self.onEvent = handler
    end
    function frame:RegisterEvent(event)
        self.events[event] = true
    end
    function frame:UnregisterEvent(event)
        self.events[event] = nil
    end
    env.CreateFrame = function()
        return frame
    end

    local ns = {}
    for _, file in ipairs(M.TocFiles()) do
        local chunk = assert(loadfile(M.ROOT .. file))
        setfenv(chunk, env)
        chunk(M.ADDON_NAME, ns)
    end

    return {
        ns = ns,
        env = env,
        frame = frame,
        Fire = function(event, ...)
            if frame.events[event] then
                frame.onEvent(frame, event, ...)
            end
        end,
    }
end

--- Loads a SavedVariables file and returns the RaidManagerDB it defines.
---@param path string
---@return table
function M.LoadSavedVariables(path)
    local chunk = assert(loadfile(path))
    local env = {}
    setfenv(chunk, env)
    chunk()
    return env.RaidManagerDB
end

M.FIXTURES = "test/Fixtures/Addon/SavedVariables/"
M.FIXTURE_FILES = {
    "one-character/WTF/Account/ARTHASACCOUNT/SavedVariables/RaidManager.lua",
    "multiple-accounts/WTF/Account/MAINACCOUNT/SavedVariables/RaidManager.lua",
    "multiple-accounts/WTF/Account/ALTACCOUNT/SavedVariables/RaidManager.lua",
    "incomplete-scan/WTF/Account/ARTHASACCOUNT/SavedVariables/RaidManager.lua",
    "raid-save/WTF/Account/ARTHASACCOUNT/SavedVariables/RaidManager.lua",
}

--- The character of the one-character fixture, which the stubbed world below plays.
---@return table
function M.FixtureCharacter()
    return M.LoadSavedVariables(M.FIXTURES .. M.FIXTURE_FILES[1]).characters["Icecrown|Arthasdk"]
end

return M

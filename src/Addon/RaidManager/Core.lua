-- Author: Gihed Annabi
-- Purpose: Addon namespace, event dispatch and slash commands.
local _, ns = ...

local frame = CreateFrame("Frame")
ns.handlers = {}

frame:SetScript("OnEvent", function(_, event, ...)
    local handler = ns.handlers[event]
    if handler then
        handler(...)
    end
end)

--- Registers the handler of an event; each event has one handler.
---@param event string
---@param handler function
function ns.On(event, handler)
    ns.handlers[event] = handler
    frame:RegisterEvent(event)
end

--- Stops listening to an event.
---@param event string
function ns.Off(event)
    ns.handlers[event] = nil
    frame:UnregisterEvent(event)
end

SLASH_RAIDMANAGER1 = "/raidmanager"
SLASH_RAIDMANAGER2 = "/rm"
SlashCmdList["RAIDMANAGER"] = function(message)
    ns.HandleSlashCommand(strtrim(message or ""))
end

-- Author: Gihed Annabi
-- Purpose: Captures the gear the character wears, slot by slot, from the item links (WoW 3.3.5a API).
local _, ns = ...

-- Inventory slots 1 (head) to 19 (tabard); 4 is the shirt.
ns.EQUIPPED_SLOTS = 19

-- An item string has nine numbers: item, enchant, four gems, suffix, unique id and the linking character's level.
local ITEM_STRING_PARTS = 9

--- Reads the ids of an item link; GetItemInfo isn't used, since it answers nothing for items the client hasn't cached.
---@param link string|nil such as "|cffa335ee|Hitem:51312:3817:41398:40117:0:0:0:1218372352:80|h[Name]|h|r"
---@return table|nil the item's fields, or nil when the link holds no complete item string
function ns.ParseItemLink(link)
    local body = link and link:match("item:([%-%d:]+)")
    if not body then
        return nil
    end
    local ids = {}
    for part in (body .. ":"):gmatch("([%-%d]*):") do
        ids[#ids + 1] = tonumber(part) or 0
    end
    if #ids < ITEM_STRING_PARTS then
        return nil
    end
    local parts = {}
    for index = 1, ITEM_STRING_PARTS do
        parts[index] = ids[index]
    end
    return {
        itemString = "item:" .. table.concat(parts, ":"),
        itemId = ids[1],
        enchantId = ids[2],
        gems = { ids[3], ids[4], ids[5], ids[6] },
        suffixId = ids[7],
        uniqueId = ids[8],
    }
end

--- One equipped slot: an item, empty, or unavailable when the slot holds an item the game didn't describe.
---@param api table the WoW API
---@param slot number 1 to 19
---@return table
function ns.CaptureSlot(api, slot)
    local link = api.GetInventoryItemLink("player", slot)
    local item = ns.ParseItemLink(link)
    if item then
        item.slot = slot
        return item
    end
    if link then
        return { slot = slot, status = "unavailable", reason = "unreadable-link" }
    end
    if api.GetInventoryItemTexture("player", slot) then
        return { slot = slot, status = "unavailable", reason = "not-cached" }
    end
    return { slot = slot, empty = true }
end

--- The equipped section: slots 1 to 19 in order.
---@param api table the WoW API
---@param now number seconds since 1970
---@return table
function ns.CaptureEquipped(api, now)
    local slots = {}
    for slot = 1, ns.EQUIPPED_SLOTS do
        slots[slot] = ns.CaptureSlot(api, slot)
    end
    return ns.Observed(now, { slots = slots })
end

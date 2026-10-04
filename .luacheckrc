-- Luacheck configuration for the WoW 3.3.5a addon and its specs (ADR-0031).
std = "lua51"
max_line_length = 120

-- The only globals the addon creates.
globals = { "RaidManagerDB", "SLASH_RAIDMANAGER1", "SLASH_RAIDMANAGER2", "SlashCmdList" }

-- The WoW API the addon calls directly; capture functions receive the rest as a table.
read_globals = { "CreateFrame", "strtrim" }

files["test/Addon/**/*.lua"] = { std = "+busted" }

# Test the addon

The RaidManager addon for World of Warcraft 3.3.5a lives in `src/Addon/RaidManager/`
([ADR-0031](../adr/0031-build-the-wow-addon-in-lua-under-src-addon.md)). Its specs prove the logic against the
fixtures of the [addon snapshot contract](../reference/addon-savedvariables.md); a check in the game proves the
client answers as the specs assume.

## Run the checks

The `addon` workflow runs luacheck, StyLua and busted on every pull request. To run them locally without installing
Lua, build a tools image once and run it from the repository folder:

```bash
docker run --name rm-lua-tools debian:bookworm-slim sh -c "apt-get update && apt-get install -y --no-install-recommends lua5.1 liblua5.1-0-dev luarocks build-essential git ca-certificates unzip curl && luarocks --lua-version 5.1 install busted 2.2.0-1 && luarocks --lua-version 5.1 install luacheck 1.2.0-1 && curl -sSL -o /tmp/s.zip https://github.com/JohnnyMorganz/StyLua/releases/download/v2.5.2/stylua-linux-x86_64.zip && unzip /tmp/s.zip -d /usr/local/bin"
```

```bash
docker commit rm-lua-tools rm-lua-tools:local
```

```bash
docker run --rm -v "$(pwd):/repo" -w /repo rm-lua-tools:local sh -c "luacheck src/Addon test/Addon && stylua --check src/Addon test/Addon && busted test/Addon"
```

In Git Bash on Windows, put `MSYS_NO_PATHCONV=1` before `docker run` and use `$(pwd -W)`.

## Check it in the game

1. Copy the `src/Addon/RaidManager` folder into `Interface/AddOns` of the 3.3.5a client, so that
   `Interface/AddOns/RaidManager/RaidManager.toc` exists.
2. Start the game, check that RaidManager is enabled in the character selection's AddOns list, and log in.
3. Type `/rm`. The chat lists each section of the snapshot with its status, such as `identity: observed`; a
   section the addon doesn't capture yet shows `unavailable (not-captured)`.
4. Type `/reload`, which makes the game write the file.
5. Open `WTF/Account/<ACCOUNT>/SavedVariables/RaidManager.lua` and send its contents for comparison with the
   fixtures. It holds no password or other credential.

`/console scriptErrors 1` shows any Lua error in the game.

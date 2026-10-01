# Set up the Discord application and bot

RaidManager uses one Discord application for three things: players sign in with it, communities add its bot to their
server, and the API asks Discord for members' roles with the bot's token
([ADR-0022](../adr/0022-check-discord-roles-at-action-time.md)). This guide sets it up for a local run and checks the
result against Discord's API.

Only the application's owner can change it in the
[Discord Developer Portal](https://discord.com/developers/applications). Tokens and secrets go into the Aspire AppHost's
user secrets, never into the repository, an issue or a chat.

## Configure the application

1. **OAuth2 → Redirects:** add both local return addresses:
   - `https://localhost:55365/signin-discord`, where Discord sign-in returns;
   - `https://localhost:55365/communities/link/discord`, where adding the bot to a server returns.
2. **Bot:**
   - Keep **Public Bot** on, so the managers of any server can add it.
   - Keep **Requires OAuth2 Code Grant** off. RaidManager asks for the full code grant itself when it adds the bot.
   - Turn on **Server Members Intent**. Listing a server's members requires it.
   - Use **Reset Token** and copy the token. Discord shows it once.
3. Store the token from the repository root:

   ```bash
   dotnet user-secrets set "Parameters:discord-bot-token" "<bot-token>" --project src/Containers/Aspire/Hosting/RaidManager.AppHost
   ```

4. Add the bot to a test server you manage, so role checks have a server to ask about. Open this address with your
   application's client id, pick the server, and authorize:

   ```text
   https://discord.com/oauth2/authorize?client_id=<client-id>&scope=bot&permissions=0
   ```

## Check the setup against Discord

Each check calls Discord's API with the bot token, as `Authorization: Bot <token>`. Read the token from the user
secrets inside the command, so it never appears on screen.

| Check | Call | Expected |
| --- | --- | --- |
| The token belongs to the application | `GET /applications/@me` | `id` is the client id |
| Server Members intent | the same call | `flags` includes `1 << 15` (`GATEWAY_GUILD_MEMBERS_LIMITED`) or `1 << 14` |
| Both return addresses | the same call | `redirect_uris` lists both addresses above |
| Anyone with the right permission can add the bot | the same call | `bot_public` is `true` and `bot_require_code_grant` is `false` |
| The bot is in the test server | `GET /users/@me/guilds` | the test server is listed |
| Role checks work | `GET /guilds/{server}/members/{user}` | your member object, with its `roles` |
| Someone outside the server isn't a member | the same call with a Discord user who isn't in the server | `404` with code `10007` (`Unknown Member`) |
| An id Discord doesn't know isn't a member | the same call with a made-up user id | `404` with code `10013` (`Unknown User`) |
| Role mapping can list roles | `GET /guilds/{server}/roles` | the server's roles |

All calls go to `https://discord.com/api/v10`.

Discord can place an application under a developer team, even one with a single member. Then the `owner` that
`GET /applications/@me` returns is the team's own account, which is in no server, and your account is
`team.owner_user_id`. Check the member call with that id, the id of the person who added the bot.

Then run the AppHost (see the README). The API refuses to start without the token, so a running API confirms the
AppHost passes it.

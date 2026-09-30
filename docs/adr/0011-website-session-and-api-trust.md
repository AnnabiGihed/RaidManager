# ADR-0011: Website session and API trust

- Status: Proposed
- Date: 2026-09-30
- Deciders: Gihed Annabi

## Context

[ADR-0001](0001-foundation-and-authentication.md) makes Discord OAuth the only sign-in provider and names the API
as the shared boundary for the website, the Discord bot, and the companion. The website is a Blazor Server app,
so its code runs on the server. The sign-in sequence needs a Discord identity to be resolved to one local user, and
later website requests act on that user's behalf.

## Decision

The website owns the player's session, and the API trusts the website server.

- The website runs the Discord OAuth authorization-code flow with the `AspNet.Security.OAuth.Discord` handler
  and keeps an HttpOnly, Secure, SameSite cookie session. Discord tokens are not kept after sign-in, and the
  browser never holds an API credential.
- The website proves itself to the API with a shared website key in the `X-RaidManager-Service-Key` header. The
  API compares it in constant time, never logs it, and refuses to start without a key of at least 32 characters.
- The Aspire AppHost generates the key as a secret parameter, persists it in the AppHost's user secrets, and gives
  it to both processes as `Website__ServiceKey`. Deployments supply it from their secret store.
- After Discord confirms an identity, the website calls `POST /internal/identity/discord-sign-in` and stores the
  returned local user id in its session. Later user-scoped calls carry the signed-in user id alongside the website
  key; the API authorizes the user from that id.
- `/internal/...` routes admit only the website scheme. The Discord bot and the desktop companion get their own
  credentials and schemes when they are built.

## Consequences

**Positive**

- No access or refresh token reaches the browser, so script injection cannot steal an API credential.
- Blazor Server keeps a single server-side session, which fits its circuit model.
- The API stays the one place for business rules, as ADR-0001 requires.

**Negative**

- The API trusts the user id the website sends. A leaked website key lets its holder act as any user, so the key is
  a high-value secret that needs rotation and must never leave server configuration.
- Rotating the key needs both processes restarted with the new value.

**Residual risk**

- A compromised website server can call the API as any user. Mutual TLS or per-request signed user assertions would
  narrow this and can be added without changing the endpoints.

## Alternatives considered

- **API owns sign-in and issues tokens to the website:** matches the original sequence diagram literally, but puts
  tokens in the website's hands and adds cross-origin cookie handling.
- **Website hosts the Application layer in-process:** removes an HTTP hop but splits business-rule hosting between
  two processes, against the API boundary of ADR-0001.
- **Built-in `AddOAuth` configured by hand instead of the Discord handler:** avoids a package but leaves Discord's
  endpoints and claim mapping for us to maintain.

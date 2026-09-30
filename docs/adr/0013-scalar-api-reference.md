# ADR-0013: Scalar as the API reference page

- Status: Proposed
- Date: 2026-09-30
- Deciders: Gihed Annabi

## Context

The API publishes its OpenAPI document at `/openapi/v1.json` with `Microsoft.AspNetCore.OpenApi`, the only document
generator the repository uses. .NET 10 no longer ships an interactive page in its templates, so developers had no way
to browse the operations or try them.

## Decision

The API serves the Scalar reference page (`Scalar.AspNetCore`) at `/scalar` in the Development environment only.

- Scalar only renders the existing `/openapi/v1.json`; `Microsoft.AspNetCore.OpenApi` stays the single generator.
- The page prefers the website-key security scheme of [ADR-0011](0011-website-session-and-api-trust.md), so a
  developer can paste the key from the AppHost's user secrets and call `/internal/...` operations. The key is never
  pre-filled.
- The Aspire dashboard shows an "API reference" link on the `api` resource.
- Other environments map no reference page; they publish only the OpenAPI document.

## Consequences

**Positive**

- Developers can explore and try every operation from the browser, with the security the API actually enforces.
- The page always matches the running code, because it reads the generated document.

**Negative**

- One more package to keep current, although it only affects Development.

## Alternatives considered

- **Swagger UI (Swashbuckle's UI package only):** familiar, but fewer features and less maintained with the built-in
  OpenAPI generator.
- **No page, only the JSON document:** workable with external tools, but slows everyday API work.

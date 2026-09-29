# ADR-0001: Foundation, authentication and game-data sources

- Status: Accepted
- Date: 2026-09-29
- Decision owners: Gihed Annabi

## Context

The product will initially target Warmane and WotLK 3.3.5a.
Its purpose is to improve raid planning by replacing manually maintained player character information with synchronized
game data. The future product must support Discord-driven raid signups and roster organization while remaining connected
to real character state.

The supplied engineering standards require .NET 10, Clean Architecture, DDD/CQRS, SQL Server, Blazor, Aspire and a
shared framework foundation. FASFC Bricks-specific requirements were explicitly replaced by Pivot.Framework.

## Decision

1. Use Discord OAuth as the only user authentication provider. No local password database will exist.
2. Use Pivot.Framework for domain, application, persistence and shared infrastructure primitives, excluding its
   Keycloak authentication packages.
3. Use Warmane Armory as a public data source where it is reliable.
4. Add a WoW addon synchronization channel for character data not represented adequately by the Armory, including raid
   lockouts and multiple equipment/loadout sets.
5. Model loadout-specific gear, GearScore, talents, glyphs and stats separately from character-wide raid lockouts.
6. Keep raid planning as its own domain feature so the Armory/character model remains reusable by the website and
   Discord bot.

## Consequences

- Discord account availability is required to use authenticated features.
- Character ownership must be verified separately through addon-assisted proof before private synchronized data is
  trusted.
- A character can expose several raid-ready loadouts even though the live character wears only one equipment set at a
  time.
- The API is the shared boundary for web, addon and Discord integrations.

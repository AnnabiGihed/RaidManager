# Architecture overview

Warmane Raid Manager separates player identity, synchronized character state and raid planning into distinct domain
features. The website and future Discord bot are presentation channels over the same application API.

```mermaid
flowchart LR
    Discord[Discord OAuth and bot] --> API[ASP.NET Core API]
    Web[Blazor Web] --> API
    Addon[WoW 3.3.5a addon sync] --> API
    Warmane[Warmane Armory] --> API
    API --> Application[Application CQRS]
    Application --> Domain[Domain]
    Application --> Persistence[EF Core persistence]
    Persistence --> Sql[(SQL Server)]
```

## Domain boundaries

- **Identity** owns the local application profile linked to a Discord snowflake identifier.
- **Characters** owns Warmane identity, verification, loadouts, synchronization freshness and raid lockouts.
- **Communities** owns Discord-backed raid communities and membership roles.
- **Raids** owns raid events, requirements, signups, offered loadout choices and final roster selections.

## Data ownership rule

Gear, GearScore, talents, glyphs and combat statistics belong to a **loadout**.
Raid lockouts belong to the **character**.
This prevents the common Armory limitation where only the currently worn gear can represent a character's raid
capability.

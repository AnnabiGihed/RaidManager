# ADR-0019: A dark design system with an app shell on every screen

- Status: Proposed
- Date: 2026-09-30
- Deciders: Gihed Annabi

## Context

The first generated mockup, the character review page, used the website's Radzen material light theme
([ADR-0012](0012-interactive-server-rendering-with-radzen.md)) with a single blue app bar. The file imported and
worked in Penpot, but the owner rejected the look. The owner then supplied a reference design, a dark "Command
Center" overview page, and asked that mockups and, later, the website follow it.

The reference design has:

- a near-black page with navy cards and one teal accent;
- a left sidebar with the logo, the community, the Player and Officer navigation, and the signed-in user;
- a top bar with a breadcrumb and the realm status;
- page headers made of a teal uppercase eyebrow, a large bold title and a one-line subtitle.

Its text is set in Segoe UI, which isn't a free font and isn't available in Penpot.

## Decision

- **One design system for mockups and the website,** taken from the reference design. The overview page itself is
  a style reference only; it doesn't add a home page to the scope.
- **Colors:** the dark palette in `HOUSE_PALETTE` (`scripts/penpot_scene.py`), one name per color, grouped as
  `Surface/`, `Line/`, `Brand/`, `Text/`, `Accent/`, `Status/`, `Avatar/` and `Community/`. The teal `Brand/accent`
  marks the primary action and the active page. Every text color passes WCAG AA on the surfaces it's used on;
  `Text/muted` is lighter than in the reference so it passes on cards.
- **Font:** Open Sans, the free Google font closest to Segoe UI, with the same humanist shapes and similar widths.
  Penpot offers it, and the website serves it from its own files under the Open Font License (#265). Headings
  are bold, and small uppercase labels carry letter spacing. `TYPE_SCALE` names each style.
- **App shell on every website screen:** a 1440 x 900 board with a 240-pixel sidebar and a 64-pixel top bar, drawn by
  `app_screen` in `scripts/penpot_components.py`:
  - the sidebar holds the logo, the community card, the Player section (Overview, Raids, My characters, Readiness,
    Companion & sync), the Officer section (Schedule, Roster builder, Raid night, Conflicts) shown only to officers,
    and the user card at the bottom;
  - the top bar holds the breadcrumb (community / page), the realm status, the user's avatar and Sign out;
  - the page's content starts 40 pixels from the sidebar and the top bar.
- **Signed-out pages have no shell** (owner decision, 2026-09-30): the sign-in page and its failure pages show the
  logo above a centered 480-pixel column, drawn by `public_screen`. There's no user or community to show, and
  nothing to navigate to before signing in.
- **Shared components:** page header, card with an optional accent bar, 40-pixel buttons with an 8-pixel radius
  (primary, secondary, danger), badges that always carry a word, notices, and avatars. Mockups use them instead of
  drawing their own.
- **Radzen stays.** The website is re-themed to this design system later (#203): a dark Radzen base theme with its
  CSS variables set from the palette, Open Sans, and the app shell in the main layout. Until then, the website and
  new mockups differ in look, and the mockups are the target.
- **The shell comes first** (owner decision, 2026-10-01): the main layout gets the dark shell before the rest of the
  re-theme, so pages are reachable. The sidebar lists only pages that exist, and each page's pull request adds its
  entry. Page contents keep the Radzen theme until the re-theme.
- **On the website** (owner decision, 2026-10-01): the palette and Radzen overrides live only in the project-wide
  theme `wwwroot/theme/raidmanager-theme.css`, as `--rm-*` tokens and the `.rm-theme-dark` class, and the shell's
  parts are standalone, generic components that pages reuse (#268).
- This decision amends [ADR-0018](0018-generate-penpot-mockups-in-the-repository.md): generated mockups use this
  design system instead of the Radzen material look.

## Consequences

**Positive**

- Every mockup has the same shell and parts, so screens stay consistent and are quicker to draw.
- Designers get the palette and type scale as named library assets in each Penpot file.

**Negative**

- The website doesn't match the mockups until it's re-themed (#203).
- Radzen components must be restyled through theme variables, and a component whose look can't be reached that way
  needs a scoped style.
- Open Sans is close to the reference font but not identical, so text widths differ slightly from the reference.

## Alternatives considered

- **Keep the Radzen material light look:** no re-theme work, but the owner rejected it.
- **Segoe UI:** matches the reference exactly, but it's licensed with Windows, isn't in Penpot, and falls back to
  another font on other systems.
- **Noto Sans or Source Sans 3:** also free and in Penpot. Noto Sans is nearly identical to Open Sans; Source Sans 3
  is narrower than the reference.
- **Another component library with a dark theme:** against the house standard, which makes Radzen mandatory.

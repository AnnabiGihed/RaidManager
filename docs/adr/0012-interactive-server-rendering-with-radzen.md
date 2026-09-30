# ADR-0012: Interactive Server rendering with Radzen

- Status: Proposed
- Date: 2026-09-30
- Deciders: Gihed Annabi

## Context

The Discord sign-in task is the website's first user interface. The house Blazor standard requires the render mode to
be decided, applied, verified, and recorded on the first UI delivery, and makes Radzen the component library. The
website template registered interactive server components but applied no render mode, so every page ran as static
server-side rendering while the interactive infrastructure loaded for nothing.

## Decision

- **Render mode:** the whole website runs `InteractiveServer`, applied once on `Routes` and `HeadOutlet` in
  `App.razor`, with prerendering on. Pages therefore render on the server first and then become interactive over a
  SignalR circuit.
- **Why Interactive Server:** the website is Blazor Server by house standard, the session is a server-side cookie
  (ADR-0011) that interactive WebAssembly could not use, and raid planning will need live interaction such as
  filtering and roster edits.
- **Sign-in and sign-out are HTTP endpoints, not components:** they change cookies, which only a plain HTTP request
  can do. Components reach them with a full page load (`forceLoad`) or a regular form post.
- **Component library:** Radzen Blazor with the `material` theme, set up once: `AddRadzenComponents`, the theme and
  script in `App.razor`, and `<RadzenComponents />` in the main layout. Bootstrap and other component libraries are
  not used.
- **Structure:** every component is a `.razor` markup file with a `.razor.cs` code-behind, logic lives in view models
  in `RaidManager.ViewModels`, and page content sits inside an error boundary in the layout.
- **Verification:** a website test asserts that pages carry the interactive server component marker, and bUnit tests
  exercise interactive handlers such as "Sign in with Discord" and "Try again".

## Consequences

**Positive**

- Interactive features work from the first page without per-component render-mode decisions.
- Server state and the cookie session stay on the server, matching ADR-0011.

**Negative**

- Each open tab holds a server circuit, so memory grows with concurrent players; components must dispose what they own.
- Code reachable from a circuit cannot use `HttpContext`; request data must be captured during prerender.

## Alternatives considered

- **Static server-side rendering with islands of interactivity:** lighter, but most planned screens are interactive
  and per-page decisions would multiply.
- **Interactive WebAssembly or Auto:** cannot use the server-side session of ADR-0011 without adding a token-handling
  backend for the browser.

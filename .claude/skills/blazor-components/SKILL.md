---
name: blazor-components
description: 'Write, fix, and refactor Blazor components for .NET 8–10: render-mode discipline (verified end-to-end and recorded in an ADR), mandatory .razor.cs code-behind separation (no @code blocks), view-model separation,
  component lifecycle, re-render correctness, lifecycle-linked cancellation, ErrorBoundary, forms/validation, JS interop, disposal,
  prerender state, bUnit coverage. Covers the mandatory Radzen component standard (setup, layout, data grids, forms, quirks). Use for any Blazor page, component, or UI-layer work.'
---

Use this skill for Blazor component work on .NET 8 through .NET 10: building new components, fixing render/lifecycle bugs, and refactoring for correctness and performance. For non-UI .NET idioms use `dotnet-best-practices`; for feature wiring use `dotnet-ddd-cqrs-conventions`. This skill owns the rules that are specific to Blazor's rendering model — the ones general C# knowledge gets wrong.

## Rules

- Establish the render mode before anything else. Identify whether the component runs under static SSR, `InteractiveServer`, `InteractiveWebAssembly`, or `InteractiveAuto`, and write code valid for that mode. A bug is often a mode mismatch, not a logic error.
- Never touch the DOM or call JS interop during prerender or static SSR — there is no DOM yet. Do JS interop in `OnAfterRenderAsync`, guarded by `firstRender` where appropriate.
- Assume any component that prerenders runs `OnInitializedAsync` twice (once server-side prerender, once interactive). Make initialization idempotent and persist fetched state across the boundary (see prerender state below) rather than fetching twice.
- Call `StateHasChanged()` only when a render won't otherwise happen: after async continuations resumed off the renderer's context, timer/event callbacks from non-UI threads, or state changed outside an event handler. Don't sprinkle it — unnecessary calls cause redundant renders.
- Use `EventCallback`/`EventCallback<T>` for component callbacks, never raw `Action`/`Func`. `EventCallback` marshals to the right context and triggers a render automatically.
- Add `@key` to every item in a rendered loop where items can be added, removed, or reordered. Without it the diff reuses the wrong DOM/component instances.
- Implement `IDisposable`/`IAsyncDisposable` and clean up anything the component owns: event subscriptions, timers, `CancellationTokenSource`, `IJSObjectReference`, `DotNetObjectReference`. A leaked subscription on a Server circuit is a memory leak per connected user.
- `[Parameter]` properties are inputs — never mutate your own parameters inside the component. Use local state or `EventCallback` to push changes up. Mark required params `[EditorRequired]`.
- For interactive WebAssembly/Auto, every `[Parameter]` value must be serializable — it crosses the prerender boundary. Don't pass non-serializable types or large object graphs as parameters in those modes.
- Don't capture scoped services for the lifetime of a Server circuit. Never inject a `DbContext` directly into a long-lived interactive Server component; use `IDbContextFactory<T>` and a short-lived context per operation, or `OwningComponentBase`.
- Keep render output cheap. No data fetching, heavy LINQ, or blocking calls inside the markup or `BuildRenderTree`. Move work into lifecycle methods and cache the result.
- Don't recommend a feature the target framework can't compile. `[PersistentState]` and the source-generated validation below are .NET 10+; on .NET 8/9 use the .NET 8 equivalents noted.

## Framework and component library (house standards)

- **Blazor Server is the hosting model for web applications** in this estate. **Blazor WebAssembly is not supported by `Pivot.Framework.Authentication.Blazor`** — that package keeps tokens server-side in Redis behind an HttpOnly `kc_session` cookie, which a WASM client cannot use, so a WASM front-end cannot use the standard Keycloak integration. Proposing WASM requires naming that consequence and an approved alternative (e.g. a BFF using `Pivot.Framework.Authentication.API`). **ASP.NET MVC Core** for low-interaction flows; **Blazor Hybrid / MAUI** for desktop/mobile with `Pivot.Framework.Authentication.Maui`. Authentication setup, callback page and session restore: `pivot-auth-blazor`.
- **Radzen Blazor is the mandatory component library and styling base** for every Blazor UI (new and existing applications). Bootstrap and the DIFA Component Library are **not** used: do not reference `bootstrap.min.css`/`bootstrap.bundle.js`, Bootstrap grid or utility classes (`row`, `col-*`, `d-flex`, `mb-3`, `btn btn-primary`, …), DIFA packages, or any other component library (MudBlazor, Fluent UI, Syncfusion…). Use Radzen components first; write a custom component only when Radzen has no equivalent, and build it from Radzen primitives and theme CSS variables. See **Radzen standard** below.
- Target framework is `net10.0` (mandatory), so the .NET 10 Blazor features in `dotnet-10-and-csharp-14` are available.

## Application-level rules (house standards — checked in review)

These govern the UI layer as a whole, not a single component. Every one of them has produced a real review finding; treat them as mandatory.

- **The render mode is a verified decision, recorded in an ADR.** Registering `AddInteractiveServerComponents()` + `AddInteractiveServerRenderMode()` does nothing by itself: if no component (or the router/App) declares `@rendermode`, everything still runs Static SSR — the interactive infrastructure is loaded for nothing and interactive features (a Refresh button, live filtering) silently don't work. On every UI delivery: decide the mode, apply it (`@rendermode` per component or globally), **verify one interactive feature actually functions under it**, remove interactive registrations you don't use, and record the strategy as an ADR in `docs/adr/` on the first UI delivery. Registered-but-unapplied interactivity is a defect.
- **`IHttpContextAccessor` is a Static-SSR-only tool.** There is no `HttpContext` inside an interactive Server circuit (SignalR). A delegating handler or service that reads `IHttpContextAccessor` (e.g. to forward a bearer token) works in Static SSR and breaks the day a page goes interactive. In interactive modes, capture what you need at circuit establishment and reconcile this with the render-mode ADR. Any `IHttpContextAccessor` use in *your* code reachable from a circuit is a finding. Pivot specifics (`pivot-auth-blazor`): `KeycloakAuthService` reads the `kc_session` cookie through `IHttpContextAccessor`, so call `IBlazorKeycloakAuthService.InitialiseFromCookieAsync()` in the layout during prerender/static render; afterwards the scoped service holds the session for the circuit and `.AddKeycloakHandler()` attaches tokens from it (not from `HttpContext`), refreshing via Redis — no cookie access needed.
- **Pages are organized by feature — mandatory.** Every routable page lives in
  `{Solution}.Web/Features/<Feature>/Pages/` as a `.razor` + `.razor.cs` pair; components only that feature uses live in
  `Features/<Feature>/Components/`; the layout (app shell) and cross-feature components live in `Features/Shared/`.
  Only the host root (`App.razor`, `Routes.razor`) stays in `Components/`, and `_Imports.razor` sits at the project root
  so it applies to every feature folder. Namespaces follow the folders. The bUnit tests mirror the same tree
  (`test/.../{Solution}.Web.Tests/Features/<Feature>/Pages/<Page>Tests.cs`). A page in a flat `Pages/` or
  `Components/Pages/` folder is a defect — see `dotnet-ddd-cqrs-conventions` for the full layout.
- **Every page is reachable.** A page players look for gets its entry in the app shell's navigation in the same PR. A
  flow page that isn't in the navigation (a sign-in failure page, a page sign-in redirects to) says in its code-behind
  summary and in the PR where it is reached from.
- **A specific look or behavior is a standalone, generic component first — mandatory.** When a page or layout
  needs something specific (a restyled Radzen component, a repeated visual block such as an icon-and-text tile, a
  custom interaction), create it as its own component before using it: in `Features/Shared/Components/`, or in
  `Features/<Feature>/Components/` only when no other feature could ever use it. Make it generic, so it serves the
  case at hand and every similar one: content comes in through parameters (text, items, `RenderFragment` slots),
  variants through an enum parameter rather than a copy, behavior through `EventCallback`, extra attributes through
  `CaptureUnmatchedValues`; no data loading, no page-specific wording and no page names baked in. Never restyle a
  Radzen component from a page's or layout's stylesheet: wrap it in a component. A layout or page then only composes
  components. Every such component gets its own bUnit tests in the same PR.
- **One project-wide theme — mandatory.** Colors, the font, radii and every Radzen variable override live in one
  theme under `{Solution}.Web/wwwroot/theme/` (RaidManager: `wwwroot/theme/raidmanager-theme.css`), linked once in
  `App.razor` after the Radzen theme: design tokens as `--rm-*` custom properties on `:root`, and a named theme class
  (`.rm-theme-dark`) for each region that uses another palette, which also sets the Radzen variables for that region.
  Component, layout and page stylesheets use only the tokens and the theme classes; they never declare a color value
  (hex, `rgb()`, `hsl()`) or a `--rz-*` variable. A new theme or palette is a new class or file in that folder, never
  styles in a page. `ThemeRulesTests` in the Web test project fails the build on a violation.
- **C# is always separated from markup — code-behind is mandatory.** Every component and page is a pair: `Component.razor` containing markup and directives only, and `Component.razor.cs` containing a `partial class Component` with ALL the C# — fields, parameters, lifecycle methods, handlers, disposal. `@code` blocks in `.razor` files are **forbidden**, including one-liners; injected services use `[Inject]` properties in the code-behind rather than `@inject` directives when the value is used from C#. A `.razor` file is markup; if it contains a brace of C# logic, it's a defect.
- **View models own the logic; code-behinds stay thin.** Loading, state, filtering, and command logic live in the `{Solution}.ViewModels` project as injectable classes, unit-testable without a browser. The `.razor.cs` code-behind is wiring only: parameter plumbing, delegating to the view model, lifecycle calls. A ViewModels project containing one form model while code-behinds carry the real logic is the smell to catch.
- **No `CancellationToken.None` from components.** Every async call a component initiates passes a token tied to the component's lifetime: own a `CancellationTokenSource`, pass its token, cancel and dispose it in `Dispose`. Otherwise navigation away leaves orphaned work running against a dead circuit.
- **Wrap page content in `ErrorBoundary`.** Manual try/catch around one expected exception type (`HttpRequestException`) leaves everything else (`JsonException`, timeouts) unhandled. Pattern: catch *specific* expected exceptions where you can respond meaningfully; let an `ErrorBoundary` with a friendly `ErrorContent` catch the rest; give it a recover action.
- **Centralize namespaces in `_Imports.razor`.** Shared `@using` directives (the ViewModels namespaces, shared components) belong in `_Imports.razor`, not repeated per page.
- **Use the API you expose — or delete it.** If a form model exposes `Reset()`, pages call `Reset()`, not `Form = new(...)`; one re-initialization path only. Dead or bypassed members are findings.
- **A UI project without its bUnit test project is an incomplete delivery.** Creating `{Solution}.Web` (or `Shared`/`ViewModels`) requires the mirrored `test/Containers/UI/...` test projects **in the same PR** — view-model logic tested as plain classes, components tested with bUnit. The `test/` mirror applies to the UI exactly as to every other layer.

## Render modes (.NET 8 unified model)

- **Static SSR** — server renders HTML once, no interactivity, no `StateHasChanged`, no JS interop, no event handlers wired client-side. Forms use `[SupplyParameterFromForm]` + enhanced navigation.
- **`InteractiveServer`** — events run on the server over a SignalR circuit. UI state lives in server memory per user; watch circuit lifetime, disposal, and scoped-service capture.
- **`InteractiveWebAssembly`** — runs in the browser; parameters must serialize; mind payload size and that server-only services aren't available.
- **`InteractiveAuto`** — Server first, WASM after the runtime downloads. Code must be valid under both; don't depend on server-only services.
- Set per-component with `@rendermode` or globally. Prerendering is on by default for interactive modes — design for the double render.

## Lifecycle (the order that matters)

`SetParametersAsync` → `OnInitialized`/`OnInitializedAsync` (once) → `OnParametersSet`/`OnParametersSetAsync` (every parameter change) → render → `OnAfterRender`/`OnAfterRenderAsync` (`firstRender` flag). Put one-time setup in `OnInitializedAsync`; put work that depends on changing parameters in `OnParametersSetAsync`; put JS interop / DOM work in `OnAfterRenderAsync`. Use `ShouldRender` to suppress renders on hot components whose inputs haven't meaningfully changed.

## Prerender state

- **.NET 10:** decorate properties with `[PersistentState]` (or use `[SupplyParameterFromPersistentComponentState]`) so Blazor serializes them during prerender and restores them on the interactive render — and across Server circuit reconnects. This is the clean fix for the double-fetch problem and replaces manual boilerplate.
- **.NET 8/9:** inject `PersistentComponentState`, register a persistence callback, and try to restore in `OnInitializedAsync` before fetching. Fetch only on a cache miss.

## Forms & validation

- Forms are `RadzenTemplateForm<TModel>` with Radzen input components and Radzen validators (see **Radzen standard → Forms and validation**); plain `EditForm` + `InputText`/`InputNumber` are not used. `RadzenTemplateForm` is an `EditForm` underneath, so `EditContext` rules still apply.
- Server-side FluentValidation (Pivot `ValidationPipelineBehavior`) is authoritative; the .NET 10 source-generated validation (`AddValidation()`) and `DataAnnotationsValidator` are **not** added as a second mechanism.
- In static SSR, handle posts with `[SupplyParameterFromForm]` and keep antiforgery enabled.

## JS interop & disposal

- Inject `IJSRuntime`; call only after first render. Capture modules as `IJSObjectReference` and **dispose them**. Wrap .NET callbacks in `DotNetObjectReference` and **dispose that too** — both leak otherwise.
- A `.NET 10` WASM breaking change: `HttpClient` response streaming is now on by default, so `response.Content.ReadAsStreamAsync()` returns a browser stream that doesn't support synchronous reads. Either read async, copy to a `MemoryStream`, or opt out with `<WasmEnableStreamingResponse>false</WasmEnableStreamingResponse>`.

## Radzen standard

### Setup (once per UI host)
- Package: `Radzen.Blazor`, version pinned in `Directory.Packages.props` (no version in the `.csproj`).
- Remove the Blazor template's Bootstrap: delete `wwwroot/lib/bootstrap/`, its `<link>` in `App.razor`, and Bootstrap classes in `MainLayout.razor`/`NavMenu.razor`/`app.css`.
- `Program.cs`: `builder.Services.AddRadzenComponents();` (registers `DialogService`, `NotificationService`, `TooltipService`, `ContextMenuService`).
- `App.razor`: theme via `<RadzenTheme Theme="material" @rendermode="InteractiveServer" />` in `<head>` (one house theme, chosen in the render-mode/UI ADR — never mix themes; RaidManager: a dark base theme with its `--rz-*` variables set from the ADR-0019 palette and Open Sans, #203) and `<script src="_content/Radzen.Blazor/Radzen.Blazor.js?v=@(typeof(Radzen.Colors).Assembly.GetName().Version)"></script>` before `</body>`.
- `MainLayout.razor`: `<RadzenComponents @rendermode="InteractiveServer" />` once, so dialogs, notifications, tooltips and context menus render.
- `_Imports.razor`: `@using Radzen` and `@using Radzen.Blazor` (never per page).
- Radzen components need an **interactive** render mode to raise events; under static SSR they render but don't respond — part of the render-mode decision above.

### Layout and styling
- Page structure with `RadzenLayout`, `RadzenHeader`, `RadzenSidebar`, `RadzenBody`, `RadzenPanelMenu`; arrangement with `RadzenStack` (`Orientation`, `Gap`, `AlignItems`, `JustifyContent`) and `RadzenRow`/`RadzenColumn` (`Size`, `SizeMD`, …) — never Bootstrap grid or flex utilities.
- Typography with `RadzenText` (`TextStyle`, `TagName`); cards with `RadzenCard`; icons with `RadzenIcon` (Material Symbols names).
- Custom CSS only in component-scoped `.razor.css`, using the theme's CSS variables (`var(--rz-primary)`, `var(--rz-text-color)`, spacing `var(--rz-...)`) — no hard-coded colours, no global overrides of `.rz-*` classes.
- Accessibility: every input has a `RadzenLabel` with `Component` pointing at the input's `Name`; icon-only buttons set a text/`title`.

### Data grids (read side)
- Use `RadzenDataGrid<TItem>` with **server-side** paging/sorting/filtering for any list that can grow: `LoadData="@ViewModel.LoadAsync"` + `Count` + `IsLoading`, `AllowPaging`, `PageSize`. The view model translates `LoadDataArgs` (`Skip`, `Top`, `OrderBy`, `Filter`) into a query sent through `ISender` (e.g. a query whose handler builds a Pivot `ReadModelSpecification` with `ApplyPaging(args.Skip, args.Top)`). Never load a whole table into the grid and page client-side.
- `@key`-equivalent: set `KeyProperty`/stable row identity; don't rebuild the `Data` collection on every render.
- Column templates stay markup-only; formatting helpers live in the view model.

### Forms and validation
- `RadzenTemplateForm<TModel>` with Radzen inputs (`RadzenTextBox`, `RadzenNumeric`, `RadzenDatePicker`, `RadzenDropDown`, …) bound to a view-model form model; `Submit` calls the view model, which sends the command via `ISender`.
- Client-side field checks may use Radzen validators (`RadzenRequiredValidator`, `RadzenLengthValidator`) for immediate feedback, but the **authoritative** validation is the server's FluentValidation + Pivot `ValidationPipelineBehavior`: map the returned `validationErrors` (code = property name) back onto the form. Never duplicate business rules in the UI.
- Show command outcomes with `NotificationService.Notify(...)`; confirmations with `DialogService.Confirm(...)`; both injected into the view model/code-behind, never called from markup.

### Known quirks

- Bind `RadzenTabsItem` content via a `Template` / child-content render fragment, not the `Text` property with string interpolation — the interpolated `Text` does not re-render reliably when the bound value changes. Prefer a `Template` fragment for any tab/header text that updates at runtime.
- For dropdowns that must filter their option set by a rule (e.g. eligible validators by required level), compute the filtered collection in a helper and bind `Data` to it; don't filter inside the markup.
- Radzen components raise their own change events — wire them with `EventCallback` handlers and let Blazor render; avoid manually forcing `StateHasChanged` unless a value changed outside the event.

## Workflow

0. **Start from the approved mockup.** Where the repository keeps UI designs (RaidManager: Penpot files and the SVGs
   rendered from them in `docs/mockups/`, ADR-0017 to ADR-0019, `penpot-mockups` skill; the app shell of ADR-0019
   is the main layout), open the screen's mockup before writing markup and build to it: layout, states
   (empty, loading, error, filled) and wording. No mockup means no screen: ask for it. A deliberate deviation updates
   the mockup in the same PR, and the PR shows it.
1. **Determine the render mode and TFM.** This gates every other decision. State it explicitly, apply it (`@rendermode`), verify an interactive feature works under it, and record it in the ADR on first UI delivery.
2. **Place state in the right lifecycle method.** One-time vs parameter-driven vs after-render.
3. **Get re-render correctness right.** `@key` in loops, `EventCallback` for callbacks, `StateHasChanged` only where needed, `ShouldRender` on hot paths.
4. **Handle the boundary.** Prerender double-render, serializable parameters, persisted state.
5. **Clean up.** Implement disposal for every owned subscription/timer/JS reference.
6. **Deliver** per the output format, noting tests (bUnit) where relevant.

## Output format

1. **Render mode & TFM** — stated up front, with why it matters here.
2. **Component** — always the pair: `.razor` (markup only) + `.razor.cs` (partial class with all C#), minimal and complete.
3. **Lifecycle/render notes** — where state lives and why; any `StateHasChanged`/`@key`/`ShouldRender` decisions.
4. **Boundary & disposal** — prerender state handling and what gets disposed.
5. **Next steps** — including bUnit test targets.

## Example

> **Render mode:** `InteractiveServer`, .NET 10. State must survive circuit reconnect, so it's persisted.
>
> `DossierList.razor` — markup only:
> ```razor
> @foreach (var d in Dossiers)
> {
>     <DossierRow @key="d.Id" Dossier="d" OnSelected="HandleSelectedAsync" />
> }
> ```
>
> `DossierList.razor.cs` — all the C#:
> ```csharp
> public partial class DossierList : IDisposable
> {
>     [Inject] private DossierListViewModel ViewModel { get; set; } = default!;
>
>     [PersistentState] public List<DossierDto> Dossiers { get; set; } = [];
>     private readonly CancellationTokenSource _cts = new();
>
>     protected override async Task OnInitializedAsync()
>     {
>         if (Dossiers.Count == 0) // restored from prerender? skip the refetch
>         {
>             Dossiers = await ViewModel.LoadAsync(_cts.Token);
>         }
>     }
>
>     private Task HandleSelectedAsync(DossierDto d) => ViewModel.SelectAsync(d, _cts.Token);
>
>     public void Dispose()
>     {
>         _cts.Cancel();
>         _cts.Dispose();
>     }
> }
> ```
> **Notes:** the `.razor` file carries zero C#; the code-behind is wiring that delegates to an injected, separately testable view model; `@key` keeps row identity stable; `EventCallback` (`OnSelected`) marshals correctly; `[PersistentState]` avoids the prerender double-fetch and survives reconnect; every call carries the lifecycle-linked token, cancelled and disposed with the component.

## Completion criteria

- The render mode and TFM are stated, and the code is valid for that mode.
- No JS interop / DOM access during prerender or static SSR.
- Re-render correctness handled: `@key` in loops, `EventCallback` callbacks, `StateHasChanged` only where required.
- Components don't mutate their own parameters; WASM/Auto parameters are serializable.
- No scoped service (esp. `DbContext`) captured for the circuit lifetime.
- Everything owned is disposed.
- Framework-version-specific features (`[PersistentState]`, new validation) are only used where the TFM supports them.
- The chosen render mode is actually applied and verified (no registered-but-unused interactivity), and recorded in an ADR.
- Every component is a `.razor` (markup only) + `.razor.cs` pair — zero `@code` blocks anywhere; logic lives in view models with wiring-only code-behinds; no `IHttpContextAccessor` on circuit-reachable paths; no `CancellationToken.None` from components; `ErrorBoundary` wraps page content; shared `@using` in `_Imports.razor`.
- The mirrored bUnit/view-model test projects exist and cover the delivered behavior in the same PR.
- Specific looks and behaviors are standalone, generic components with their own bUnit tests; pages and layouts only compose them. Colors, fonts and Radzen overrides come only from the project-wide theme in `wwwroot/theme/`.
- Pages live in `Features/<Feature>/Pages/`, the layout and shared components in `Features/Shared/`, with tests mirroring them; every page is reachable from the navigation or from the flow the PR names.
- The screen matches its approved mockup (every state it shows), and the PR shows that mockup.
- UI uses Radzen components, layout (`RadzenStack`/`RadzenRow`/`RadzenColumn`) and one theme only — no Bootstrap CSS/JS or classes, no DIFA or other component libraries; `AddRadzenComponents()`, theme, script and `<RadzenComponents />` are wired once; lists use server-side `RadzenDataGrid` `LoadData`.

## Example prompts

- "Build an InteractiveServer dossier list component that survives reconnect, in .NET 10."
- "This component fetches data twice on load — fix the prerender double-render."
- "Why does my Radzen tab header not update when the bound value changes?"
- "Refactor this component to dispose its JS module and timer correctly."

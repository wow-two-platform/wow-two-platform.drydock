# Claude UI directions

*Last updated: 2026-09-27*

Parked alternatives to the current [Studio workspace](../../architecture/architecture.md#workspace). The user kept the
Studio workspace on 2026-09-27; these boards return when the UI is revisited.

## Status

- [x] Six directions drawn over the local rig's real data (2026-09-26).
- [x] User shortlisted Glass Harbor, Frost and Bento Deck.
- [x] Harbor Frost merge drawn: Frost's top navigation and big numbers over Glass Harbor's dashboard.
- [x] Shortlisted palettes saved as SDK themes.
- [ ] Parked until the UI is revisited.

## Boards

Each file is a standalone 1440×900 page; open it in a browser. Fonts load from Google Fonts.

| # | Board | File | Layout | Palette | Type |
|---|---|---|---|---|---|
| 1 | Glass Harbor | [glass-harbor.html](glass-harbor.html) | Floating glass rail, KPI row, host/targets split, deployments table | Deep navy, cyan brand, amber lights | Outfit · Manrope |
| 2 | Frost | [frost.html](frost.html) | Glass top navigation, heavy numbers, ring gauges | Pastel blue-lavender-peach wash, deep teal | Plus Jakarta Sans |
| 3 | Aurora Mono | [aurora-mono.html](aurora-mono.html) | Minimal dark shell with a command bar | Near-black, off-white text | Geist · Geist Mono |
| 4 | Bento Deck | [bento-deck.html](bento-deck.html) | Icon rail, bento grid, ink hero tile, pastel KPI tiles | Warm paper, ink, pastels | Bricolage Grotesque · DM Sans |
| 5 | Mission Control | [mission-control.html](mission-control.html) | Dense operations console | Near-black blue-grey, pale steel text | JetBrains Mono · Space Grotesk |
| 6 | Nautical Chart | [nautical-chart.html](nautical-chart.html) | Brand-led editorial overview | Chart paper, navy ink | Fraunces · Source Sans 3 |
| 7 | Harbor Frost, light | [harbor-frost-light.html](harbor-frost-light.html) | Frost top navigation and big numbers, Glass Harbor dashboard | Frost | Plus Jakarta Sans |
| 8 | Harbor Frost, dark | [harbor-frost-dark.html](harbor-frost-dark.html) | Same layout | Glass Harbor | Plus Jakarta Sans |

The Harbor Frost moon and sun buttons link the two modes. The editable canvas with all eight boards is
private to the owner: https://claude.ai/artifact/U7QyaqaKQkhJajZ1E3CUqp.

## Harbor Frost merge

| Part | Source |
|---|---|
| Glass top bar, segmented navigation, environment pill, avatar | Frost |
| Heavy 44px numbers, ring gauges with centered values | Frost |
| KPI row, host/targets split, full-width deployments table | Glass Harbor |
| Glass panels over a colored glow, cyan brand, amber highlights | Glass Harbor |
| Light palette | Frost |
| Dark palette | Glass Harbor |

A top bar replaces the sidebar, so the rail toggle (Ctrl/⌘ B) goes away. Five navigation items fit to
roughly 1000px; narrower screens need a menu button.

## SDK themes

`@wow-two-beta/ui` (React) carries the shortlisted palettes in `src/foundation/themes/authored.ts`:
`glass-harbor`, `frost`, `bento-deck` and `harbor-frost` (Frost light with Glass Harbor dark). They are
AA-proven candidates with the large radius knob. The glass themes add an `ambient` backdrop, emitted as
`--theme-ambient` and painted by the `surface-ambient` class. `@wow-two-beta/ui-vue`, which Wheelhouse
now uses, does not carry them yet.

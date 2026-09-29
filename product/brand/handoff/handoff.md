# WoW2 logo patterns — analysis and continuation brief

*Last updated: 2026-09-29*

> Recommendation: four composition families for independently branded platform apps — three core forms and one optional endorsement.
> This is a saved proposal and handoff, not an adopted workspace convention.

## Status

- [x] Wheelhouse Soft folds selected by the user
- [x] default compact wordmark, gray lower fold, transparent fold gaps and no flag recorded
- [x] ten transparent Wheelhouse PNGs saved; original export commit `a56644e`
- [x] composition analysis and continuation instructions saved here
- [ ] adopt the shared rules into workspace conventions in the separate chat
- [ ] design and approve the canonical WoW2 identity
- [ ] compose Wheelhouse endorsements using that exact approved WoW2 artwork
- [ ] verify small-size variants and create editable vector masters
- [ ] integrate the selected assets into the application when that work is requested

---

## Scope and ownership

The user fixed the family scope to WoW2 platform applications, SDKs and foundational tools.
Consumer ventures, including ForeverPin, are outside this family convention.

| Entity | Recommended identity scope |
|---|---|
| Independently used platform app | Own symbol and wordmark; the four compositions below |
| SDK family | Own family symbol and wordmark; tile and endorsement only where used |
| Individual SDK package | Family artwork plus the package name in normal text; no automatic new logo |
| Version, beta channel or environment | Text label or UI badge; no separate brand identity |
| Backbone / foundational tooling | Own identity only if it is independently named and encountered; classify its actual role first |
| WoW2 parent brand | Canonical parent wordmark and symbol; an avatar/tile where needed; no self-endorsement |

“Backbone” in the earlier discussion does not establish a new standalone application.
Identity belongs to the user-facing product or family, not automatically to each repository or service.

---

## Four composition families

Here, a **composition** means an arrangement of the approved symbol, lettering and optional parent endorsement.
Its colors, export sizes and file formats are separate choices.

| Pattern | Contents | Main use cases | Requirement |
|---|---|---|---|
| Primary horizontal | Product symbol plus name, or an integrated symbol/initial | App header, documentation header, repository introduction, product page | Default |
| Standalone symbol | Product symbol only | Compact navigation, product switcher, topology diagrams, favicon source | Core; pair with a nearby name or accessible label when recognition is uncertain |
| App tile | Symbol on its approved square carrier/background | Launcher, installation surface, avatar, square app card | Core for apps with those surfaces; conditional for SDK families |
| Endorsed signature | Primary identity plus a subordinate `by` and exact WoW2 logo | Ownership context in introductions, presentations, selected documentation or external material | Optional usage; prepared as a controlled library composition |

The ordinary product UI uses the primary or standalone form. A WoW2 endorsement is useful when the parent relationship is otherwise unclear.
This is consistent with [Atlassian's distinction between product and attribution logos](https://atlassian.design/foundations/logos).
The four-family count itself is our recommendation for WoW2, not a number prescribed by that source.

### Responsive selection

| Situation | Preferred response |
|---|---|
| Enough horizontal space | Primary horizontal composition |
| Header becomes narrow | Standalone symbol with an accessible product name |
| Square identity slot | Approved app tile or platform-specific derivative |
| Product and WoW2 ownership need introduction together | Endorsed composition at a size where both remain readable |
| Endorsement becomes illegible | Use the ordinary product identity; place ownership information elsewhere |
| Tall or nearly square presentation area | A stacked composition only if the horizontal and symbol forms cannot serve it |

The tile is a carrier for the same symbol. It does not require a second symbol design.
Native installation assets may have platform-specific masks, opacity and packaging requirements;
the current transparent-outside green tile is a general brand export, not a claim of native-store readiness.

---

## Conditional derivatives

- **stacked composition:** symbol above name, introduced only for a demonstrated layout need
- **micro symbol:** optical adjustment of the existing symbol if its normal geometry fails at small sizes
- **true one-ink version:** only when a print, engraving or other constrained surface needs it
- **platform icon bundle:** sizes and file packaging required by an actual consumer
- **social/share artwork:** a layout that places an approved logo; not another logo identity

Micro work can widen seams or simplify an unresolvable detail while retaining recognition.
Any change to Wheelhouse's approved gray fold or boat silhouette needs a visual review;
it should not disappear as an incidental resize or export operation.

Start small-size review at 16, 20, 24 and 32 CSS pixels, including a square favicon slot.
These are review targets, not claimed minimum sizes for the current artwork.
The present broad boat is roughly twice as wide as it is tall, so square slots require particular attention.

---

## Color variants and asset count

| Set | Calculation | PNG count before platform-specific sizes |
|---|---|---|
| Core app set | Primary × 2 surface colors + symbol × 2 + tile × 1 | 5 |
| Core plus endorsement | Core 5 + endorsed × 2 | 7 |
| Wheelhouse's current expanded set | Primary, symbol and endorsed × black/white/mint + green tile | 10 |

An approved accent color is useful when it serves a real surface; it is not required for every product.
Wheelhouse's black, white and mint variants retain a gray facet, so they are not true one-ink versions.
Themes, resolutions, PNG/SVG/ICO formats and `@2x` outputs do not increase the composition count.

### Asset master and export proposal

- editable vector sources become the canonical geometry when they have been drawn and visually approved
- a PNG embedded inside an SVG is still a raster asset, not a vector master
- outlined production lettering avoids dependence on installed fonts; keep the editable lettering source separately
- current native-resolution PNGs remain usable exports and visual references
- transparent artwork keeps counters and fold gaps transparent, including on dark surfaces
- logo clear space is a layout rule; the current eight-pixel export padding is only an antialiasing safety margin
- product usage documentation records compositions, palette, clear space, tested minimum sizes and allowed backgrounds
- raster sizes follow actual target surfaces; upscaling an existing PNG does not create additional detail

---

## Shared family design philosophy

The shared pattern should be recognizable construction discipline, not the same object repeated across products.

| Shared across the family | Product-specific |
|---|---|
| Bold, readable visual weight | Distinct silhouette and product metaphor |
| Controlled corner treatment and deliberate negative space | Approved brand accent |
| Consistent wordmark hierarchy and endorsement placement | Lettering adaptations justified by the product name |
| Consistent optical sizing in product lists | Product-specific inner geometry |
| Reusable tile, spacing and export rules | Product symbol within that carrier |

Paper boats, nautical symbols, folded-paper geometry and W initials are Wheelhouse choices.
They do not become mandatory for Secrets Vault, SDK families or the WoW2 parent logo.
Color alone should not carry the difference between two product identities.

---

## Wheelhouse decisions to preserve

The [brand asset guide](../brand.md) remains the owner of the current file inventory and dimensions.

- selected direction: **01 / Soft folds**, from the upper row of the saved comparison
- recognizable childhood paper boat; restrained rounding and straight fold directions
- boat replaces the initial W; the following lettering is `heelhouse`, avoiding a duplicate W
- gray lower triangular facet helps reveal the W silhouette
- white-looking seams on white surfaces are transparent gaps in the exports
- no flag or mast
- compact wordmark is the default, without an endorsement
- `by wow2` is optional; current endorsed files contain cropped ordinary text, not a canonical WoW2 logo
- `powered by WoW2` is reserved for a meaningful technology-dependency context, not the default authorship signature
- green tile connects the new mark to the previous Wheelhouse icon
- black/gray, white/gray and mint/gray exports share identical alpha geometry
- raster extraction, native dimensions and application integration are documented separately from design approval

The user's request to crop and remove the background authorized direct image processing.
The user subsequently explicitly approved a pixel-preserving script when automatic image extraction introduced artifacts.
The saved final exports use the approved raster artwork; failed automatic extraction outputs are excluded.

---

## Exact WoW2 endorsement workflow

1. Design the WoW2 parent identity independently, using the family discipline without forcing Wheelhouse's boat metaphor.
2. Approve and save canonical WoW2 artwork, including intended light/dark versions and clear-space rules.
3. Use a neutral `by` label followed by the **actual approved WoW2 asset** beneath Wheelhouse's lettering.
4. Keep parent branding visually subordinate; leave the ordinary compact Wheelhouse wordmark unchanged.
5. Compose the supplied assets deterministically in vector or image-editing software. Do not ask an image generator to redraw both brands together.
6. Use the same approved WoW2 source for every product endorsement; do not approximate it with a similar font.
7. Verify aspect ratio, alignment, readable size, clear space, transparency and light/dark contrast.
8. Update the compositor's inputs to the canonical WoW2 artwork, so a later replay cannot restore the old text endorsement.
9. Save new endorsed exports and update the guide/preview. Record which WoW2 master produced them.

The current `by wow2` text exports are a layout reference until that canonical artwork exists.
No WoW2 badge or endorsement is planned inside the tiny boat or app tile.

---

## Saved work and reproducibility

- [Current asset guide and preview](../brand.md)
- [Ten transparent PNGs](../soft-folds/)
- [Selected source row](../source/soft-folds-selection.png)
- [Existing attribution lettering](../source/by-wow2-attribution.png)
- [Previous Wheelhouse icon](../source/legacy-wheelhouse-icon.png)
- [Exploration history](../exploration/exploration.md), including the complete final comparison
- [Portable export script](../../../engineering/scripts/export-brand-logos.py), reading the saved source images

The initial export set is committed in the independent Wheelhouse repository as `a56644e`.
Current code still uses its existing ship icon; saving this brand kit did not integrate it into the application.
Earlier rejected boards remain historical references, not alternate approved production logos.

The exporter requires Python with Pillow and NumPy. From the Wheelhouse repository, an isolated replay is:

```sh
python3 engineering/scripts/export-brand-logos.py --output-dir /tmp/wheelhouse-brand-replay
```

Omitting `--output-dir` regenerates the current logo PNGs and preview in `product/brand/`.
Sources are read-only inputs. The current replay still uses the textual attribution source;
its composition logic must be updated when the canonical WoW2 asset is adopted.

---

## Separate-chat continuation

The workspace is `/Users/max/Projects/10x-ws/workbench/career/engineering/wow-two/wow-two-ws`.
The independent product repository is `workbench/wow-two-platform/wow-two-platform.wheelhouse`.

Suggested continuation brief:

> Continue the WoW2 identity work from `workbench/wow-two-platform/wow-two-platform.wheelhouse/product/brand/handoff/handoff.md`.
> Keep the scope to WoW2 platform apps, SDKs and foundational tools; exclude consumer ventures.
> Turn the reviewed pattern analysis into a shared convention using the existing convention indexes and ownership rules.
> Design and approve a canonical WoW2 logo, then compose Wheelhouse's optional endorsements using that exact asset.
> Preserve the approved Soft folds boat, gray facet, fold gaps, bold lettering and flag-free default wordmark.
> Use the saved sources and export process; do not regenerate the chosen Wheelhouse identity while adding the parent logo.

### Convention adoption boundaries

- the shared construction, composition and endorsement rules belong in a single convention owner
- a design identity leaf under `conventions/design/` is a proposed home; confirm the nearest existing owner before creating it
- update the root and design indexes, and reconcile the marketing index's visual-identity pointer rather than duplicating rules
- product geometry, exact color choices and asset filenames stay in each product's brand guide
- shared family rules here remain recommendations until the separate chat adopts them
- parent identity design and exact parent-asset composition remain unfinished follow-up work

---

## References

- [Atlassian logo guidance](https://atlassian.design/foundations/logos), checked 2026-09-29:
  separates symbols, wordmarks, attribution and color variants; attribution is contextual and approved artwork is reused.
- workspace `conventions/design/design-conventions.md` and `conventions/design/research/design-exploration.md`:
  separate the shared design method from product-specific decisions and save approved choices.
- workspace `conventions/marketing/marketing-conventions.md`:
  currently points visual identity toward frontend styling; reconcile that pointer when adopting a dedicated identity convention.

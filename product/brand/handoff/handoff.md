# WoW2 identity adoption — Wheelhouse handoff

*Last updated: 2026-09-29*

> Wheelhouse's approved Soft folds identity uses the exact approved WoW2 Panels artwork for optional endorsement.

## Status

- [x] Wheelhouse Soft folds selected; compact lettering, gray fold, transparent gaps and no flag preserved
- [x] shared composition rules adopted in the [logo system](../../../../../../conventions/design/identity/logo-system.md)
- [x] canonical WoW2 Panels shape selected; [parent guide](../../../../../../docs/brand/wow2/brand.md) owns its artwork
- [x] Wheelhouse endorsements composed from hash-verified copies of the approved parent primary assets
- [x] approved final set saved: ten transparent PNGs, two review sheets and [complete manifest](../manifest.json)
- [x] seven unendorsed exports preserved byte-for-byte
- [x] review symbol, primary, endorsed and tile sizes; [brand guide](../brand.md#size-review) records minimums and failed targets
- [ ] create editable vector masters through a separate visual review
- [ ] integrate the selected assets into the application when requested

---

## Ownership

- shared family scope, composition, derivatives and endorsement rules belong to the [logo system](../../../../../../conventions/design/identity/logo-system.md)
- Wheelhouse geometry, palette, file inventory and placement values belong to the [brand guide](../brand.md)
- WoW2 geometry and approved parent exports belong to its [brand guide](../../../../../../docs/brand/wow2/brand.md)
- [vendored parent inputs](../source/wow2/provenance.json) keep this repository's exporter independent of workspace paths

---

## Wheelhouse decisions

- selected direction: **Soft folds**, from the upper row of the saved comparison
- rounded paper boat replaces the initial W; following lettering remains `heelhouse`
- gray lower facet and transparent fold gaps retain the approved W silhouette
- default compact wordmark, standalone mark and green tile remain unendorsed
- black/gray, white/gray and mint/gray Wheelhouse artwork retain identical alpha geometry
- optional endorsement uses the original neutral `by` letters plus the approved WoW2 Panels symbol and name
- navy parent artwork accompanies black Wheelhouse; white parent artwork accompanies white or mint Wheelhouse
- endorsed content remains subordinate beneath the first h; exact dimensions belong to the [brand guide](../brand.md)
- the approved raster source remains the geometry master; no editable vector master is claimed

---

## Parent provenance

| Input | Source | SHA-256 record |
|---|---|---|
| Navy primary | `wow-two-ws/docs/brand/wow2/panels/wow2-primary-navy.png` | [Provenance](../source/wow2/provenance.json) |
| White primary | `wow-two-ws/docs/brand/wow2/panels/wow2-primary-white.png` | [Provenance](../source/wow2/provenance.json) |
| Approved master | `wow-two-ws/docs/brand/wow2/source/panels-approved.png` | [Provenance](../source/wow2/provenance.json) |

- the two primary PNGs are copied byte-for-byte into `product/brand/source/wow2/`
- the compositor verifies each vendored file against its recorded SHA-256 before exporting
- source alpha supplies the tight crop; a proportional resize supplies the subordinate parent size
- `by` uses only the left crop of the historical attribution image; the old textual `wow2` is excluded
- neither parent symbol nor parent lettering is redrawn, retyped or recolored
- the original Wheelhouse source images remain read-only inputs

---

## Reproducibility

The [exporter](../../../engineering/scripts/export-brand-logos.py) requires Python, Pillow and NumPy.
From the Wheelhouse repository, an isolated replay is:

```sh
python3 engineering/scripts/export-brand-logos.py --output-dir /tmp/wheelhouse-brand-replay
```

Omitting `--output-dir` writes the current `product/brand/soft-folds/` PNGs, both review sheets and `manifest.json`.
The exporter reads only repository inputs; the deterministic manifest records dimensions, hashes, placement and parent provenance.

- all ten exports, both review sheets and the final manifest match isolated replay byte-for-byte
- default wordmarks, standalone marks and app tile match the pre-endorsement exports byte-for-byte
- endorsed product pixels preserve the corresponding default wordmark exactly
- light/dark review confirms transparent margins, gray fold retention and approved parent colors
- reviewed minimums: primary `24px` high, symbol `20px` square, endorsed `128px` high, tile `64px` square
- the initial Wheelhouse export commit remains `a56644e`; the exact-parent endorsement supersedes its text-only layout

---

## Remaining work

- apply the reviewed minimums and check the actual application backgrounds during integration
- keep any micro-symbol adjustment separate from these unchanged source exports
- review any future vector redraw against the approved raster before replacing the master
- keep application integration separate from this saved brand-kit update

Earlier rejected boards remain in the [exploration archive](../exploration/exploration.md).

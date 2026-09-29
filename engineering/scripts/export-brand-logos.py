#!/usr/bin/env python3
"""Replay the approved Soft folds exports from read-only repository source images.

Requires Pillow and NumPy. By default, writes product/brand/soft-folds/*.png
and the preview.png and size-review.png review sheets. Use --output-dir for an isolated replay.
Only review labels use Pillow's bundled font; exported logos retain the source lettering.
"""

import argparse
from collections import deque
import hashlib
import json
from pathlib import Path

import numpy as np
from PIL import Image, ImageDraw, ImageFilter, ImageFont


REPO = Path(__file__).resolve().parents[2]
BRAND = REPO / 'product/brand'
SOURCE = BRAND / 'source'


def flood(mask, seeds):
    seen = np.zeros(mask.shape, dtype=bool)
    queue = deque(seeds)
    h, w = mask.shape
    while queue:
        x, y = queue.popleft()
        if x < 0 or y < 0 or x >= w or y >= h or seen[y, x] or not mask[y, x]:
            continue
        seen[y, x] = True
        queue.extend(((x - 1, y), (x + 1, y), (x, y - 1), (x, y + 1)))
    return seen


def filter_mask(mask, minimum=12):
    pending = mask.copy()
    result = np.zeros(mask.shape, dtype=bool)
    components = []
    while pending.any():
        y, x = np.argwhere(pending)[0]
        part = flood(pending, [(int(x), int(y))])
        pending[part] = False
        if part.sum() >= minimum:
            result |= part
            ys, xs = np.where(part)
            components.append([int(xs.min()), int(ys.min()), int(xs.max() + 1), int(ys.max() + 1)])
    return result, components


def morph(mask, expand):
    im = Image.fromarray(np.uint8(mask) * 255)
    return np.asarray(im.filter(ImageFilter.MaxFilter(3) if expand else ImageFilter.MinFilter(3))) > 0


def remove_paper(image, tile=False):
    """Keep opaque source pixels; unmatte only the silhouette's antialiased edge."""
    rgb = np.asarray(image.convert('RGB'), dtype=np.float32)
    mask, parts = filter_mask(rgb.min(axis=2) < 225)
    if tile:
        h, w = mask.shape
        edge = [(x, y) for x in range(w) for y in (0, h - 1)]
        edge += [(x, y) for y in range(h) for x in (0, w - 1)]
        mask = ~flood(~mask, edge)
    core = morph(mask, False)
    band = morph(mask, True)
    nearest = rgb.copy()
    known = core.copy()
    for _ in range(8):
        sums = np.zeros_like(rgb)
        counts = np.zeros(mask.shape, dtype=np.float32)
        for dy, dx in ((-1, 0), (1, 0), (0, -1), (0, 1), (-1, -1), (-1, 1), (1, -1), (1, 1)):
            shifted = np.roll(known, (dy, dx), axis=(0, 1))
            if dy < 0:
                shifted[dy:, :] = False
            elif dy > 0:
                shifted[:dy, :] = False
            if dx < 0:
                shifted[:, dx:] = False
            elif dx > 0:
                shifted[:, :dx] = False
            sums += np.roll(nearest, (dy, dx), axis=(0, 1)) * shifted[..., None]
            counts += shifted
        available = (counts > 0) & ~known & band
        nearest[available] = sums[available] / counts[available, None]
        known |= available
    white = 253.0
    distance = white - nearest
    alpha = ((white - rgb) * distance).sum(axis=2) / np.maximum((distance * distance).sum(axis=2), 1)
    alpha = np.clip(alpha, 0, 1)
    alpha[~band] = 0
    alpha[alpha < 0.035] = 0
    alpha[core] = 1
    final_rgb = rgb.copy()
    final_rgb[~core] = nearest[~core]
    final_rgb[alpha == 0] = 0
    rgba = np.dstack((np.uint8(np.clip(final_rgb, 0, 255).round()), np.uint8((alpha * 255).round())))
    return Image.fromarray(rgba), parts


def trim(image, padding=8, square=False):
    box = image.getchannel('A').getbbox()
    assert box
    content = image.crop(box)
    w, h = content.size
    size = (max(w, h) + 2 * padding,) * 2 if square else (w + 2 * padding, h + 2 * padding)
    out = Image.new('RGBA', size)
    out.alpha_composite(content, ((size[0] - w) // 2, (size[1] - h) // 2))
    return out


def recolor(image, color):
    a = np.array(image)
    main = (a[:, :, :3].max(axis=2) < 65) & (a[:, :, 3] > 0)
    a[main, :3] = color
    return Image.fromarray(a)


def parent_assets():
    """Load the approved parent artwork only after verifying its vendored bytes."""
    folder = SOURCE / 'wow2'
    provenance = json.loads((folder / 'provenance.json').read_text())
    assets = {}
    for variant in ('navy', 'white'):
        name = f'wow2-primary-{variant}.png'
        path = folder / name
        expected = provenance['files'][name]['sha256']
        actual = hashlib.sha256(path.read_bytes()).hexdigest()
        if actual != expected:
            raise ValueError(f'Approved parent asset hash mismatch: {name}')
        with Image.open(path) as source:
            if source.mode != 'RGBA' or source.getchannel('A').getextrema()[0] != 0:
                raise ValueError(f'Expected transparent RGBA parent artwork: {name}')
            if not source.getchannel('A').getbbox():
                raise ValueError(f'Approved parent artwork is empty: {name}')
            content = trim(source, padding=0)
        height = 36
        width = round(content.width * height / content.height)
        assets[variant] = content.resize((width, height), Image.Resampling.LANCZOS)
    if not np.array_equal(np.asarray(assets['navy'].getchannel('A')),
                          np.asarray(assets['white'].getchannel('A'))):
        raise ValueError('Approved navy and white parent assets must share alpha geometry.')
    return assets, provenance


def endorse(wordmark, by_label, parent, first_letter_x):
    """Compose the unchanged product above a neutral by and exact parent logo."""
    bounds = wordmark.getchannel('A').getbbox()
    byline_height = max(by_label.height, parent.height)
    byline_y = bounds[3] + 10
    parent_x = first_letter_x + by_label.width + 12
    if parent_x + parent.width > bounds[2]:
        raise ValueError('Parent endorsement exceeds the product lettering width.')
    endorsed = Image.new('RGBA', (wordmark.width, byline_y + byline_height + 8))
    endorsed.alpha_composite(wordmark)
    endorsed.alpha_composite(by_label, (first_letter_x, byline_y + (byline_height - by_label.height) // 2))
    endorsed.alpha_composite(parent, (parent_x, byline_y + (byline_height - parent.height) // 2))
    return trim(endorsed)


def preview_font(size=23):
    return ImageFont.load_default(size=size)


def resize_height(image, height):
    return image.resize((round(image.width * height / image.height), height), Image.Resampling.LANCZOS)


def size_review(exports, parent, parent_origin, path):
    """Render fixed 1x review targets; enlarged symbols use nearest-neighbor pixels."""
    sheet = Image.new('RGB', (2048, 2048), '#e9ece9')
    draw = ImageDraw.Draw(sheet)
    title, label, small = preview_font(25), preview_font(19), preview_font(15)
    draw.text((20, 14), 'Wheelhouse / size review / actual CSS pixels at 100% zoom', font=title, fill='#183e32')
    draw.text((20, 48), 'Original artwork only. Symbol zooms are 3x nearest-neighbor; all other samples are actual size.',
              font=small, fill='#4b5c50')
    surfaces = [(12, '', '#ffffff', '#26392e'), (1032, '-white', '#18221e', '#e7f0e9')]
    measurements = {'symbols': [], 'primary': [], 'endorsement': [], 'tile': {'canvas': [64, 64]}}
    for x, suffix, background, ink in surfaces:
        draw.rectangle((x, 76, x + 1004, 306), fill=background)
        draw.text((x + 10, 85), 'Symbol / square slots / actual + 3x pixels', font=label, fill=ink)
        for i, side in enumerate((16, 20, 24, 32)):
            symbol = exports[f'wheelhouse-mark{suffix}.png'].copy()
            symbol.thumbnail((side, side), Image.Resampling.LANCZOS)
            slot = Image.new('RGBA', (side, side))
            slot.alpha_composite(symbol, ((side - symbol.width) // 2, (side - symbol.height) // 2))
            _, parts = filter_mask(np.asarray(slot.getchannel('A')) >= 128, minimum=2)
            cell = x + 14 + i * 247
            draw.text((cell, 118), f'{side} px / {len(parts)} facets', font=small, fill=ink)
            sheet.paste(slot, (cell, 149), slot)
            zoom = slot.resize((side * 3, side * 3), Image.Resampling.NEAREST)
            sheet.paste(zoom, (cell + 55, 149), zoom)
            if not suffix:
                measurements['symbols'].append({'square_size': side, 'render_pixels': list(symbol.size),
                                                'facets_at_alpha_128_min_2_pixels': len(parts)})
        draw.rectangle((x, 320, x + 1004, 520), fill=background)
        draw.text((x + 10, 330), 'Primary / total image height' + (' / white + mint' if suffix else ''), font=label, fill=ink)
        for i, height in enumerate((24, 32, 40)):
            primary = resize_height(exports[f'wheelhouse-wordmark{suffix}.png'], height)
            y = 368 + i * 48
            draw.text((x + 14, y), f'{height} px', font=small, fill=ink)
            sheet.paste(primary, (x + 100, y), primary)
            if suffix:
                mint = resize_height(exports['wheelhouse-wordmark-mint.png'], height)
                sheet.paste(mint, (x + 480, y), mint)
            else:
                measurements['primary'].append({'total_height': height, 'render_pixels': list(primary.size)})
        draw.rectangle((x, 535, x + 1004, 645), fill=background)
        tile = exports['wheelhouse-app-icon.png'].resize((64, 64), Image.Resampling.LANCZOS)
        sheet.paste(tile, (x + 14, 560), tile)
        draw.text((x + 100, 572), 'Tile / 64 x 64 px / general square avatar', font=label, fill=ink)

    draw.text((20, 660), 'Endorsement / proportional total height / parent requires symbol width >=24 and equivalent primary height >=24',
              font=small, fill='#183e32')
    parent_layer = Image.new('RGBA', exports['wheelhouse-wordmark-by-wow2.png'].size)
    parent_layer.alpha_composite(parent, parent_origin)
    with Image.open(SOURCE / 'wow2/wow2-primary-navy.png') as original:
        bounds = original.getchannel('A').getbbox()
        parent_padding_ratio = original.height / (bounds[3] - bounds[1])
    y = 700
    for height in (96, 112, 128, 144, 209):
        layer = resize_height(parent_layer, height)
        active = (np.asarray(layer.getchannel('A')) >= 128).any(axis=0)
        start = int(np.flatnonzero(active)[0])
        end = start
        while end < len(active) and active[end]:
            end += 1
        symbol_width = end - start
        equivalent_height = parent.height * height / parent_layer.height * parent_padding_ratio
        passes = symbol_width >= 24 and equivalent_height >= 24
        metric = {'total_height': height, 'render_pixels': list(layer.size),
                  'parent_symbol_visible_width_at_alpha_128': symbol_width,
                  'parent_equivalent_primary_height': round(equivalent_height, 2),
                  'meets_parent_size_rules': passes}
        measurements['endorsement'].append(metric)
        caption = (f'{height} px total / parent symbol {symbol_width} px / parent primary {equivalent_height:.1f} px'
                   f' / {"MEETS" if passes else "BELOW"} parent minima')
        for x, suffix, background, ink in surfaces:
            row_x, row_width = (12, 2024) if height == 209 else (x, 1004)
            draw.rectangle((row_x, y, row_x + row_width, y + height + 32), fill=background)
            draw.text((row_x + 8, y + 6), caption, font=small, fill=ink)
            asset = resize_height(exports[f'wheelhouse-wordmark-by-wow2{suffix}.png'], height)
            sheet.paste(asset, (row_x + 8, y + 26), asset)
            if height == 209:
                y += height + 38
        if height != 209:
            y += height + 38
    draw.text((20, 1980), 'Facets: connected alpha >=128 regions, each at least 2 pixels. Geometry checks support visual review; they do not prove recognition.',
              font=small, fill='#4b5c50')
    sheet.save(path)
    return measurements


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument(
        '--output-dir', type=Path, default=BRAND,
        help='Destination for artwork PNGs and review sheets (default: repository product/brand).',
    )
    args = parser.parse_args()
    destination = args.output_dir.expanduser().resolve()
    output = destination / 'soft-folds'

    with Image.open(SOURCE / 'soft-folds-selection.png') as source:
        selected = source.convert('RGB')
    if selected.size != (1774, 444):
        parser.error(f'Expected the saved 1774x444 selection; found {selected.width}x{selected.height}.')
    with Image.open(SOURCE / 'by-wow2-attribution.png') as source:
        if source.size != (160, 37):
            parser.error(f'Expected the saved 160x37 attribution crop; found {source.size}.')
        # The original by ends at x48; x48-55 is blank. Exclude the old wow2 text.
        by_source = source.convert('RGB').crop((0, 0, 52, source.height))
    try:
        parents, parent_provenance = parent_assets()
    except (OSError, KeyError, ValueError) as error:
        parser.error(str(error))

    wordmark_source = selected.crop((95, 185, 1285, 365))
    wordmark_raw, parts = remove_paper(wordmark_source)
    wordmark = trim(wordmark_raw)
    mark = trim(wordmark_raw.crop((0, 0, 312, wordmark_raw.height)))
    app_raw, _ = remove_paper(selected.crop((1340, 100, 1668, 420)), tile=True)
    app_icon = trim(app_raw, square=True)
    by_raw, _ = remove_paper(by_source)
    by_label = trim(by_raw, padding=0)
    # A 36-pixel parent logo stays subordinate to the 147-pixel primary lettering.
    wordmark_bounds = wordmark.getchannel('A').getbbox()
    wordmark_x_offset = wordmark_bounds[0] - wordmark_raw.getchannel('A').getbbox()[0]
    text_parts = [p for p in parts if p[0] > 312]
    first_letter_x = min(p[0] for p in text_parts) + wordmark_x_offset
    endorsed = endorse(wordmark, by_label, parents['navy'], first_letter_x)

    exports = {
        'wheelhouse-wordmark.png': wordmark,
        'wheelhouse-wordmark-by-wow2.png': endorsed,
        'wheelhouse-mark.png': mark,
        'wheelhouse-app-icon.png': app_icon,
    }
    for suffix, color in [('white', (255, 255, 255)), ('mint', (166, 212, 189))]:
        exports[f'wheelhouse-wordmark-{suffix}.png'] = recolor(wordmark, color)
        # Parent artwork remains approved white even when Wheelhouse uses mint.
        exports[f'wheelhouse-wordmark-by-wow2-{suffix}.png'] = endorse(
            exports[f'wheelhouse-wordmark-{suffix}.png'], recolor(by_label, (255, 255, 255)),
            parents['white'], first_letter_x,
        )
        exports[f'wheelhouse-mark-{suffix}.png'] = recolor(mark, color)

    output.mkdir(parents=True, exist_ok=True)
    manifest = []
    for name, im in exports.items():
        path = output / name
        im.save(path, optimize=True)
        a = np.array(im.getchannel('A'))
        assert a.min() == 0 and a.max() == 255, name
        assert (a[0, :] == 0).all() and (a[-1, :] == 0).all(), name
        assert (a[:, 0] == 0).all() and (a[:, -1] == 0).all(), name
        manifest.append({'file': name, 'width': im.width, 'height': im.height, 'bytes': path.stat().st_size,
                         'alpha_bbox': list(im.getchannel('A').getbbox()),
                         'opaque_pixels': int((a == 255).sum()), 'partial_pixels': int(((a > 0) & (a < 255)).sum())})

    # A review-only sheet displays every exported PNG on its intended contrast background.
    sheet = Image.new('RGB', (1500, 150 + len(exports) * 280), '#e9ece9')
    draw = ImageDraw.Draw(sheet)
    font = preview_font()
    draw.text((30, 30), 'Wheelhouse / Soft folds / transparent exports', font=font, fill='#183e32')
    for index, (name, im) in enumerate(exports.items()):
        y = 100 + index * 280
        dark = '-white' in name or '-mint' in name
        background = '#18221e' if dark else '#ffffff'
        draw.rounded_rectangle((20, y, 1480, y + 262), radius=12, fill=background)
        draw.text((40, y + 12), name, font=font, fill='#bbcebf' if dark else '#4b5c50')
        preview = im.copy()
        preview.thumbnail((1250, 209), Image.Resampling.LANCZOS)
        sheet.paste(preview, (40, y + 50), preview)
    sheet.save(destination / 'preview.png')
    measurements = size_review(exports, parents['navy'],
                               (first_letter_x + by_label.width + 12, wordmark_bounds[3] + 10),
                               destination / 'size-review.png')
    print(json.dumps({'exports': manifest, 'components': parts, 'byline_offset_x': first_letter_x,
                      'parent_content_height': 36, 'by_parent_gap': 12,
                      'parent_provenance': parent_provenance, 'size_review': measurements}, indent=2))


if __name__ == '__main__':
    main()

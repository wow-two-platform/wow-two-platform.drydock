#!/usr/bin/env python3
"""Replay the approved Soft folds exports from read-only repository source images.

Requires Pillow and NumPy. By default, writes product/brand/soft-folds/*.png
and product/brand/preview.png. Use --output-dir for an isolated replay.
Only preview labels use a font; exported logos retain the source lettering.
"""

import argparse
from collections import deque
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


def preview_font():
    for name in ('DejaVuSans.ttf', 'Arial.ttf', 'Helvetica.ttc'):
        try:
            return ImageFont.truetype(name, 23)
        except OSError:
            pass
    return ImageFont.load_default()


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument(
        '--output-dir', type=Path, default=BRAND,
        help='Destination for soft-folds/*.png and preview.png (default: repository product/brand).',
    )
    args = parser.parse_args()
    destination = args.output_dir.expanduser().resolve()
    output = destination / 'soft-folds'

    with Image.open(SOURCE / 'soft-folds-selection.png') as source:
        selected = source.convert('RGB')
    if selected.size != (1774, 444):
        parser.error(f'Expected the saved 1774x444 selection; found {selected.width}x{selected.height}.')
    with Image.open(SOURCE / 'by-wow2-attribution.png') as source:
        byline_source = source.convert('RGB')

    wordmark_source = selected.crop((95, 185, 1285, 365))
    wordmark_raw, parts = remove_paper(wordmark_source)
    wordmark = trim(wordmark_raw)
    mark = trim(wordmark_raw.crop((0, 0, 312, wordmark_raw.height)))
    app_raw, _ = remove_paper(selected.crop((1340, 100, 1668, 420)), tile=True)
    app_icon = trim(app_raw, square=True)
    byline_raw, _ = remove_paper(byline_source)
    byline = trim(byline_raw, padding=0)
    # The endorsed lockup reuses the approved attribution lettering without changing its shape.
    # A 31-pixel byline remains subordinate to the 147-pixel primary lettering.
    wordmark_bounds = wordmark.getchannel('A').getbbox()
    wordmark_x_offset = wordmark_bounds[0] - wordmark_raw.getchannel('A').getbbox()[0]
    text_parts = [p for p in parts if p[0] > 312]
    first_letter_x = min(p[0] for p in text_parts) + wordmark_x_offset
    endorsed = Image.new('RGBA', (wordmark.width, wordmark.height + byline.height + 10))
    endorsed.alpha_composite(wordmark)
    endorsed.alpha_composite(byline, (first_letter_x, wordmark_bounds[3] + 10))
    endorsed = trim(endorsed)

    exports = {
        'wheelhouse-wordmark.png': wordmark,
        'wheelhouse-wordmark-by-wow2.png': endorsed,
        'wheelhouse-mark.png': mark,
        'wheelhouse-app-icon.png': app_icon,
    }
    for suffix, color in [('white', (255, 255, 255)), ('mint', (166, 212, 189))]:
        for name, asset in [('wordmark', wordmark), ('wordmark-by-wow2', endorsed), ('mark', mark)]:
            exports[f'wheelhouse-{name}-{suffix}.png'] = recolor(asset, color)

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
    sheet = Image.new('RGB', (1500, 150 + len(exports) * 180), '#e9ece9')
    draw = ImageDraw.Draw(sheet)
    font = preview_font()
    draw.text((30, 30), 'Wheelhouse / Soft folds / transparent exports', font=font, fill='#183e32')
    for index, (name, im) in enumerate(exports.items()):
        y = 100 + index * 180
        dark = '-white' in name or '-mint' in name
        background = '#18221e' if dark else '#ffffff'
        draw.rounded_rectangle((20, y, 1480, y + 162), radius=12, fill=background)
        draw.text((40, y + 12), name, font=font, fill='#bbcebf' if dark else '#4b5c50')
        preview = im.copy()
        preview.thumbnail((1250, 106), Image.Resampling.LANCZOS)
        sheet.paste(preview, (40, y + 50), preview)
    sheet.save(destination / 'preview.png')
    print(json.dumps({'exports': manifest, 'components': parts, 'byline_offset_x': first_letter_x}, indent=2))


if __name__ == '__main__':
    main()

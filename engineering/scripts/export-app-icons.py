#!/usr/bin/env python3
"""Derive the web app's logo files from the approved brand exports.

Requires Pillow. Each source must match its SHA-256 in product/brand/manifest.json. By default, writes the header
wordmarks and the favicon and touch icons into the web app; use --output-dir for an isolated replay.
"""

import argparse
import hashlib
import json
from pathlib import Path

from PIL import Image


REPO = Path(__file__).resolve().parents[2]
BRAND = REPO / 'product/brand'
WEB = REPO / 'engineering/codebase/wheelhouse.frontend-services/apps/web'

# Four times the brand guide's 24px primary minimum, so the header stays sharp on high-density screens.
WORDMARK_HEIGHT = 96
WORDMARK_COLOURS = 64
FAVICON_SIZE = 64
FAVICON_MARGIN = 2
TOUCH_ICON_SIZE = 180


def source(name):
    """Opens an approved export after checking it against the brand manifest."""
    manifest = json.loads((BRAND / 'manifest.json').read_text())
    record = next(entry for entry in manifest['exports'] if entry['file'] == f'soft-folds/{name}')
    path = BRAND / record['file']
    if hashlib.sha256(path.read_bytes()).hexdigest() != record['sha256']:
        raise SystemExit(f'{path} does not match product/brand/manifest.json')
    return Image.open(path).convert('RGBA')


def wordmark(name):
    """Scales the padded primary wordmark to the header asset height, on a 64-colour palette with alpha."""
    image = source(name)
    width = round(image.width * WORDMARK_HEIGHT / image.height)
    image = image.resize((width, WORDMARK_HEIGHT), Image.LANCZOS)
    return image.quantize(WORDMARK_COLOURS, method=Image.Quantize.FASTOCTREE, dither=Image.Dither.NONE)


def favicon(name):
    """Centres the standalone boat in a transparent square slot."""
    mark = source(name)
    mark = mark.crop(mark.getbbox())
    scale = (FAVICON_SIZE - 2 * FAVICON_MARGIN) / max(mark.size)
    mark = mark.resize((round(mark.width * scale), round(mark.height * scale)), Image.LANCZOS)
    canvas = Image.new('RGBA', (FAVICON_SIZE, FAVICON_SIZE))
    canvas.alpha_composite(mark, ((FAVICON_SIZE - mark.width) // 2, (FAVICON_SIZE - mark.height) // 2))
    return canvas


def touch_icon():
    """Extends the tile's green to a full-bleed opaque square; the platform applies its own corner mask."""
    tile = source('wheelhouse-app-icon.png')
    tile = tile.crop(tile.getbbox())
    side = max(tile.size)
    green = tile.getpixel((tile.width // 2, tile.height // 12))[:3]
    canvas = Image.new('RGBA', (side, side), green + (255,))
    canvas.alpha_composite(tile, ((side - tile.width) // 2, (side - tile.height) // 2))
    return canvas.convert('RGB').resize((TOUCH_ICON_SIZE, TOUCH_ICON_SIZE), Image.LANCZOS)


OUTPUTS = {
    'src/presentation/shell/assets/wordmark-dark-ink.png': lambda: wordmark('wheelhouse-wordmark.png'),
    'src/presentation/shell/assets/wordmark-light-ink.png': lambda: wordmark('wheelhouse-wordmark-white.png'),
    'public/favicon.png': lambda: favicon('wheelhouse-mark.png'),
    'public/favicon-dark.png': lambda: favicon('wheelhouse-mark-white.png'),
    'public/apple-touch-icon.png': touch_icon,
}


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--output-dir', type=Path, default=WEB, help='the web app directory to write into')
    args = parser.parse_args()
    for relative, render in OUTPUTS.items():
        target = args.output_dir / relative
        target.parent.mkdir(parents=True, exist_ok=True)
        render().save(target, optimize=True)
        print(f'{relative}  {hashlib.sha256(target.read_bytes()).hexdigest()}')


if __name__ == '__main__':
    main()

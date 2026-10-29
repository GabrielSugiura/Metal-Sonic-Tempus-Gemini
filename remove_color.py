#!/usr/bin/env python3
"""
remove_color.py — Replace all pixels of a given hex color with fully transparent.

Usage:
    python3 remove_color.py <input.png> <output.png> [--color #b5a5d5]
    python3 remove_color.py --in-place <file.png> [--color #b5a5d5]

Behavior:
    For every pixel whose RGB triple equals the target color,
    set its alpha channel to 0 (fully transparent). Every other
    pixel is left untouched.

Note:
    Matching is exact on RGB; alpha is always forced to 0 for
    matching pixels, so the input may be any mode (RGB, RGBA,
    etc.) and the output is always RGBA.
"""

import argparse
import sys
from pathlib import Path

from PIL import Image

# Default target color
DEFAULT_COLOR = (0xB5, 0xA5, 0xD5)


def parse_hex(color_str: str) -> tuple[int, int, int]:
    """Parse a '#RRGGBB' (or 'RRGGBB') hex string into an (R, G, B) tuple."""
    color_str = color_str.lstrip("#")
    if len(color_str) != 6:
        raise ValueError(f"Color must be #RRGGBB, got {color_str!r}")
    try:
        value = int(color_str, 16)
    except ValueError as exc:
        raise ValueError(f"Invalid hex digits in {color_str!r}") from exc
    return ((value >> 16) & 0xFF, (value >> 8) & 0xFF, value & 0xFF)


def remove_color(
    input_path: Path, output_path: Path, target_color: tuple[int, int, int]
) -> None:
    """Open the image, erase target_color pixels to transparent, save."""
    img = Image.open(input_path)
    # Normalize to RGBA so we can always write alpha
    img = img.convert("RGBA")
    data = img.getdata()

    r, g, b = target_color
    new_pixels = []
    replaced = 0
    for px in data:
        if px[0] == r and px[1] == g and px[2] == b:
            new_pixels.append((r, g, b, 0))
            replaced += 1
        else:
            new_pixels.append(px)

    img.putdata(new_pixels)
    img.save(output_path)
    print(f"Replaced {replaced} pixels of #{r:02X}{g:02X}{b:02X} -> transparent")
    print(f"Saved -> {output_path}")


def main() -> None:
    parser = argparse.ArgumentParser(
        description="Erase a hex color from an image (set to transparent)."
    )
    parser.add_argument(
        "input", help="Input image path (PNG)."
    )
    parser.add_argument(
        "output", nargs="?",
        help="Output image path. Omit to overwrite the input in place "
             "(backup made automatically).",
    )
    parser.add_argument(
        "--color", default="#b5a5d5",
        help="Hex color to erase, e.g. '#b5a5d5' (default: #b5a5d5).",
    )
    parser.add_argument(
        "--in-place", action="store_true",
        help="Overwrite the input file instead of writing a new file.",
    )

    args = parser.parse_args()

    target_color = parse_hex(args.color)

    input_path = Path(args.input)
    if not input_path.exists():
        sys.exit(f"Error: input file not found: {input_path}")

    if args.in_place or args.output is None:
        # Backup the original (copy, so the input path stays valid for processing)
        backup_path = input_path.with_suffix(input_path.suffix + ".bak")
        import shutil
        if not backup_path.exists():
            shutil.copy2(input_path, backup_path)
            print(f"Backed up original to {backup_path}")
        output_path = input_path
    else:
        output_path = Path(args.output)

    remove_color(input_path, output_path, target_color)


if __name__ == "__main__":
    main()

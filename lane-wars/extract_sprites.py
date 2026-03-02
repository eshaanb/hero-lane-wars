"""
Extract individual assets from source_spritesheet.png and save them
as separate PNG files with transparency for the Lane Wars project.

Usage:
  python extract_sprites.py              # Extract all assets
  python extract_sprites.py --debug      # Draw bounding boxes on a debug image first
  python extract_sprites.py --tolerance 45  # Override bg removal for all assets
"""

import os
import sys
from collections import deque
from PIL import Image, ImageDraw
import numpy as np

PROJ = os.path.dirname(os.path.abspath(__file__))
SRC = os.path.join(PROJ, "assets", "source_spritesheet.png")
OUT = os.path.join(PROJ, "assets", "sprites")


def remove_bg_flood(img, tolerance=40):
    """
    Remove background via edge-seeded flood fill.
    Only removes pixels connected to the image border that match
    sampled background colors within the given tolerance.
    """
    rgba = img.convert("RGBA")
    data = np.array(rgba)
    h, w = data.shape[:2]

    # Sample background from corners + edge midpoints
    sample_points = [
        (0, 0), (0, w - 1), (h - 1, 0), (h - 1, w - 1),
        (0, w // 4), (0, 3 * w // 4),
        (h - 1, w // 4), (h - 1, 3 * w // 4),
        (h // 4, 0), (3 * h // 4, 0),
        (h // 4, w - 1), (3 * h // 4, w - 1),
    ]
    bg_colors = []
    for sy, sx in sample_points:
        if 0 <= sy < h and 0 <= sx < w:
            bg_colors.append(data[sy, sx, :3].astype(np.int32))

    # Deduplicate similar bg samples (cluster within 15 of each other)
    unique_bg = []
    for c in bg_colors:
        is_dup = False
        for u in unique_bg:
            if np.sum((c - u) ** 2) < 15 * 15:
                is_dup = True
                break
        if not is_dup:
            unique_bg.append(c)

    tol_sq = tolerance * tolerance

    # Pre-compute "could be background" mask (vectorized)
    could_be_bg = np.zeros((h, w), dtype=bool)
    for bg in unique_bg:
        diff = np.sum((data[:, :, :3].astype(np.int32) - bg) ** 2, axis=2)
        could_be_bg |= (diff < tol_sq)

    # BFS flood fill from edges
    visited = np.zeros((h, w), dtype=bool)
    to_clear = np.zeros((h, w), dtype=bool)
    queue = deque()

    for x in range(w):
        if could_be_bg[0, x] and not visited[0, x]:
            queue.append((0, x)); visited[0, x] = True
        if could_be_bg[h - 1, x] and not visited[h - 1, x]:
            queue.append((h - 1, x)); visited[h - 1, x] = True
    for y in range(1, h - 1):
        if could_be_bg[y, 0] and not visited[y, 0]:
            queue.append((y, 0)); visited[y, 0] = True
        if could_be_bg[y, w - 1] and not visited[y, w - 1]:
            queue.append((y, w - 1)); visited[y, w - 1] = True

    while queue:
        cy, cx = queue.popleft()
        to_clear[cy, cx] = True
        for dy, dx in ((-1, 0), (1, 0), (0, -1), (0, 1)):
            ny, nx = cy + dy, cx + dx
            if 0 <= ny < h and 0 <= nx < w and not visited[ny, nx] and could_be_bg[ny, nx]:
                visited[ny, nx] = True
                queue.append((ny, nx))

    data[to_clear, 3] = 0
    return Image.fromarray(data)


def auto_trim(img):
    """Trim fully transparent border pixels."""
    alpha = np.array(img)[:, :, 3]
    rows = np.any(alpha > 0, axis=1)
    cols = np.any(alpha > 0, axis=0)
    if not rows.any() or not cols.any():
        return img
    y0, y1 = np.where(rows)[0][[0, -1]]
    x0, x1 = np.where(cols)[0][[0, -1]]
    return img.crop((x0, y0, x1 + 1, y1 + 1))


def pad_to_square(img):
    """Pad image to a square canvas, centered."""
    w, h = img.size
    size = max(w, h)
    canvas = Image.new("RGBA", (size, size), (0, 0, 0, 0))
    canvas.paste(img, ((size - w) // 2, (size - h) // 2), img)
    return canvas


def crop_and_save(img, bbox, output_path, target_size=None, bg_tolerance=40,
                  do_trim=True, square=False):
    """Crop, remove background, trim, optionally square-pad, resize, save."""
    cropped = img.crop(bbox)
    cleaned = remove_bg_flood(cropped, tolerance=bg_tolerance)
    if do_trim:
        cleaned = auto_trim(cleaned)
    if square:
        cleaned = pad_to_square(cleaned)
    if target_size:
        cleaned = cleaned.resize(target_size, Image.NEAREST)
    os.makedirs(os.path.dirname(output_path), exist_ok=True)
    cleaned.save(output_path)
    print(f"  -> {os.path.relpath(output_path, PROJ)}  ({cleaned.size[0]}x{cleaned.size[1]})")


def get_assets(w, h):
    """
    Return asset definitions with bounding boxes scaled to actual image size.
    Reference coordinates assume 1024x536 source; auto-scaled to actual.

    mode:
      "flood"  — edge-seeded flood fill (good when asset colors differ from bg)
      "crop"   — tight crop only, no bg removal (for assets whose colors match bg)
    """
    sx = w / 1024.0
    sy = h / 536.0

    def box(x0, y0, x1, y1):
        return (int(x0 * sx), int(y0 * sy), int(x1 * sx), int(y1 * sy))

    return {
        # ── UI Elements ──
        # Ability frame: dark grey-blue frame. Colors too close to bg for
        # flood fill. Crop tight and keep as-is (frame includes its own bg).
        "ability_frame": {
            "bbox": box(10, 10, 195, 208),
            "path": os.path.join(OUT, "ui", "ability_frame.png"),
            "target": (48, 48),
            "mode": "crop",
        },
        # Gems are tiny colored diamonds inside dark-bordered squares.
        # Flood fill leaks through the dark borders. Just crop them.
        # Coordinates verified via pixel brightness scanning.
        "gem_blue": {
            "bbox": (230, 816, 394, 980),
            "path": os.path.join(OUT, "ui", "gem_blue.png"),
            "target": (16, 16),
            "mode": "crop",
            "_note": "absolute coords (not scaled)",
        },
        "gem_red": {
            "bbox": (443, 816, 598, 980),
            "path": os.path.join(OUT, "ui", "gem_red.png"),
            "target": (16, 16),
            "mode": "crop",
            "_note": "absolute coords (not scaled)",
        },
        # Star: light 4-point shape on dark ground.
        "star_icon": {
            "bbox": box(942, 450, 1000, 510),
            "path": os.path.join(OUT, "ui", "star_icon.png"),
            "target": (16, 16),
            "mode": "flood",
            "tolerance": 20,
            "square": True,
        },

        # ── Buildings ──
        # Grey stone is distinct from brown dirt — flood fill works.
        "war_academy": {
            "bbox": box(315, 12, 555, 295),
            "path": os.path.join(OUT, "buildings", "war_academy.png"),
            "target": (64, 64),
            "mode": "flood",
            "tolerance": 24,
            "square": True,
        },
        "watch_tower": {
            "bbox": box(738, 8, 892, 338),
            "path": os.path.join(OUT, "buildings", "watch_tower.png"),
            "target": (32, 64),
            "mode": "flood",
            "tolerance": 24,
        },

        # ── Units ──
        # Blue catapult: tighter bbox to exclude UI gem in top-left.
        "catapult_blue": {
            "bbox": box(210, 298, 415, 475),
            "path": os.path.join(OUT, "units", "catapult_blue_new.png"),
            "target": (32, 32),
            "mode": "flood",
            "tolerance": 22,
            "square": True,
        },
        # Red catapult: brown wood matches dirt. Crop only — user
        # can hand-clean in a pixel editor from the full-res version.
        "catapult_red": {
            "bbox": box(528, 298, 715, 475),
            "path": os.path.join(OUT, "units", "catapult_red_new.png"),
            "target": (32, 32),
            "mode": "crop",
        },

        # ── Environment ──
        # Lane tile: clean dirt patch with no overlapping assets.
        "lane_tile": {
            "bbox": (1800, 560, 2000, 760),
            "path": os.path.join(OUT, "environment", "lane_tile.png"),
            "target": (64, 64),
            "mode": "crop",
            "_note": "absolute coords (not scaled)",
        },
    }


def debug_mode(img, assets):
    """Draw labeled bounding boxes on a copy of the image for verification."""
    debug = img.copy()
    draw = ImageDraw.Draw(debug)
    colors = [
        "red", "lime", "cyan", "magenta", "yellow",
        "orange", "white", "deeppink", "dodgerblue",
    ]
    for i, (name, info) in enumerate(assets.items()):
        color = colors[i % len(colors)]
        bbox = info["bbox"]
        draw.rectangle(bbox, outline=color, width=3)
        draw.text((bbox[0] + 6, bbox[1] + 6), name, fill=color)
    out_path = os.path.join(PROJ, "assets", "debug_bboxes.png")
    debug.save(out_path)
    print(f"Debug image saved: {out_path}")
    print("Open it to verify bounding boxes, then re-run without --debug.")


def main():
    args = sys.argv[1:]
    tolerance_override = None
    is_debug = "--debug" in args

    if "--tolerance" in args:
        idx = args.index("--tolerance")
        if idx + 1 < len(args):
            tolerance_override = int(args[idx + 1])

    img = Image.open(SRC).convert("RGBA")
    w, h = img.size
    print(f"Source: {SRC}")
    print(f"Size:   {w} x {h}\n")

    assets = get_assets(w, h)

    if is_debug:
        debug_mode(img, assets)
        return

    # ── Extract game-ready (downscaled) sprites ──
    print("=== Extracting game-ready sprites ===\n")
    for name, info in assets.items():
        mode = info.get("mode", "flood")
        tol = tolerance_override if tolerance_override is not None else info.get("tolerance", 0)
        sq = info.get("square", False)
        print(f"[{name}]  (mode={mode}, tolerance={tol})")
        try:
            if mode == "crop":
                # Simple crop + resize, no background removal
                cropped = img.crop(info["bbox"])
                if info.get("target"):
                    cropped = cropped.resize(info["target"], Image.NEAREST)
                os.makedirs(os.path.dirname(info["path"]), exist_ok=True)
                cropped.save(info["path"])
                print(f"  -> {os.path.relpath(info['path'], PROJ)}  ({cropped.size[0]}x{cropped.size[1]})")
            else:
                crop_and_save(img, info["bbox"], info["path"],
                              target_size=info.get("target"), bg_tolerance=tol,
                              square=sq)
        except Exception as e:
            print(f"  ERROR: {e}")

    # ── Full-resolution versions ──
    print("\n=== Full-resolution versions (for hand-editing) ===\n")
    fullres_dir = os.path.join(OUT, "extracted_fullres")
    os.makedirs(fullres_dir, exist_ok=True)

    for name, info in assets.items():
        mode = info.get("mode", "flood")
        tol = tolerance_override if tolerance_override is not None else info.get("tolerance", 0)
        sq = info.get("square", False)
        print(f"[{name}]  (mode={mode})")
        try:
            if mode == "crop":
                cropped = img.crop(info["bbox"])
                path = os.path.join(fullres_dir, f"{name}.png")
                os.makedirs(os.path.dirname(path), exist_ok=True)
                cropped.save(path)
                print(f"  -> {os.path.relpath(path, PROJ)}  ({cropped.size[0]}x{cropped.size[1]})")
            else:
                crop_and_save(img, info["bbox"],
                              os.path.join(fullres_dir, f"{name}.png"),
                              target_size=None, bg_tolerance=tol, square=sq)
        except Exception as e:
            print(f"  ERROR: {e}")

    print(f"\nDone! Check {os.path.relpath(OUT, PROJ)} for results.")
    print("Tip: run with --debug to verify bounding boxes visually.")


if __name__ == "__main__":
    main()

"""
Generate readable prototype sprites for the Lane Wars Godot project.
Uses only built-in Python modules (struct, zlib) so it can run anywhere.
"""

import os
import struct
import zlib


PNG_SIGNATURE = b"\x89PNG\r\n\x1a\n"
BASE = os.path.join(os.path.dirname(os.path.abspath(__file__)), "assets", "sprites")


def make_chunk(chunk_type: bytes, data: bytes) -> bytes:
    chunk = chunk_type + data
    return struct.pack(">I", len(data)) + chunk + struct.pack(">I", zlib.crc32(chunk) & 0xFFFFFFFF)


def encode_png(width: int, height: int, pixels):
    raw = bytearray()
    for y in range(height):
        raw.append(0)
        for x in range(width):
            raw.extend(pixels[y][x])
    ihdr = struct.pack(">IIBBBBB", width, height, 8, 6, 0, 0, 0)
    return (
        PNG_SIGNATURE
        + make_chunk(b"IHDR", ihdr)
        + make_chunk(b"IDAT", zlib.compress(bytes(raw)))
        + make_chunk(b"IEND", b"")
    )


def blank(w: int, h: int, color=(0, 0, 0, 0)):
    return [[color for _ in range(w)] for _ in range(h)]


def set_px(pixels, x: int, y: int, color):
    if 0 <= y < len(pixels) and 0 <= x < len(pixels[0]):
        pixels[y][x] = color


def fill_rect(pixels, x0: int, y0: int, x1: int, y1: int, color):
    for y in range(y0, y1):
        for x in range(x0, x1):
            set_px(pixels, x, y, color)


def h_line(pixels, x0: int, x1: int, y: int, color):
    for x in range(x0, x1 + 1):
        set_px(pixels, x, y, color)


def v_line(pixels, x: int, y0: int, y1: int, color):
    for y in range(y0, y1 + 1):
        set_px(pixels, x, y, color)


def outline_rect(pixels, x0: int, y0: int, x1: int, y1: int, color):
    h_line(pixels, x0, x1 - 1, y0, color)
    h_line(pixels, x0, x1 - 1, y1 - 1, color)
    v_line(pixels, x0, y0, y1 - 1, color)
    v_line(pixels, x1 - 1, y0, y1 - 1, color)


def save_png(path: str, width: int, height: int, pixels):
    os.makedirs(os.path.dirname(path), exist_ok=True)
    data = encode_png(width, height, pixels)
    with open(path, "wb") as f:
        f.write(data)
    print(f"  wrote {path} ({width}x{height})")


def draw_footman(fill, outline, accent):
    pixels = blank(16, 16)
    fill_rect(pixels, 6, 1, 10, 4, accent)      # helmet
    fill_rect(pixels, 5, 4, 11, 10, fill)       # torso
    fill_rect(pixels, 4, 5, 6, 9, accent)       # shield
    fill_rect(pixels, 10, 5, 12, 8, accent)     # sword arm
    fill_rect(pixels, 6, 10, 8, 15, fill)       # left leg
    fill_rect(pixels, 8, 10, 10, 15, fill)      # right leg
    outline_rect(pixels, 5, 4, 11, 10, outline)
    outline_rect(pixels, 4, 5, 6, 9, outline)
    outline_rect(pixels, 6, 1, 10, 4, outline)
    return pixels


def draw_archer(fill, outline, accent):
    pixels = blank(16, 16)
    fill_rect(pixels, 6, 2, 10, 5, accent)      # hood
    fill_rect(pixels, 6, 5, 10, 10, fill)       # torso
    fill_rect(pixels, 5, 10, 7, 15, fill)       # left leg
    fill_rect(pixels, 9, 10, 11, 15, fill)      # right leg
    v_line(pixels, 12, 3, 11, outline)          # bow stave
    set_px(pixels, 11, 4, accent)
    set_px(pixels, 11, 10, accent)
    h_line(pixels, 10, 12, 7, outline)          # drawn arm/bow grip
    outline_rect(pixels, 6, 5, 10, 10, outline)
    outline_rect(pixels, 6, 2, 10, 5, outline)
    return pixels


def draw_knight(fill, outline, accent):
    pixels = blank(16, 16)
    fill_rect(pixels, 5, 1, 11, 4, accent)
    fill_rect(pixels, 4, 4, 12, 10, fill)
    fill_rect(pixels, 3, 5, 5, 10, outline)
    fill_rect(pixels, 11, 5, 13, 9, accent)
    fill_rect(pixels, 5, 10, 8, 15, fill)
    fill_rect(pixels, 8, 10, 11, 15, fill)
    outline_rect(pixels, 4, 4, 12, 10, outline)
    outline_rect(pixels, 5, 1, 11, 4, outline)
    return pixels


def draw_catapult(fill, outline, accent):
    pixels = blank(16, 16)
    h_line(pixels, 3, 11, 11, fill)
    v_line(pixels, 5, 8, 11, fill)
    v_line(pixels, 9, 8, 11, fill)
    set_px(pixels, 3, 12, outline)
    set_px(pixels, 11, 12, outline)
    v_line(pixels, 11, 4, 10, outline)
    h_line(pixels, 8, 13, 4, outline)
    set_px(pixels, 13, 3, accent)
    set_px(pixels, 14, 2, accent)
    return pixels


def draw_barracks():
    pixels = blank(32, 32)
    wall = (135, 108, 78, 255)
    roof = (170, 72, 56, 255)
    wood = (86, 56, 36, 255)
    shadow = (54, 42, 32, 255)
    flag = (210, 210, 130, 255)
    fill_rect(pixels, 4, 12, 28, 28, wall)
    fill_rect(pixels, 2, 8, 30, 14, roof)
    fill_rect(pixels, 13, 18, 19, 28, wood)
    fill_rect(pixels, 7, 17, 11, 21, wood)
    fill_rect(pixels, 21, 17, 25, 21, wood)
    v_line(pixels, 24, 4, 11, shadow)
    fill_rect(pixels, 24, 4, 29, 7, flag)
    outline_rect(pixels, 4, 12, 28, 28, shadow)
    outline_rect(pixels, 2, 8, 30, 14, shadow)
    return pixels


def draw_archery_range():
    pixels = blank(32, 32)
    wood = (102, 84, 54, 255)
    canopy = (88, 130, 76, 255)
    target = (220, 210, 190, 255)
    ring = (180, 64, 48, 255)
    shadow = (40, 48, 28, 255)
    fill_rect(pixels, 4, 16, 28, 28, wood)
    fill_rect(pixels, 6, 10, 26, 16, canopy)
    fill_rect(pixels, 20, 6, 22, 28, shadow)    # support post
    fill_rect(pixels, 8, 18, 15, 25, target)
    outline_rect(pixels, 8, 18, 15, 25, ring)
    fill_rect(pixels, 10, 20, 13, 23, ring)
    outline_rect(pixels, 4, 16, 28, 28, shadow)
    outline_rect(pixels, 6, 10, 26, 16, shadow)
    return pixels


def draw_armory():
    pixels = blank(32, 32)
    wall = (112, 116, 124, 255)
    trim = (78, 82, 92, 255)
    ember = (224, 114, 48, 255)
    steel = (196, 204, 214, 255)
    fill_rect(pixels, 4, 12, 28, 28, wall)
    fill_rect(pixels, 6, 8, 26, 12, trim)
    fill_rect(pixels, 10, 18, 22, 26, trim)
    fill_rect(pixels, 22, 16, 26, 26, steel)
    fill_rect(pixels, 8, 22, 12, 26, ember)
    outline_rect(pixels, 4, 12, 28, 28, trim)
    outline_rect(pixels, 10, 18, 22, 26, trim)
    return pixels


def draw_siege_workshop():
    pixels = blank(32, 32)
    wood = (120, 92, 62, 255)
    roof = (98, 86, 74, 255)
    metal = (158, 166, 176, 255)
    dark = (56, 42, 30, 255)
    fill_rect(pixels, 4, 14, 28, 28, wood)
    fill_rect(pixels, 2, 10, 30, 16, roof)
    fill_rect(pixels, 9, 18, 23, 22, metal)
    v_line(pixels, 12, 17, 25, dark)
    v_line(pixels, 20, 17, 25, dark)
    outline_rect(pixels, 4, 14, 28, 28, dark)
    outline_rect(pixels, 2, 10, 30, 16, dark)
    return pixels


def draw_forge():
    pixels = blank(32, 32)
    stone = (92, 86, 92, 255)
    dark = (46, 36, 44, 255)
    fire = (236, 126, 50, 255)
    steel = (174, 182, 192, 255)
    fill_rect(pixels, 5, 14, 27, 28, stone)
    fill_rect(pixels, 10, 18, 22, 26, dark)
    fill_rect(pixels, 12, 20, 20, 25, fire)
    fill_rect(pixels, 22, 10, 25, 20, steel)
    h_line(pixels, 18, 26, 10, steel)
    outline_rect(pixels, 5, 14, 27, 28, dark)
    return pixels


def draw_tower():
    pixels = blank(64, 64)
    stone = (155, 162, 172, 255)
    dark = (74, 80, 92, 255)
    banner = (196, 56, 52, 255)
    glow = (228, 204, 122, 255)
    fill_rect(pixels, 22, 12, 42, 56, stone)
    fill_rect(pixels, 18, 8, 46, 16, stone)
    fill_rect(pixels, 16, 16, 48, 22, stone)
    fill_rect(pixels, 18, 56, 46, 60, dark)
    for x in (18, 24, 30, 36, 42):
        fill_rect(pixels, x, 4, x + 4, 10, stone)
    fill_rect(pixels, 29, 24, 35, 40, dark)     # doorway
    fill_rect(pixels, 24, 24, 28, 28, glow)
    fill_rect(pixels, 36, 24, 40, 28, glow)
    v_line(pixels, 46, 14, 32, dark)
    fill_rect(pixels, 46, 14, 54, 20, banner)
    outline_rect(pixels, 22, 12, 42, 56, dark)
    outline_rect(pixels, 16, 16, 48, 22, dark)
    return pixels


def draw_tower_variant(stone, dark, banner, glow):
    pixels = draw_tower()
    # Repaint the shared tower silhouette with race colors.
    fill_rect(pixels, 22, 12, 42, 56, stone)
    fill_rect(pixels, 18, 8, 46, 16, stone)
    fill_rect(pixels, 16, 16, 48, 22, stone)
    fill_rect(pixels, 18, 56, 46, 60, dark)
    for x in (18, 24, 30, 36, 42):
        fill_rect(pixels, x, 4, x + 4, 10, stone)
    fill_rect(pixels, 29, 24, 35, 40, dark)
    fill_rect(pixels, 24, 24, 28, 28, glow)
    fill_rect(pixels, 36, 24, 40, 28, glow)
    v_line(pixels, 46, 14, 32, dark)
    fill_rect(pixels, 46, 14, 54, 20, banner)
    outline_rect(pixels, 22, 12, 42, 56, dark)
    outline_rect(pixels, 16, 16, 48, 22, dark)
    return pixels


def draw_treasury():
    pixels = blank(32, 32)
    wall = (146, 122, 76, 255)
    roof = (214, 176, 62, 255)
    dark = (86, 62, 28, 255)
    coin = (242, 214, 98, 255)
    fill_rect(pixels, 5, 13, 27, 27, wall)
    fill_rect(pixels, 3, 8, 29, 14, roof)
    fill_rect(pixels, 12, 18, 20, 27, dark)
    fill_rect(pixels, 22, 18, 25, 21, coin)
    fill_rect(pixels, 24, 20, 27, 23, coin)
    outline_rect(pixels, 5, 13, 27, 27, dark)
    outline_rect(pixels, 3, 8, 29, 14, dark)
    return pixels


def draw_gold_icon():
    pixels = blank(16, 16)
    gold = (238, 198, 56, 255)
    dark = (162, 120, 22, 255)
    fill_rect(pixels, 3, 5, 13, 11, gold)
    outline_rect(pixels, 3, 5, 13, 11, dark)
    fill_rect(pixels, 5, 3, 11, 5, gold)
    outline_rect(pixels, 5, 3, 11, 5, dark)
    return pixels


def draw_heart_icon():
    pixels = blank(16, 16)
    red = (214, 48, 72, 255)
    dark = (136, 22, 40, 255)
    fill_rect(pixels, 4, 3, 7, 6, red)
    fill_rect(pixels, 9, 3, 12, 6, red)
    fill_rect(pixels, 3, 5, 13, 9, red)
    fill_rect(pixels, 5, 9, 11, 12, red)
    fill_rect(pixels, 6, 12, 10, 14, red)
    outline_rect(pixels, 4, 3, 7, 6, dark)
    outline_rect(pixels, 9, 3, 12, 6, dark)
    return pixels


def draw_grid_cell(fill, border):
    pixels = blank(32, 32)
    fill_rect(pixels, 0, 0, 32, 32, fill)
    outline_rect(pixels, 0, 0, 32, 32, border)
    return pixels


def generate_all():
    print("Generating prototype sprites...\n")

    save_png(os.path.join(BASE, "units", "footman_blue.png"), 16, 16, draw_footman((74, 118, 212, 255), (28, 54, 124, 255), (188, 210, 248, 255)))
    save_png(os.path.join(BASE, "units", "footman_red.png"), 16, 16, draw_footman((208, 78, 72, 255), (118, 30, 28, 255), (246, 196, 178, 255)))
    save_png(os.path.join(BASE, "units", "archer_blue.png"), 16, 16, draw_archer((70, 170, 156, 255), (22, 88, 82, 255), (196, 236, 224, 255)))
    save_png(os.path.join(BASE, "units", "archer_red.png"), 16, 16, draw_archer((224, 126, 76, 255), (126, 58, 20, 255), (252, 212, 170, 255)))
    save_png(os.path.join(BASE, "units", "knight_blue.png"), 16, 16, draw_knight((112, 132, 210, 255), (34, 52, 110, 255), (216, 224, 246, 255)))
    save_png(os.path.join(BASE, "units", "knight_red.png"), 16, 16, draw_knight((190, 94, 104, 255), (96, 30, 46, 255), (246, 214, 220, 255)))
    save_png(os.path.join(BASE, "units", "catapult_blue.png"), 16, 16, draw_catapult((116, 98, 72, 255), (42, 62, 120, 255), (120, 196, 255, 255)))
    save_png(os.path.join(BASE, "units", "catapult_red.png"), 16, 16, draw_catapult((126, 92, 68, 255), (118, 42, 34, 255), (255, 178, 110, 255)))

    save_png(os.path.join(BASE, "buildings", "barracks.png"), 32, 32, draw_barracks())
    save_png(os.path.join(BASE, "buildings", "archery_range.png"), 32, 32, draw_archery_range())
    save_png(os.path.join(BASE, "buildings", "armory.png"), 32, 32, draw_armory())
    save_png(os.path.join(BASE, "buildings", "siege_workshop.png"), 32, 32, draw_siege_workshop())
    save_png(os.path.join(BASE, "buildings", "forge.png"), 32, 32, draw_forge())
    save_png(os.path.join(BASE, "buildings", "treasury.png"), 32, 32, draw_treasury())
    save_png(os.path.join(BASE, "buildings", "base.png"), 64, 64, draw_tower())
    save_png(os.path.join(BASE, "buildings", "tower_undead.png"), 64, 64, draw_tower_variant((128, 138, 146, 255), (44, 54, 62, 255), (88, 196, 120, 255), (164, 255, 176, 255)))
    save_png(os.path.join(BASE, "buildings", "tower_elf.png"), 64, 64, draw_tower_variant((180, 198, 164, 255), (62, 88, 54, 255), (82, 164, 118, 255), (220, 255, 180, 255)))
    save_png(os.path.join(BASE, "buildings", "tower_orc.png"), 64, 64, draw_tower_variant((154, 136, 108, 255), (72, 52, 32, 255), (198, 68, 46, 255), (255, 188, 116, 255)))
    save_png(os.path.join(BASE, "buildings", "tower_dragon.png"), 64, 64, draw_tower_variant((112, 88, 118, 255), (46, 28, 54, 255), (232, 94, 52, 255), (255, 210, 92, 255)))

    save_png(os.path.join(BASE, "ui", "gold_icon.png"), 16, 16, draw_gold_icon())
    save_png(os.path.join(BASE, "ui", "heart_icon.png"), 16, 16, draw_heart_icon())

    save_png(os.path.join(BASE, "buildings", "grid_cell.png"), 32, 32, draw_grid_cell((0, 0, 0, 0), (80, 80, 80, 200)))
    save_png(os.path.join(BASE, "buildings", "grid_cell_valid.png"), 32, 32, draw_grid_cell((40, 180, 60, 80), (30, 200, 50, 180)))
    save_png(os.path.join(BASE, "buildings", "grid_cell_invalid.png"), 32, 32, draw_grid_cell((200, 50, 50, 80), (220, 40, 40, 180)))

    print("\nDone - 25 sprites generated.")


if __name__ == "__main__":
    generate_all()

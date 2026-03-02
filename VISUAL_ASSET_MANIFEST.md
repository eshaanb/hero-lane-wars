# Visual Asset Manifest — Lane Wars

> Generated from project scan on 2026-03-01
> Engine: Godot 4.6 (C# / .NET) | Viewport: 1280x720

---

## 1. PROJECT OVERVIEW

**Genre:** Lane Defense / Auto-Battler hybrid (1v1 competitive RTS).

**Setting:** A fantasy warfare world where six asymmetric factions — Iron Legion (humans), Expedition Guild (scouts), Undead Court (necromancers), Sylvan Host (elves), Orc Warclans, and Dragon Broods — clash across a single contested lane. Players are "army architects": they place production buildings that automatically spawn units, manage a gold economy, and attempt to destroy the opponent's tower. Matches run 8–15 minutes with deterministic simulation designed for future lockstep multiplayer.

**Current State:** Sprint 1 complete. Four unit types (Footman, Archer, Knight, Catapult), six building types, six race definitions, AI opponent, and full core loop are functional. Existing art is programmer-placeholder pixel art generated via a Python script.

---

## 2. THEMATIC STYLE

**Recommended Style: Clean Pixel Art (16-bit era)** — consistent with the spec directive ("stylized low-poly or clean pixel art — pick one and commit") and the existing placeholder direction.

Specifically:

- **Sharp, hand-pixeled sprites** with visible outlines (1–2px dark stroke) for silhouette readability at the 1280x720 viewport.
- **Limited palette per asset** (6–10 colors plus transparency) to keep sprites tight and readable at small sizes.
- **Top-down perspective** matching the current camera. Units face right (P1) or left (P2); buildings shown from a slight ¾ overhead view.
- **Faction identity through color accent + silhouette shape**, not detail density. Every unit type must read as distinct at 1x zoom within 0.5 seconds.
- **Minimal animation frames for MVP**: idle (1 frame), walk (2–4 frames), attack (2–3 frames), death (2–3 frames). Prioritize readability over smoothness.

---

## 3. COLOR PALETTE

The following five hex codes form the game's visual identity. All faction sprites derive tints from these anchors, with per-faction accent shifts.

| Swatch | Hex | Role |
|--------|-----|------|
| ![#3A5FA8](https://via.placeholder.com/16/3A5FA8/3A5FA8.png) | `#3A5FA8` | **Player 1 Blue** — primary team tint for all P1 units and UI |
| ![#B8382E](https://via.placeholder.com/16/B8382E/B8382E.png) | `#B8382E` | **Player 2 Red** — primary team tint for all P2 units and UI |
| ![#E8C638](https://via.placeholder.com/16/E8C638/E8C638.png) | `#E8C638` | **Gold / Economy** — coins, treasury, income flash, UI currency |
| ![#2A3040](https://via.placeholder.com/16/2A3040/2A3040.png) | `#2A3040` | **Dark Base** — outlines, shadows, HUD backgrounds, lane ground |
| ![#D6CEB8](https://via.placeholder.com/16/D6CEB8/D6CEB8.png) | `#D6CEB8` | **Parchment Neutral** — UI panels, health bar backgrounds, terrain |

---

## 4. SPRITE LIST

### 4.1 Primary Unit Types (3 new / upgraded)

These three units are the highest-priority visual upgrades. Each needs a **blue** and **red** team variant.

#### Swordsman (replaces current Footman placeholder)

> A stocky humanoid warrior viewed top-down, wearing a rounded half-helm and short chainmail tunic, holding a kite shield on the left arm (team-colored emblem on shield face) and a short broadsword raised in the right hand. Boots and belt rendered in dark brown; metal glints on helm and shield rim using a single highlight pixel. 16x16px.

#### Ranger (replaces current Archer placeholder)

> A lean, hooded figure in a knee-length cloak (team-colored hood lining visible), drawing a longbow that extends 2–3 pixels past the body silhouette to the right. A small quiver of arrows visible on the back as a 2px brown rectangle. Stance is angled slightly, one foot forward, conveying motion even when idle. 16x16px.

#### Siege Ram (replaces current Catapult placeholder)

> A wheeled wooden siege engine shown in ¾ top-down view: a rectangular timber frame (dark oak brown) on two visible wheels (dark grey circles), with a suspended battering log (lighter wood with iron-banded tip in team accent color) hanging from the crossbeam. A small pennant flag in team color flies from the rear post. Wider than tall to distinguish from humanoid silhouettes. 16x16px.

### 4.2 Key Structures (2 new)

#### War Academy (new building — Commander unlock, planned Sprint 3)

> A two-story stone keep viewed ¾ overhead, 32x32px. Ground floor has a wide arched entrance (dark interior). Upper floor features crenellated battlements with a single tall banner pole flying a triangular pennant in a neutral cream color (re-tinted per faction at runtime). Walls are medium grey stone-block pattern (2x2px block grid); roof trim is darker slate. A faint orange glow emanates from two narrow window slits on the upper floor, suggesting a war room within.

#### Watch Tower (new defensive structure — planned for lane 2 / scouting)

> A slender, tall circular tower viewed ¾ overhead, 32x32px. Base is 10px wide, tapering to 6px at the top over the full sprite height to create a vertical emphasis distinct from the squat production buildings. A conical peaked roof (dark teal-green) sits at the top with a single beacon flame (2x2px bright orange-yellow) at the apex. Three narrow arrow-slit windows descend the shaft. Stone wall color matches the War Academy for visual cohesion. A small wooden platform ring at mid-height breaks up the silhouette.

### 4.3 UI / Environment Elements (2 new)

#### Lane Terrain Tile (repeating ground)

> A 64x64px seamless-tileable ground texture for the lane battlefield. Trampled dirt path with sparse, flattened grass tufts along the edges. The palette uses 4 colors: dark earth (`#5A4E3A`), mid brown (`#7A6C52`), light dust highlight (`#A89878`), and desaturated green grass accents (`#6E8656`). Subtle horizontal wear lines suggest heavy foot traffic. Must tile cleanly left-to-right with no visible seam, as lanes scroll horizontally.

#### Ability Cooldown Frame (Commander UI — Sprint 3 prep)

> A 48x48px UI frame for commander ability slots. An octagonal stone border (3px wide, dark slate with a single-pixel inner bevel highlight) surrounding a 42x42px interior. The interior is semi-transparent dark (`#2A3040` at 80% opacity) when the ability is ready, and fills bottom-to-top with a desaturated red sweep (`#6E2222` at 60% opacity) during cooldown. The top-left corner has a small diamond-shaped gem inset (4x4px) that glows team-color when the ability is available. Designed to sit in a horizontal row of three (Q / W / E) at the bottom of the HUD.

---

## 5. TECHNICAL SPECS

### 5.1 Sprite Dimensions (established by existing assets)

| Asset Category | Canvas Size | Notes |
|----------------|-------------|-------|
| **Units** | **16x16 px** | All unit sprites. Rendered at 1:1 on a 1280x720 viewport (scale factor 0.8 applied in `UnitRenderer`). Keep all detail within the center 14x14 area; outer ring is padding/transparency. |
| **Buildings (production)** | **32x32 px** | Barracks, Archery Range, Armory, Siege Workshop, Forge, Treasury. Must fit cleanly in the 32px build-zone grid cells. |
| **Towers / Bases** | **64x64 px** | Player base towers and faction-variant towers. Larger canvas for landmark readability. |
| **UI Icons** | **16x16 px** | Gold icon, heart icon, status indicators. |
| **UI Frames** | **48x48 px** | Ability slots, commander portrait frames. |
| **Terrain Tiles** | **64x64 px** | Must be seamless-tileable on the horizontal axis. |

### 5.2 Format & Export

| Property | Value |
|----------|-------|
| **File format** | PNG (RGBA, 8-bit depth) |
| **Transparency** | Required — all sprites use alpha channel |
| **Texture filter** | `canvas_items` (nearest-neighbor) — set in `project.godot`. No anti-aliasing; keep edges pixel-crisp. |
| **Naming convention** | `snake_case` — e.g., `footman_blue.png`, `siege_ram_red.png`, `war_academy.png` |
| **Team variants** | Units require `_blue.png` and `_red.png` variants. Buildings are neutral (tinted at runtime via modulate or shader). |

### 5.3 Animation Specs (future spritesheets)

| Animation | Frames | Layout |
|-----------|--------|--------|
| Idle | 1 | Single sprite (current format) |
| Walk | 2–4 | Horizontal strip, same canvas height |
| Attack | 2–3 | Horizontal strip |
| Death | 2–3 | Horizontal strip |
| **Spritesheet stride** | — | Each frame is the full canvas width (16px for units, 32px for buildings). Total sheet width = `frame_count * canvas_width`. |

### 5.4 Directory Structure

```
lane-wars/assets/sprites/
├── units/
│   ├── footman_blue.png      (existing)
│   ├── footman_red.png       (existing)
│   ├── swordsman_blue.png    ← NEW
│   ├── swordsman_red.png     ← NEW
│   ├── ranger_blue.png       ← NEW
│   ├── ranger_red.png        ← NEW
│   ├── siege_ram_blue.png    ← NEW
│   └── siege_ram_red.png     ← NEW
├── buildings/
│   ├── barracks.png          (existing)
│   ├── war_academy.png       ← NEW
│   └── watch_tower.png       ← NEW
├── ui/
│   ├── gold_icon.png         (existing)
│   ├── heart_icon.png        (existing)
│   └── ability_frame.png     ← NEW
└── environment/
    └── lane_tile.png         ← NEW (new subdirectory)
```

---

## 6. PRIORITY & NEXT STEPS

| Priority | Asset | Reason |
|----------|-------|--------|
| **P0** | Swordsman (blue + red) | Replaces most common unit placeholder; immediate visual lift |
| **P0** | Ranger (blue + red) | Second most spawned unit; needs distinct silhouette from swordsman |
| **P0** | Lane Terrain Tile | Bare lane background is the largest visual gap right now |
| **P1** | Siege Ram (blue + red) | Less frequently seen but important for silhouette variety |
| **P1** | War Academy | Prepares for Sprint 3 commander system |
| **P2** | Watch Tower | Needed for Sprint 2 second-lane expansion |
| **P2** | Ability Cooldown Frame | Sprint 3 commander UI |

---

*This manifest should be treated as a living document. Update it as assets are completed and new needs emerge from Sprint 2+ development.*

# Lane Wars — Game Design Specification

**Version:** 0.1.0
**Author:** Eshaan
**Engine:** Godot 4.x (C#)
**Target Platform:** PC (Steam), with future mobile consideration
**Genre:** Lane Defense / Auto-Battler Hybrid

---

## 1. Game Overview

### 1.1 Elevator Pitch

Lane Wars is a competitive 1v1 (expandable to 2v2) strategy game where players build production structures that automatically spawn units down lanes toward the enemy base. Players choose a faction, manage an income-based economy, and make build-order decisions to overwhelm the opponent. A limited-ability Commander unit gives players real-time agency during combat without overwhelming micro demands.

### 1.2 Core Fantasy

You are an army architect. You don't control individual soldiers — you design the war machine, then watch it execute. Your decisions compound over time, and the tension comes from reading your opponent's composition and adapting your production before it's too late.

### 1.3 Reference Games

- **Castle Fight (WC3):** Core building/spawning loop, faction asymmetry, income system
- **Hero Line Wars (WC3):** Commander unit with abilities, lane defense feel
- **Teamfight Tactics:** Economy management (interest, streaks), adaptation between rounds

### 1.4 Design Pillars

1. **Decisions over mechanics** — winning comes from smart composition choices, not APM
2. **Readable combat** — players should always understand why they're winning or losing
3. **Asymmetric factions** — each faction should feel fundamentally different, not just reskinned
4. **Snowball prevention** — comeback mechanics keep games competitive until the end

---

## 2. Game Flow

### 2.1 Match Structure

```
FACTION SELECT → BUILD PHASE (repeating) → COMBAT (continuous) → MATCH END
```

A match plays out in real-time with no discrete rounds (unlike TFT). Once a building is placed, it begins producing units immediately and continuously. The game ends when one player's base is destroyed.

**Target match length:** 8–15 minutes.

### 2.2 Pre-Game

1. Players queue for a match (or invite for private games)
2. Faction selection phase (30 seconds, simultaneous blind pick)
3. Optional: Ban phase for ranked (each player bans 1 faction)

### 2.3 Core Gameplay Loop (Real-Time)

```
Earn gold (passive income tick every 10 seconds)
    → Decide what to build (production buildings or economy buildings)
    → Place building in your build zone
    → Building spawns units automatically on a timer
    → Units march down the lane and fight enemy units
    → Surviving units that reach the enemy base deal damage to it
    → First base to 0 HP loses
```

### 2.4 Match End Conditions

- **Primary:** Enemy base HP reaches 0
- **Tiebreaker:** If neither base is destroyed after 15 minutes, the base with more HP wins
- **Sudden death:** At 12 minutes, all unit spawn rates increase by 50% to force resolution

---

## 3. Map Layout

### 3.1 Standard Map (1v1)

```
┌─────────────────────────────────────────────────┐
│  [P1 BASE]  [P1 BUILD ZONE]    LANE 1    [P2 BUILD ZONE]  [P2 BASE]  │
│                                                                        │
│  [P1 BASE]  [P1 BUILD ZONE]    LANE 2    [P2 BUILD ZONE]  [P2 BASE]  │
└─────────────────────────────────────────────────┘
```

- **2 lanes** in the standard 1v1 map
- Each player has a **build zone** adjacent to their base per lane
- Units spawn from buildings and walk in a straight path down the lane
- Lanes are independent — units do not cross between lanes
- Each lane has its own base HP pool (e.g., 50 HP per lane, 100 total)

### 3.2 Build Zone

- Grid-based placement area (8 wide × 4 deep per lane)
- Buildings occupy 1x1 or 2x2 grid cells depending on tier
- Buildings can be sold for 50% of their cost
- Buildings can be upgraded in place (costs the difference + a small premium)

### 3.3 Future Maps

- **3-lane map** for 2v2 (shared middle lane)
- **Single-lane map** for fast games (5 minute target)

---

## 4. Economy System

### 4.1 Income

| Source | Amount | Frequency |
|--------|--------|-----------|
| Base income | 10 gold | Every 10 seconds |
| Per production building | +1 gold per building | Every 10 seconds |
| Economy buildings | +3 gold per econ building | Every 10 seconds |
| Unit kill bounty | Varies by unit | On kill |

**Starting gold:** 100
**Starting income:** 10 gold / 10 seconds

### 4.2 Economy Buildings

Each faction has an economy building that generates extra income but produces no units. This creates the classic RTS tension: invest in economy early for a stronger late game, or build units immediately to pressure.

### 4.3 Interest (Stretch Goal)

Optional TFT-inspired mechanic: earn +1 bonus gold per 10 gold saved (max +5 bonus). This rewards banking gold and creates interesting timing decisions. **Flag for playtesting — may overcomplicate the core loop.**

### 4.4 Building Costs

Buildings should follow an exponential cost curve:

| Tier | Approximate Cost | Unit Power Level |
|------|-----------------|------------------|
| T1 | 20-30 gold | Basic units |
| T2 | 50-80 gold | Mid-tier units |
| T3 | 120-180 gold | Elite units |
| T4 (ultimate) | 300+ gold | Game-changers, 1 per player limit |

---

## 5. Unit System

### 5.1 Unit Behavior

All units are **fully autonomous**. Once spawned, they:

1. Walk down their lane toward the enemy base at their movement speed
2. Engage the nearest enemy unit when within attack range
3. Fight until one side is dead
4. Surviving units continue marching
5. Units that reach the enemy base deal **leak damage** equal to their remaining HP percentage (a full-HP T3 unit leaks harder than a half-dead T1)

### 5.2 Unit Stats

Every unit has:

| Stat | Description |
|------|-------------|
| HP | Health points |
| Damage | Attack damage per hit |
| Attack Speed | Seconds between attacks |
| Move Speed | Lane movement speed |
| Range | Melee (0) or ranged (units) |
| Armor Type | Light / Medium / Heavy / Magical |
| Damage Type | Physical / Piercing / Siege / Magic |
| Spawn Time | Seconds between spawns from the building |

### 5.3 Damage Type Matrix

This creates a rock-paper-scissors layer where scouting and adaptation matter:

| Damage \ Armor | Light | Medium | Heavy | Magical |
|----------------|-------|--------|-------|---------|
| Physical | 100% | 100% | 75% | 100% |
| Piercing | 150% | 75% | 100% | 100% |
| Siege | 100% | 100% | 150% | 75% |
| Magic | 75% | 100% | 100% | 150% |

### 5.4 Unit Abilities (Select Units Only)

Most units are stat-sticks — they just attack. A subset of T2+ units have one passive or active ability:

- **Passive examples:** Aura (+10% damage to nearby allies), Regeneration, Thorns
- **Active examples (auto-cast):** Heal (targets lowest HP ally), War Cry (AoE buff on spawn), Shield (absorbs X damage)

Keep ability count low for readability. If a player can't understand why they lost a fight at a glance, there are too many abilities.

---

## 6. Commander System

### 6.1 Overview

Each player controls one **Commander** unit that exists on the battlefield. The Commander does NOT auto-attack and is NOT meant to be a primary damage dealer. Instead, the Commander has **3 abilities** with meaningful cooldowns that let the player influence fights at key moments.

The Commander cannot die — if reduced to 0 HP, it retreats to base and is unavailable for 15 seconds (punishment for poor positioning).

### 6.2 Commander Abilities (Per Faction)

Each faction's Commander has a thematic kit. Example (generic starter):

| Ability | Effect | Cooldown |
|---------|--------|----------|
| **Rally** | Buff: +20% move speed and attack speed to nearby allies for 5s | 30s |
| **Barricade** | Place a temporary wall that blocks a lane section for 4s | 45s |
| **Artillery Strike** | Deal AoE damage to a target area after 1.5s delay | 60s |

### 6.3 Commander Design Rules

- Abilities should **influence** fights, not **decide** them
- No ability should one-shot any unit
- Cooldowns should be long enough that each use feels significant
- The Commander should need to be positioned in a lane, creating a choice of which lane to support
- Commander abilities should be visually loud and readable

---

## 7. Factions

### 7.1 Launch Factions (4 minimum for prototype)

Each faction has 6-8 production buildings organized into a simple tech tree.

#### Faction 1: The Iron Legion (Balanced / Beginner-Friendly)

**Theme:** Disciplined human military. Straightforward units, no gimmicks.

| Building | Cost | Unit | Type | Notes |
|----------|------|------|------|-------|
| Barracks | 25 | Footman | Melee, Medium armor, Physical | Bread and butter |
| Archery Range | 30 | Archer | Ranged, Light armor, Piercing | Glass cannon |
| Armory | 50 | Knight | Melee, Heavy armor, Physical | Tanky frontline |
| Siege Workshop | 80 | Catapult | Ranged, Heavy armor, Siege | Slow, high damage |
| War Academy | 120 | Paladin | Melee, Heavy armor, Physical | Has Heal aura |
| Cathedral | 150 | War Priest | Ranged, Magical armor, Magic | Heals + damages |
| Treasury | 40 | (none) | Economy building | +3 income |

**Commander — The General:**
- Rally: AoE attack speed buff
- Shield Wall: Reduces damage taken by units in an area
- Siege Volley: AoE physical damage

#### Faction 2: The Hive (Swarm / Quantity over Quality)

**Theme:** Insectoid swarm. Cheap, fast units that overwhelm through numbers.

| Building | Cost | Unit | Type | Notes |
|----------|------|------|------|-------|
| Hatchery | 15 | Drone | Melee, Light armor, Physical | Very cheap, very fast spawn |
| Spitter Nest | 25 | Spitter | Ranged, Light armor, Piercing | Weak but fast |
| Warren | 45 | Broodling | Melee, Medium armor, Physical | Spawns 2 per cycle |
| Acid Pool | 70 | Acid Crawler | Melee, Light armor, Magic | Damages on death (AoE) |
| Cocoon | 120 | Behemoth | Melee, Heavy armor, Siege | Slow, massive HP |
| Spawning Pit | 160 | Swarm Queen | Ranged, Magical armor, Magic | Spawns mini-units |
| Feeding Ground | 30 | (none) | Economy building | +3 income |

**Commander — The Overmind:**
- Frenzy: Massive speed boost to all units for 3s
- Burrow: Teleport Commander to any point in your lanes
- Spawn Swarm: Instantly spawn a wave of temporary drones

#### Faction 3: The Arcanum (Magic / Control)

**Theme:** Wizard's tower. Fewer, more powerful units with abilities.

| Building | Cost | Unit | Type | Notes |
|----------|------|------|------|-------|
| Apprentice Hall | 30 | Apprentice | Ranged, Magical armor, Magic | Basic caster |
| Golem Forge | 50 | Stone Golem | Melee, Heavy armor, Physical | Tanky, slow |
| Enchantment Spire | 80 | Enchantress | Ranged, Magical armor, Magic | Slows enemies |
| Summoning Circle | 100 | Elemental | Melee, Medium armor, Magic | Randomized element |
| Arcane Library | 150 | Archmage | Ranged, Magical armor, Magic | AoE damage ability |
| Void Gate | 200 | Void Walker | Melee, Magical armor, Magic | Teleports past frontline |
| Crystal Mine | 45 | (none) | Economy building | +3 income |

**Commander — The Archmage:**
- Frost Nova: AoE slow + damage
- Mana Shield: Absorb damage for allied units in area
- Polymorph: Temporarily disable one enemy unit (long cooldown)

#### Faction 4: The Horde (Aggression / Snowball)

**Theme:** Orc warband. Strong early game, rewards aggression.

| Building | Cost | Unit | Type | Notes |
|----------|------|------|------|-------|
| War Camp | 20 | Grunt | Melee, Medium armor, Physical | Solid early unit |
| Axe Thrower Lodge | 30 | Axe Thrower | Ranged, Medium armor, Physical | Short range, high DPS |
| Beast Pen | 55 | War Rider | Melee, Light armor, Physical | Fast, high damage |
| Totem Pole | 70 | Shaman | Ranged, Magical armor, Magic | Buffs nearby allies |
| Blood Pit | 130 | Berserker | Melee, Light armor, Physical | Gets stronger at low HP |
| War Altar | 170 | Warchief | Melee, Heavy armor, Physical | AoE war cry on spawn |
| Plunder Cache | 35 | (none) | Economy building | +3 income |

**Commander — The Warlord:**
- War Cry: AoE damage + attack buff
- Blood Rage: Sacrifice Commander HP to empower units
- Charge: Dash forward, pushing enemy units back

### 7.2 Faction Design Rules

- Every faction needs at least one answer to heavy armor, light armor, and magical armor
- Every faction needs at least one ranged option
- No faction should have a "correct" build order — multiple viable openers
- T1 units should remain relevant late game through sheer numbers (no unit should become completely obsolete)

---

## 8. Tech Tree Structure

### 8.1 Building Prerequisites

Simple branching tree, not deep chains:

```
        T1 Buildings (no prereqs)
       /          |           \
    T2 Buildings (require 1 T1 of same type)
       \          |           /
        T3 Buildings (require 1 T2)
              |
        T4 Ultimate (require 2 different T3s, limit 1)
```

### 8.2 Upgrade System

Each production building can be upgraded once (costs ~60% of the original building cost):

- **Upgrade effect:** Spawned units get +15% HP and +15% damage, AND spawn rate decreases by 10% (faster spawns)
- Upgrades are per-building, not global
- Creates a decision: build more buildings, or upgrade existing ones?

---

## 9. Scouting and Information

### 9.1 Fog of War

- Players can always see the **lane** (all unit combat is visible)
- Players **cannot** see the enemy's **build zone** by default
- The Commander can move to the middle of the lane to reveal the enemy build zone briefly (risk/reward — Commander is exposed)

### 9.2 Scoreboard

Always visible:

- Enemy base HP per lane
- Your income per tick
- Number of enemy buildings (but not which ones, unless scouted)
- Kill/leak counter

---

## 10. Visual Design

### 10.1 Camera

- **Top-down or slight isometric angle** (30–45 degrees)
- Fixed camera per lane with ability to toggle between lanes (Tab key)
- Minimap showing both lanes with unit density indicators
- Zoom in/out with scroll wheel

### 10.2 Art Direction

- **Stylized low-poly or clean pixel art** — pick one and commit
- Unit silhouettes must be instantly distinguishable by type
- Color-coding: Player 1 = blue tint, Player 2 = red tint on all units
- Health bars above all units
- Income tick should have a satisfying visual/audio cue (coins, particles)
- Damage numbers are optional but helpful (toggle in settings)

### 10.3 UI Layout

```
┌─────────────────────────────────────────┐
│ [Lane 1 View]              [Minimap]    │
│                             [Scores]    │
│                                         │
│                                         │
├─────────────────────────────────────────┤
│ [Gold: 150]  [Income: 18/tick]          │
│ [Building Panel — faction buildings]    │
│ [Commander Abilities — Q W E]           │
└─────────────────────────────────────────┘
```

---

## 11. Technical Architecture

### 11.1 Project Structure (Godot 4 / C#)

```
lane-wars/
├── project.godot
├── src/
│   ├── Core/
│   │   ├── GameManager.cs          # Match state, win conditions, timing
│   │   ├── EconomyManager.cs       # Gold, income ticks, transactions
│   │   ├── LaneManager.cs          # Per-lane unit tracking, leak detection
│   │   └── TurnClock.cs            # 10-second income tick timer
│   ├── Units/
│   │   ├── UnitBase.cs             # Base class: HP, damage, movement, combat
│   │   ├── UnitStats.cs            # Data container for unit stats
│   │   ├── UnitFactory.cs          # Spawns units from building data
│   │   ├── CombatResolver.cs       # Damage calculation, armor/damage types
│   │   └── UnitAbilities/
│   │       ├── IAbility.cs         # Ability interface
│   │       ├── HealAura.cs
│   │       ├── DeathExplosion.cs
│   │       └── ...
│   ├── Buildings/
│   │   ├── BuildingBase.cs         # Base class: cost, spawn timer, upgrade
│   │   ├── BuildingData.cs         # Scriptable resource for building definitions
│   │   ├── BuildingPlacer.cs       # Grid placement, validation, ghost preview
│   │   ├── BuildZone.cs            # Grid state, occupied cells
│   │   └── ProductionQueue.cs      # Spawn timing per building
│   ├── Commander/
│   │   ├── CommanderController.cs  # Movement, ability usage, retreat
│   │   ├── CommanderAbility.cs     # Base ability class with cooldown
│   │   └── Abilities/
│   │       ├── RallyAbility.cs
│   │       ├── BarricadeAbility.cs
│   │       └── ...
│   ├── Factions/
│   │   ├── FactionData.cs          # Scriptable resource: buildings list, commander
│   │   └── FactionRegistry.cs      # All available factions
│   ├── AI/
│   │   ├── AIController.cs         # Decision-making loop
│   │   ├── BuildOrderStrategy.cs   # Predefined and adaptive build orders
│   │   └── AIPersonality.cs        # Aggro, econ, balanced profiles
│   ├── Networking/
│   │   ├── NetworkManager.cs       # Connection, lobby, matchmaking
│   │   ├── InputSynchronizer.cs    # Lockstep input sync
│   │   ├── GameStateHash.cs        # Desync detection
│   │   └── ReplayRecorder.cs       # Record inputs for replay
│   └── UI/
│       ├── HUD.cs                  # Gold, income, scores, timer
│       ├── BuildingPanel.cs        # Building selection and placement
│       ├── CommanderAbilityBar.cs  # Q/W/E cooldown display
│       ├── Minimap.cs              # Lane overview
│       ├── EndGameScreen.cs        # Victory/defeat, stats
│       └── FactionSelectScreen.cs  # Pre-game faction pick
├── data/
│   ├── factions/
│   │   ├── iron_legion.tres        # Faction resource files
│   │   ├── hive.tres
│   │   ├── arcanum.tres
│   │   └── horde.tres
│   ├── units/                      # Unit stat definitions
│   └── buildings/                  # Building stat definitions
├── scenes/
│   ├── Main.tscn                   # Entry point
│   ├── Game.tscn                   # Match scene
│   ├── Menus/
│   │   ├── MainMenu.tscn
│   │   ├── FactionSelect.tscn
│   │   └── Settings.tscn
│   ├── Lane.tscn                   # Reusable lane scene
│   ├── BuildZone.tscn
│   └── UI/
│       ├── HUD.tscn
│       └── BuildingPanel.tscn
├── assets/
│   ├── sprites/
│   ├── audio/
│   └── fonts/
└── tests/
    ├── TestCombatResolver.cs
    ├── TestEconomy.cs
    └── TestUnitPathing.cs
```

### 11.2 Data-Driven Design

All unit stats, building costs, faction data, and balance values should be defined in **resource files** (Godot .tres or JSON), NOT hardcoded. This is critical for iteration speed.

Example unit definition (JSON for readability, implement as Godot Resources):

```json
{
  "id": "iron_legion_footman",
  "display_name": "Footman",
  "hp": 120,
  "damage": 15,
  "attack_speed": 1.0,
  "move_speed": 80,
  "range": 0,
  "armor_type": "medium",
  "damage_type": "physical",
  "spawn_time": 4.0,
  "abilities": [],
  "sprite": "res://assets/sprites/units/footman.png"
}
```

### 11.3 Core Systems Implementation Priority

**Sprint 1 — Minimum Playable (Weeks 1-2):**
1. Grid-based build zone with click-to-place
2. One building type that spawns units on a timer
3. Units walk down a lane (simple linear path)
4. Basic combat: units stop, attack nearest enemy, one dies
5. Base HP that decreases when units reach it
6. Gold counter with passive income ticks

**Sprint 2 — Playable Game (Weeks 3-4):**
1. Multiple building types with different unit stats
2. Damage type / armor type matrix
3. Building upgrade system
4. Economy buildings
5. Win/loss condition and restart

**Sprint 3 — Commander + Second Lane (Weeks 5-6):**
1. Commander unit with movement between lanes
2. Three commander abilities with cooldowns
3. Second lane
4. Lane switching UI

**Sprint 4 — Faction System (Weeks 7-8):**
1. Faction data structure and loading
2. Implement 2 factions with full building rosters
3. Faction select screen
4. Tech tree prerequisites

**Sprint 5 — AI Opponent (Weeks 9-10):**
1. Basic AI that builds on a timer
2. AI personality profiles (rush, econ, balanced)
3. AI commander ability usage

**Sprint 6 — Polish + 2 More Factions (Weeks 11-14):**
1. Complete 4 factions
2. Balance pass (spreadsheet-driven)
3. Visual polish, particles, screen shake
4. Sound effects, music
5. Settings menu, keybindings

**Sprint 7 — Networking (Weeks 15-22):**
1. Lockstep networking implementation
2. Lobby system
3. Desync detection
4. Replay system
5. Matchmaking (basic ELO)

### 11.4 Networking Strategy

**Lockstep deterministic simulation** is recommended:

- Both clients run the same simulation
- Only player **inputs** are sent over the network (building placed, ability used, commander moved)
- Inputs are batched into "ticks" (e.g., every 100ms)
- Both clients execute the same inputs on the same tick
- Periodic state hash comparison to detect desync

**Why lockstep over client-server:**
- Far less bandwidth (inputs only, not full game state)
- No authority server needed (peer-to-peer works)
- Natural replay system (just record inputs)
- Well-suited for RTS with many units

**Lockstep requirements:**
- **Deterministic math:** Use fixed-point arithmetic, no floats for game logic
- **Deterministic iteration:** Ordered collections only (no HashSet, no Dictionary iteration)
- **Deterministic random:** Seeded RNG shared between clients
- **Separate render from simulation:** Visual interpolation on top of deterministic tick

### 11.5 Key Godot Patterns

- Use **Godot Resources** (.tres) for all data definitions (stats, costs, faction configs)
- Use **signals** for decoupled communication (unit died, building placed, income tick)
- Use **scene composition** over deep inheritance (Unit scene = Sprite + Hitbox + HealthBar + MovementComponent)
- Use **autoloads** sparingly — GameManager and maybe AudioManager
- Keep **rendering separate from game logic** for future networking

---

## 12. Balance Framework

### 12.1 Balance Spreadsheet

Maintain a spreadsheet that calculates:

- **Gold efficiency:** (unit total stats) / (building cost) per spawn
- **DPS per gold:** damage output relative to investment
- **Effective HP per gold:** survivability relative to investment
- **Time to ROI:** how many spawn cycles before a building "pays for itself" in combat value vs. income buildings

### 12.2 Balance Levers

When something is too strong or too weak, adjust in this priority order:

1. **Cost** (easiest, least disruptive)
2. **Spawn time** (affects DPS output without changing unit feel)
3. **HP / Damage** (changes unit identity more)
4. **Abilities** (last resort, highest complexity)

### 12.3 Matchup Tracking

Track win rates per faction matchup. Target: no matchup should exceed 55/45 win rate at equal skill. Asymmetry is fine as long as every faction has viable counterplay.

---

## 13. Monetization (Post-Launch Considerations)

### 13.1 Model

- **Base game:** $9.99–$14.99 on Steam Early Access
- **Cosmetic DLC:** Unit skins, building skins, commander skins, lane themes
- **NO pay-to-win:** No faction or gameplay advantage for purchase
- **Future faction DLC:** New factions at $2.99–$4.99 each (balance carefully)

### 13.2 Retention Features

- Ranked ladder with seasonal resets
- Match history and stats tracking
- Replay sharing
- Daily/weekly challenges
- Faction mastery progression (cosmetic rewards)

---

## 14. Development Milestones

| Milestone | Target | Deliverable |
|-----------|--------|-------------|
| **Prototype** | Week 6 | 1 lane, 1 faction, colored rectangles, playable core loop |
| **Alpha** | Week 14 | 2 lanes, 4 factions, AI opponent, commander system |
| **Closed Beta** | Week 22 | Multiplayer, basic matchmaking, visual polish |
| **Early Access** | Week 30+ | Steam release, ranked ladder, 4+ factions |

---

## 15. Open Design Questions

These need to be resolved through playtesting:

1. **Round-based vs continuous?** Current spec is continuous (units always spawning). Alternative: discrete rounds like Castle Fight where you build, then watch a fight, then build again. Continuous feels more engaging but harder to balance.

2. **Interest mechanic — in or out?** Banking gold for interest adds depth but may be too complex for the lane defense audience. Playtest both.

3. **Lane count:** 2 lanes feels right for 1v1 but needs testing. 1 lane may be too linear. 3 lanes may spread attention too thin for a game about composition decisions.

4. **Commander power level:** How impactful should abilities be? Too weak and the Commander feels pointless. Too strong and it becomes a MOBA. Target: Commander should swing close fights, not win lost ones.

5. **Leak damage formula:** Current proposal is remaining HP percentage. Alternative: flat damage per unit type. Percentage rewards keeping units alive; flat rewards unit count (favoring swarm).

6. **Building destruction:** Can players destroy enemy buildings with special abilities or unit types? This adds a layer but may feel unfun. Consider for post-launch.

---

## 16. Claude Code / Codex Usage Notes

### 16.1 How to Use This Spec with AI Coding Tools

When working with Claude Code or Codex, reference specific sections:

- **"Implement the economy system as described in Section 4"**
- **"Create the UnitBase class following the architecture in Section 11.1"**
- **"Build the combat resolver using the damage matrix from Section 5.3"**

### 16.2 Suggested Prompting Strategy

Break work into single-system tasks:

1. "Set up the Godot 4 C# project structure as outlined in Section 11.1"
2. "Implement EconomyManager.cs — passive income ticks every 10 seconds, building income bonuses, gold transactions with validation"
3. "Create the BuildZone grid system — 8x4 grid, click to place buildings, ghost preview, collision detection with existing buildings"
4. "Implement UnitBase with movement along a lane, stopping to fight the nearest enemy, and basic HP/damage combat"
5. "Create the CombatResolver using the damage type matrix from Section 5.3"

### 16.3 Testing Strategy

Every core system should have unit tests:

- **Economy:** Does income tick correctly? Can you overspend? Does building income stack?
- **Combat:** Does the damage matrix apply correctly? Do units target correctly?
- **Buildings:** Do prerequisites work? Can you upgrade? Does selling refund correctly?
- **Win condition:** Does leak damage apply? Does the game end at 0 HP?

### 16.4 Data Files First

Before implementing systems, create the data files (JSON or .tres) for all units and buildings. This lets you iterate on balance without touching code.

---

## Appendix A: Keybindings

| Key | Action |
|-----|--------|
| Tab | Switch lane view |
| Q / W / E | Commander abilities |
| Right-click | Move Commander |
| Left-click | Select building to place / confirm placement |
| Escape | Cancel placement |
| 1–8 | Quick-select building from panel |
| Space | Center camera on Commander |
| Delete | Sell selected building |

## Appendix B: Audio Cues

| Event | Sound |
|-------|-------|
| Income tick | Coin jingle |
| Building placed | Construction thud |
| Building upgraded | Anvil + level-up chime |
| Unit spawned | Faction-specific (march, screech, arcane hum, war horn) |
| Unit killed | Impact + fade |
| Leak (unit reaches base) | Warning alarm + base damage thud |
| Commander ability | Distinct per ability |
| Victory | Triumphant fanfare |
| Defeat | Somber tone |

## Appendix C: Glossary

| Term | Definition |
|------|------------|
| **Leak** | When an enemy unit reaches your base and deals damage |
| **Income** | Gold earned per tick (base + buildings) |
| **Build zone** | Grid area where players place production buildings |
| **Commander** | Player-controlled hero unit with 3 abilities |
| **Spawn time** | Interval between unit spawns from a building |
| **Tech tree** | Building prerequisite chain |
| **Gold efficiency** | Combat value per gold spent |

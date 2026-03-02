using Godot;
using System.Collections.Generic;
using LaneWars.Units;

namespace LaneWars.Core;

public partial class LaneRenderer : Node2D
{
    private struct EngagementLine
    {
        public Vector2 Start;
        public Vector2 End;
        public Color Color;
    }

    private struct ProjectileVisual
    {
        public Vector2 Start;
        public Vector2 End;
        public Color Color;
        public float Lifetime;
        public float Duration;
        public float Radius;
    }

    private PackedScene _unitScene = null!;
    private Node2D _unitsContainer = null!;
    private readonly Dictionary<int, UnitRenderer> _renderers = new();
    private MatchSimulation? _sim;

    // Visual constants
    private const float PixelsPerUnit = 1.0f; // 1 sim unit = 1 pixel (adjust as needed)
    private const float LaneY = 360.0f; // Lane center Y position
    private const int CrowdSeparationUnits = 60;
    private const int MaxRowsPerCluster = 3;
    private const float TeamYOffset = 8.0f;
    private const float RowSpacingPx = 14.0f;
    private readonly List<EngagementLine> _engagementLines = new();
    private readonly List<ProjectileVisual> _projectiles = new();
    private readonly Dictionary<int, int> _lastUnitAttackMs = new();
    private readonly int[] _lastTowerAttackMs = new int[2];

    public void Initialize(MatchSimulation sim)
    {
        _sim = sim;
        _unitScene = GD.Load<PackedScene>("res://scenes/Unit.tscn");
        _unitsContainer = GetNode<Node2D>("Units");
        QueueRedraw();
    }

    public override void _Draw()
    {
        float laneStartX = 200.0f;
        float laneEndX = laneStartX + 1000 * 0.8f; // 1000 units * PixelsPerUnit
        float laneY = 360.0f;
        float laneHeight = 50.0f;

        // Lane background
        DrawRect(new Rect2(laneStartX, laneY - laneHeight / 2, laneEndX - laneStartX, laneHeight),
            new Color(0.18f, 0.18f, 0.24f, 0.6f));
        // Lane border lines
        DrawLine(new Vector2(laneStartX, laneY - laneHeight / 2),
            new Vector2(laneEndX, laneY - laneHeight / 2), new Color(0.4f, 0.4f, 0.5f), 1);
        DrawLine(new Vector2(laneStartX, laneY + laneHeight / 2),
            new Vector2(laneEndX, laneY + laneHeight / 2), new Color(0.4f, 0.4f, 0.5f), 1);

        for (int i = 0; i < _engagementLines.Count; i++)
        {
            var line = _engagementLines[i];
            DrawLine(line.Start, line.End, line.Color, 2);
        }

        for (int i = 0; i < _projectiles.Count; i++)
        {
            var projectile = _projectiles[i];
            float progress = projectile.Duration > 0.0f
                ? 1.0f - projectile.Lifetime / projectile.Duration
                : 1.0f;
            progress = Mathf.Clamp(progress, 0.0f, 1.0f);

            Vector2 pos = projectile.Start.Lerp(projectile.End, progress);
            DrawCircle(pos, projectile.Radius, projectile.Color);
        }
    }

    public override void _Process(double delta)
    {
        if (_projectiles.Count == 0)
            return;

        bool changed = false;
        for (int i = _projectiles.Count - 1; i >= 0; i--)
        {
            var projectile = _projectiles[i];
            projectile.Lifetime -= (float)delta;
            if (projectile.Lifetime <= 0.0f)
            {
                _projectiles.RemoveAt(i);
                changed = true;
                continue;
            }

            _projectiles[i] = projectile;
            changed = true;
        }

        if (changed)
            QueueRedraw();
    }

    public void SyncFromSim(MatchSimulation sim)
    {
        var units = sim.Units;
        var aliveIds = new HashSet<int>(); // OK for rendering layer, not sim
        var visualOffsets = CalculateVisualOffsets(units);
        var visualPositions = new Dictionary<int, Vector2>(units.Count);
        _engagementLines.Clear();

        // Update or create renderers
        for (int i = 0; i < units.Count; i++)
        {
            var unit = units[i];
            aliveIds.Add(unit.UnitId);

            if (!_renderers.ContainsKey(unit.UnitId))
            {
                // Spawn new renderer
                var renderer = _unitScene.Instantiate<UnitRenderer>();
                renderer.Initialize(unit);
                _unitsContainer.AddChild(renderer);
                _renderers[unit.UnitId] = renderer;
            }

            Vector2 offset = visualOffsets.TryGetValue(unit.UnitId, out var foundOffset) ? foundOffset : Vector2.Zero;
            visualPositions[unit.UnitId] = UnitRenderer.CalculateScreenPosition(unit.PositionX, offset);
            _renderers[unit.UnitId].SyncFromSim(unit, sim.LaneLengthUnits, offset);
        }

        SpawnAttackProjectiles(units, visualPositions);
        SpawnTowerProjectiles(sim, visualPositions);
        BuildEngagementLines(units, visualPositions);
        BuildTowerTargetLines(sim, visualPositions);
        QueueRedraw();

        // Remove dead unit renderers
        var toRemove = new List<int>();
        foreach (var kvp in _renderers)
        {
            if (!aliveIds.Contains(kvp.Key))
                toRemove.Add(kvp.Key);
        }
        foreach (var id in toRemove)
        {
            _renderers[id].QueueFree();
            _renderers.Remove(id);
        }

        var currentIds = new HashSet<int>(aliveIds);
        var staleIds = new List<int>();
        foreach (var kvp in _lastUnitAttackMs)
        {
            if (!currentIds.Contains(kvp.Key))
                staleIds.Add(kvp.Key);
        }
        for (int i = 0; i < staleIds.Count; i++)
            _lastUnitAttackMs.Remove(staleIds[i]);
    }

    private void SpawnAttackProjectiles(IReadOnlyList<UnitState> units, Dictionary<int, Vector2> visualPositions)
    {
        for (int i = 0; i < units.Count; i++)
        {
            var unit = units[i];
            int lastAttackMs = _lastUnitAttackMs.TryGetValue(unit.UnitId, out var lastSeen) ? lastSeen : 0;

            if (unit.RecentAttackMs > 0 &&
                lastAttackMs == 0 &&
                unit.TargetUnitId != -1 &&
                visualPositions.TryGetValue(unit.UnitId, out var start) &&
                visualPositions.TryGetValue(unit.TargetUnitId, out var end))
            {
                bool isRanged = unit.Range > 0;
                SpawnProjectile(
                    start,
                    end,
                    isRanged
                        ? (unit.OwnerPlayer == 0 ? new Color(0.55f, 0.95f, 0.85f, 1.0f) : new Color(1.0f, 0.7f, 0.45f, 1.0f))
                        : new Color(1.0f, 0.95f, 0.75f, 0.9f),
                    isRanged ? 0.18f : 0.08f,
                    isRanged ? 3.0f : 2.0f);
            }

            _lastUnitAttackMs[unit.UnitId] = unit.RecentAttackMs;
        }
    }

    private void SpawnTowerProjectiles(MatchSimulation sim, Dictionary<int, Vector2> visualPositions)
    {
        for (int tower = 0; tower < 2; tower++)
        {
            int recentAttackMs = sim.GetTowerRecentAttackMs(tower);
            if (recentAttackMs > 0 && _lastTowerAttackMs[tower] == 0)
            {
                int targetUnitId = sim.GetTowerTargetUnitId(tower);
                if (targetUnitId != -1 && visualPositions.TryGetValue(targetUnitId, out var end))
                {
                    Vector2 start = tower == 0
                        ? new Vector2(200.0f, LaneY)
                        : new Vector2(200.0f + sim.LaneLengthUnits * 0.8f, LaneY);

                    SpawnProjectile(
                        start,
                        end,
                        tower == 0 ? new Color(0.55f, 0.9f, 1.0f, 1.0f) : new Color(1.0f, 0.75f, 0.4f, 1.0f),
                        0.2f,
                        4.0f);
                }
            }

            _lastTowerAttackMs[tower] = recentAttackMs;
        }
    }

    private void SpawnProjectile(Vector2 start, Vector2 end, Color color, float duration, float radius)
    {
        _projectiles.Add(new ProjectileVisual
        {
            Start = start,
            End = end,
            Color = color,
            Lifetime = duration,
            Duration = duration,
            Radius = radius
        });
    }

    private void BuildEngagementLines(IReadOnlyList<UnitState> units, Dictionary<int, Vector2> visualPositions)
    {
        var seenPairs = new HashSet<long>();

        for (int i = 0; i < units.Count; i++)
        {
            var unit = units[i];
            if (unit.TargetUnitId == -1)
                continue;
            if (!visualPositions.TryGetValue(unit.UnitId, out var start))
                continue;
            if (!visualPositions.TryGetValue(unit.TargetUnitId, out var end))
                continue;

            int minId = unit.UnitId < unit.TargetUnitId ? unit.UnitId : unit.TargetUnitId;
            int maxId = unit.UnitId > unit.TargetUnitId ? unit.UnitId : unit.TargetUnitId;
            long pairKey = ((long)minId << 32) | (uint)maxId;
            if (!seenPairs.Add(pairKey))
                continue;

            Color lineColor = unit.OwnerPlayer == 0
                ? new Color(0.55f, 0.75f, 1.0f, 0.55f)
                : new Color(1.0f, 0.55f, 0.55f, 0.55f);

            _engagementLines.Add(new EngagementLine
            {
                Start = start,
                End = end,
                Color = lineColor
            });
        }
    }

    private void BuildTowerTargetLines(MatchSimulation sim, Dictionary<int, Vector2> visualPositions)
    {
        AddTowerTargetLine(sim.GetTowerTargetUnitId(0), new Vector2(200.0f, LaneY), new Color(0.5f, 0.9f, 1.0f, 0.7f), visualPositions);
        AddTowerTargetLine(sim.GetTowerTargetUnitId(1), new Vector2(200.0f + sim.LaneLengthUnits * 0.8f, LaneY), new Color(1.0f, 0.7f, 0.4f, 0.7f), visualPositions);
    }

    private void AddTowerTargetLine(int targetUnitId, Vector2 towerPosition, Color color, Dictionary<int, Vector2> visualPositions)
    {
        if (targetUnitId == -1)
            return;
        if (!visualPositions.TryGetValue(targetUnitId, out var targetPos))
            return;

        _engagementLines.Add(new EngagementLine
        {
            Start = towerPosition,
            End = targetPos,
            Color = color
        });
    }

    private static Dictionary<int, Vector2> CalculateVisualOffsets(IReadOnlyList<UnitState> units)
    {
        var offsets = new Dictionary<int, Vector2>(units.Count);

        for (int owner = 0; owner <= 1; owner++)
        {
            var ownerUnits = new List<UnitState>();
            for (int i = 0; i < units.Count; i++)
            {
                if (units[i].OwnerPlayer == owner)
                    ownerUnits.Add(units[i]);
            }

            ownerUnits.Sort((a, b) =>
            {
                int cmp = a.PositionX.CompareTo(b.PositionX);
                return cmp != 0 ? cmp : a.UnitId.CompareTo(b.UnitId);
            });

            int lastPosition = int.MinValue;
            int slotInCluster = 0;
            for (int i = 0; i < ownerUnits.Count; i++)
            {
                var unit = ownerUnits[i];
                if (unit.PositionX - lastPosition > CrowdSeparationUnits)
                    slotInCluster = 0;

                int row = slotInCluster % MaxRowsPerCluster;
                float directionSign = owner == 0 ? -1.0f : 1.0f;

                float yOffset = directionSign * (TeamYOffset + row * RowSpacingPx);

                offsets[unit.UnitId] = new Vector2(0.0f, yOffset);

                slotInCluster++;
                lastPosition = unit.PositionX;
            }
        }

        return offsets;
    }
}

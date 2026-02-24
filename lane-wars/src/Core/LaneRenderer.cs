using Godot;
using System.Collections.Generic;
using LaneWars.Units;

namespace LaneWars.Core;

public partial class LaneRenderer : Node2D
{
    private PackedScene _unitScene = null!;
    private Node2D _unitsContainer = null!;
    private readonly Dictionary<int, UnitRenderer> _renderers = new();
    private MatchSimulation? _sim;

    // Visual constants
    private const float PixelsPerUnit = 1.0f; // 1 sim unit = 1 pixel (adjust as needed)
    private const float LaneY = 360.0f; // Lane center Y position

    public void Initialize(MatchSimulation sim)
    {
        _sim = sim;
        _unitScene = GD.Load<PackedScene>("res://scenes/Unit.tscn");
        _unitsContainer = GetNode<Node2D>("Units");
    }

    public void SyncFromSim(MatchSimulation sim)
    {
        var units = sim.Units;
        var aliveIds = new HashSet<int>(); // OK for rendering layer, not sim

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

            _renderers[unit.UnitId].SyncFromSim(unit, sim.LaneLengthUnits);
        }

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
    }
}

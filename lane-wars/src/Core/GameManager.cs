using Godot;
using System.Collections.Generic;
using LaneWars.AI;
using LaneWars.Buildings;
using LaneWars.Units;
using LaneWars.UI;

namespace LaneWars.Core;

public partial class GameManager : Node2D
{
    [Export] public GameConfig Config { get; set; } = null!;
    [Export] public BuildingData BarracksData { get; set; } = null!;

    private MatchSimulation _sim = null!;
    private double _accumulator;

    // References set in _Ready via GetNode
    private LaneRenderer _laneRenderer = null!;
    private BuildZoneRenderer _p1BuildZone = null!;
    private BuildZoneRenderer _p2BuildZone = null!;
    private HUD _hud = null!;
    private BuildingPanel _buildingPanel = null!;
    private EndGameOverlay _endGameOverlay = null!;

    // AI
    private SimpleAI _ai = null!;

    // Placement state
    private BuildingData? _selectedBuilding;
    private bool _isPlacing;
    private bool _matchEnded;

    public MatchSimulation Sim => _sim;

    public override void _Ready()
    {
        _sim = new MatchSimulation(
            Config.StartingGold, Config.BaseIncomePerTick, Config.IncomeTickMs,
            Config.BaseHpPerLane, Config.BuildZoneWidth, Config.BuildZoneHeight,
            Config.SimTickMs, Config.LaneLengthUnits);

        _laneRenderer = GetNode<LaneRenderer>("Lane");
        _p1BuildZone = GetNode<BuildZoneRenderer>("Player1Side/BuildZoneVisual");
        _p2BuildZone = GetNode<BuildZoneRenderer>("Player2Side/BuildZoneVisual");
        _hud = GetNode<HUD>("UI/HUD");
        _buildingPanel = GetNode<BuildingPanel>("UI/BuildingPanel");
        _endGameOverlay = GetNode<EndGameOverlay>("UI/EndGameOverlay");

        _laneRenderer.Initialize(_sim);
        _p1BuildZone.Initialize(Config.BuildZoneWidth, Config.BuildZoneHeight);
        _p2BuildZone.Initialize(Config.BuildZoneWidth, Config.BuildZoneHeight);
        _buildingPanel.Initialize(_sim, 0);

        // Wire UI events
        _buildingPanel.BuildingClicked += OnBuildingSelected;
        _endGameOverlay.PlayAgainPressed += OnPlayAgain;

        // Create AI for player 1
        var barracksInfo = BuildingDataToBuildingInfo(BarracksData);
        _ai = new SimpleAI(12345, barracksInfo, Config.BuildZoneWidth, Config.BuildZoneHeight);
    }

    public override void _Process(double delta)
    {
        if (_matchEnded) return;

        // Fixed timestep accumulator
        _accumulator += delta * 1000.0; // convert to ms
        while (_accumulator >= Config.SimTickMs)
        {
            _accumulator -= Config.SimTickMs;
            _sim.ProcessTick();
            _ai.Think(_sim, 1);
        }

        // Update renderers
        _laneRenderer.SyncFromSim(_sim);
        _hud.UpdateDisplay(_sim, 0); // Player 0 is human
        _buildingPanel.UpdateAffordability(_sim, 0);

        // Check for match end
        if (_sim.IsMatchOver())
        {
            _matchEnded = true;
            _endGameOverlay.Show(_sim.GetWinner(), 0);
        }
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event is InputEventMouseButton mb && mb.Pressed && mb.ButtonIndex == MouseButton.Left && _isPlacing && _selectedBuilding != null)
        {
            TryPlaceBuilding(mb.GlobalPosition);
        }
        if (@event is InputEventKey key && key.Pressed && key.Keycode == Key.Escape)
        {
            CancelPlacement();
        }
    }

    private void OnBuildingSelected(BuildingData data)
    {
        _selectedBuilding = data;
        _isPlacing = true;
    }

    private void TryPlaceBuilding(Vector2 mousePos)
    {
        // Convert mouse position to grid coordinates for player 0's build zone
        var (gridX, gridY) = _p1BuildZone.WorldToGrid(mousePos);
        if (gridX < 0) return;

        var info = BuildingDataToBuildingInfo(_selectedBuilding!);
        if (_sim.PlaceBuilding(0, gridX, gridY, info))
        {
            _p1BuildZone.RefreshVisuals(_sim, 0);
            CancelPlacement();
        }
    }

    public void CancelPlacement()
    {
        _selectedBuilding = null;
        _isPlacing = false;
    }

    private void OnPlayAgain()
    {
        GetTree().ReloadCurrentScene();
    }

    public static BuildingInfo BuildingDataToBuildingInfo(BuildingData data)
    {
        var info = new BuildingInfo
        {
            GoldCost = data.GoldCost,
            GridWidth = data.GridWidth,
            GridHeight = data.GridHeight,
            IsEconomyBuilding = data.IsEconomyBuilding,
            IncomeBonus = data.IncomeBonus,
        };
        if (data.SpawnedUnit != null)
        {
            info.UnitHp = data.SpawnedUnit.Hp;
            info.UnitDamage = data.SpawnedUnit.Damage;
            info.UnitAttackCooldownMs = data.SpawnedUnit.AttackCooldownMs;
            info.UnitMoveSpeed = data.SpawnedUnit.MoveSpeedUnits;
            info.UnitRange = data.SpawnedUnit.Range;
            info.UnitArmorType = (int)data.SpawnedUnit.ArmorType;
            info.UnitDamageType = (int)data.SpawnedUnit.DamageType;
            info.SpawnTimeMs = data.SpawnedUnit.SpawnTimeMs;
        }
        return info;
    }
}

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
    [Export] public RaceData[] AvailableRaces { get; set; } = System.Array.Empty<RaceData>();
    [Export] public RaceData? PlayerRace { get; set; }
    [Export] public RaceData? EnemyRace { get; set; }
    [Export] public BuildingData BarracksData { get; set; } = null!;
    [Export] public BuildingData? ArcheryRangeData { get; set; }
    [Export] public BuildingData? ArmoryData { get; set; }
    [Export] public BuildingData? SiegeWorkshopData { get; set; }
    [Export] public BuildingData? ForgeData { get; set; }
    [Export] public BuildingData? EconomyBuildingData { get; set; }

    private MatchSimulation _sim = null!;
    private double _accumulator;

    // References set in _Ready via GetNode
    private LaneRenderer _laneRenderer = null!;
    private BuildZoneRenderer _p1BuildZone = null!;
    private BuildZoneRenderer _p2BuildZone = null!;
    private HUD _hud = null!;
    private BuildingPanel _buildingPanel = null!;
    private EndGameOverlay _endGameOverlay = null!;
    private RaceSelectOverlay _raceSelectOverlay = null!;
    private Sprite2D _p1TowerSprite = null!;
    private Sprite2D _p2TowerSprite = null!;
    private ProgressBar _p1TowerHpBar = null!;
    private ProgressBar _p2TowerHpBar = null!;

    // AI
    private SimpleAI _ai = null!;

    // Placement state
    private BuildingData? _selectedBuilding;
    private bool _isPlacing;
    private bool _matchEnded;
    private bool _matchStarted;

    public MatchSimulation Sim => _sim;

    public override void _Ready()
    {
        _laneRenderer = GetNode<LaneRenderer>("Lane");
        _p1BuildZone = GetNode<BuildZoneRenderer>("Player1Side/BuildZoneVisual");
        _p2BuildZone = GetNode<BuildZoneRenderer>("Player2Side/BuildZoneVisual");
        _hud = GetNode<HUD>("UI/HUD");
        _buildingPanel = GetNode<BuildingPanel>("UI/BuildingPanel");
        _endGameOverlay = GetNode<EndGameOverlay>("UI/EndGameOverlay");
        _raceSelectOverlay = GetNode<RaceSelectOverlay>("UI/RaceSelectOverlay");
        _p1TowerSprite = GetNode<Sprite2D>("Player1Side/Base");
        _p2TowerSprite = GetNode<Sprite2D>("Player2Side/Base");
        _p1TowerHpBar = GetNode<ProgressBar>("Player1Side/TowerHpBar");
        _p2TowerHpBar = GetNode<ProgressBar>("Player2Side/TowerHpBar");

        _p1BuildZone.Initialize(Config.BuildZoneWidth, Config.BuildZoneHeight);
        _p1BuildZone.ObscureOccupiedCells = false;
        _p2BuildZone.ObscureOccupiedCells = true;
        _p2BuildZone.Initialize(Config.BuildZoneWidth, Config.BuildZoneHeight);

        // Wire UI events
        _endGameOverlay.PlayAgainPressed += OnPlayAgain;
        _raceSelectOverlay.RaceSelected += OnRaceSelected;

        if (AvailableRaces.Length > 0)
        {
            _buildingPanel.Visible = false;
            _hud.SetRaceNames("Select", "Pending");
            _raceSelectOverlay.Initialize(AvailableRaces);
        }
        else
        {
            StartMatchWithRaces(PlayerRace, EnemyRace);
        }
    }

    public override void _Process(double delta)
    {
        if (!_matchStarted)
            return;

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
        UpdatePlacementPreview();
        _laneRenderer.SyncFromSim(_sim);
        _hud.UpdateDisplay(_sim, 0); // Player 0 is human
        UpdateTowerVisuals();
        _buildingPanel.UpdateAffordability(_sim, 0);
        _p1BuildZone.RefreshVisuals(_sim, 0);
        _p2BuildZone.RefreshVisuals(_sim, 1);

        // Check for match end
        if (_sim.IsMatchOver())
        {
            _matchEnded = true;
            _endGameOverlay.Show(_sim.GetWinner(), 0, BuildPostgameAnalysis(_sim, 0, 1, _ai));
        }
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (!_matchStarted)
            return;

        if (@event is InputEventMouseButton mb && mb.Pressed && mb.ButtonIndex == MouseButton.Left)
        {
            if (_isPlacing && _selectedBuilding != null)
            {
                TryPlaceBuilding(GetGlobalMousePosition());
            }
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
        _buildingPanel.SetSelectedBuilding(data);
    }

    private void TryPlaceBuilding(Vector2 mousePos)
    {
        // Convert mouse position to grid coordinates for player 0's build zone
        var (gridX, gridY) = _p1BuildZone.WorldToGrid(mousePos);
        if (gridX < 0) return;

        BuildingData selectedBuilding = _selectedBuilding!;
        var info = BuildingDataToBuildingInfo(selectedBuilding);
        if (_sim.PlaceBuilding(0, gridX, gridY, info))
        {
            _p1BuildZone.RefreshVisuals(_sim, 0);
            _buildingPanel.UpdateAffordability(_sim, 0);
            if (_sim.GetGold(0) < selectedBuilding.GoldCost ||
                !_sim.MeetsRequirements(0, selectedBuilding.RequiredBuildingNames))
            {
                CancelPlacement();
            }
            else
            {
                _buildingPanel.SetSelectedBuilding(selectedBuilding);
                UpdatePlacementPreview();
            }
        }
    }

    public void CancelPlacement()
    {
        _selectedBuilding = null;
        _isPlacing = false;
        _p1BuildZone.ClearPreview();
        _buildingPanel.SetSelectedBuilding(null);
    }

    private void OnPlayAgain()
    {
        GetTree().ReloadCurrentScene();
    }

    private void OnRaceSelected(RaceData selectedRace)
    {
        RaceData enemyRace = ChooseEnemyRace(selectedRace);
        _raceSelectOverlay.Close();
        StartMatchWithRaces(selectedRace, enemyRace);
    }

    public static BuildingInfo BuildingDataToBuildingInfo(BuildingData data)
    {
        var info = new BuildingInfo
        {
            BuildingName = data.BuildingName,
            RequiredBuildingNames = data.RequiredBuildingNames,
            UnlocksBuildingNames = data.UnlocksBuildingNames,
            GoldCost = data.GoldCost,
            BuildDelayMs = data.BuildDelayMs,
            GridWidth = data.GridWidth,
            GridHeight = data.GridHeight,
            StrategicRole = data.StrategicRole,
            CompositionHint = ResolveCompositionHint(data),
            IsEconomyBuilding = data.IsEconomyBuilding,
            IncomeBonus = data.IncomeBonus,
            SupportDamageBonus = data.SupportDamageBonus,
            BuildingSpritePath = data.SpritePath,
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
            info.UnitSpritePath = data.SpawnedUnit.SpritePath;
            info.UnitTowerDamageMultiplierPct = data.SpawnedUnit.TowerDamageMultiplierPct;
        }
        return info;
    }

    private void UpdatePlacementPreview()
    {
        if (!_isPlacing || _selectedBuilding == null)
        {
            _p1BuildZone.ClearPreview();
            return;
        }

        var (gridX, gridY) = _p1BuildZone.WorldToGrid(GetGlobalMousePosition());
        if (gridX < 0 || gridY < 0)
        {
            _p1BuildZone.ClearPreview();
            return;
        }

        bool canPlace = _sim.CanPlaceBuilding(
            0,
            gridX,
            gridY,
            _selectedBuilding.GridWidth,
            _selectedBuilding.GridHeight) &&
            _sim.MeetsRequirements(0, _selectedBuilding.RequiredBuildingNames) &&
            _sim.GetGold(0) >= _selectedBuilding.GoldCost;

        _p1BuildZone.SetPreview(
            gridX,
            gridY,
            _selectedBuilding.GridWidth,
            _selectedBuilding.GridHeight,
            canPlace);
    }

    private void UpdateTowerVisuals()
    {
        if (!_matchStarted)
            return;
        UpdateTowerVisual(_p1TowerSprite, _p1TowerHpBar, _sim.GetTowerHp(0));
        UpdateTowerVisual(_p2TowerSprite, _p2TowerHpBar, _sim.GetTowerHp(1));
    }

    private void UpdateTowerVisual(Sprite2D sprite, ProgressBar hpBar, int currentHp)
    {
        int player = sprite == _p1TowerSprite ? 0 : 1;
        hpBar.MaxValue = _sim.GetStartingTowerHp(player);
        hpBar.Value = currentHp;

        float maxHp = _sim.GetStartingTowerHp(player);
        float hpPct = maxHp > 0 ? currentHp / maxHp : 0.0f;
        sprite.Modulate = new Color(1.0f, 0.5f + hpPct * 0.5f, 0.5f + hpPct * 0.5f);
    }

    private static BuildingData? FindBuildingOrFallback(BuildingData[] buildings, string name, BuildingData? fallback)
    {
        for (int i = 0; i < buildings.Length; i++)
        {
            if (buildings[i] != null && buildings[i].BuildingName == name)
                return buildings[i];
        }
        return fallback;
    }

    private static BuildingData? FindBuildingByArchetype(BuildingData[] buildings, string archetype)
    {
        for (int i = 0; i < buildings.Length; i++)
        {
            var building = buildings[i];
            if (building == null)
                continue;

            if (MatchesArchetype(building, archetype))
                return building;
        }

        return null;
    }

    private static bool MatchesArchetype(BuildingData building, string archetype)
    {
        if (archetype == "economy")
            return building.StrategicRole == StrategicRole.Economy || building.IncomeBonus > 0;

        if (archetype == "support")
            return building.StrategicRole == StrategicRole.Tech && building.SupportDamageBonus > 0;

        if (building.SpawnedUnit == null)
            return false;

        string role = building.SpawnedUnit.UnitRole;
        return archetype switch
        {
            "ranged" => role == "Ranged",
            "tank" => building.StrategicRole == StrategicRole.Defense || role == "Tank",
            "siege" => building.CompositionHint == CompositionHint.Splash || role == "Siege",
            "core" => building.StrategicRole == StrategicRole.Pressure && building.RequiredBuildingNames.Length == 0,
            _ => false
        };
    }

    private static BuildingInfo? ToOptionalBuildingInfo(BuildingData? data)
    {
        return data != null ? BuildingDataToBuildingInfo(data) : null;
    }

    private void StartMatchWithRaces(RaceData? playerRace, RaceData? enemyRace)
    {
        PlayerRace = playerRace;
        EnemyRace = enemyRace;
        _matchEnded = false;
        _accumulator = 0.0;

        int[] towerHpByPlayer =
        {
            PlayerRace?.TowerHp ?? Config.TowerHp,
            EnemyRace?.TowerHp ?? Config.TowerHp
        };
        int[] towerAttackDamageByPlayer =
        {
            PlayerRace?.TowerAttackDamage ?? Config.TowerAttackDamage,
            EnemyRace?.TowerAttackDamage ?? Config.TowerAttackDamage
        };
        int[] towerAttackCooldownByPlayer =
        {
            PlayerRace?.TowerAttackCooldownMs ?? Config.TowerAttackCooldownMs,
            EnemyRace?.TowerAttackCooldownMs ?? Config.TowerAttackCooldownMs
        };
        int[] towerAttackRangeByPlayer =
        {
            PlayerRace?.TowerAttackRange ?? Config.TowerAttackRange,
            EnemyRace?.TowerAttackRange ?? Config.TowerAttackRange
        };

        _sim = new MatchSimulation(
            Config.StartingGold, Config.BaseIncomePerTick, Config.IncomeTickMs,
            Config.TowerHp, Config.BuildZoneWidth, Config.BuildZoneHeight,
            Config.SimTickMs, Config.LaneLengthUnits,
            Config.TowerAttackDamage, Config.TowerAttackCooldownMs, Config.TowerAttackRange,
            towerHpByPlayer, towerAttackDamageByPlayer, towerAttackCooldownByPlayer, towerAttackRangeByPlayer);
        _laneRenderer.Initialize(_sim);

        if (PlayerRace != null && PlayerRace.AvailableBuildings.Length > 0)
            _buildingPanel.AvailableBuildings = PlayerRace.AvailableBuildings;

        _buildingPanel.Initialize(_sim, 0);
        _buildingPanel.BuildingClicked += OnBuildingSelected;
        _buildingPanel.Visible = true;
        _hud.SetRaceNames(PlayerRace?.RaceName ?? "Custom", EnemyRace?.RaceName ?? "Custom");
        ApplyRaceTowerVisuals();

        var enemyBuildings = EnemyRace != null && EnemyRace.AvailableBuildings.Length > 0
            ? EnemyRace.AvailableBuildings
            : new[] { BarracksData };

        var coreInfo = BuildingDataToBuildingInfo(
            FindBuildingByArchetype(enemyBuildings, "core")
            ?? FindBuildingOrFallback(enemyBuildings, "Barracks", BarracksData)!);
        BuildingInfo? rangedInfo = ToOptionalBuildingInfo(FindBuildingByArchetype(enemyBuildings, "ranged"));
        BuildingInfo? tankInfo = ToOptionalBuildingInfo(FindBuildingByArchetype(enemyBuildings, "tank"));
        BuildingInfo? siegeInfo = ToOptionalBuildingInfo(FindBuildingByArchetype(enemyBuildings, "siege"));
        BuildingInfo? supportInfo = ToOptionalBuildingInfo(FindBuildingByArchetype(enemyBuildings, "support"));
        BuildingInfo? economyInfo = ToOptionalBuildingInfo(FindBuildingByArchetype(enemyBuildings, "economy"));
        var aiPlan = ChooseAiStrategyPlan(EnemyRace);
        _ai = new SimpleAI(12345, coreInfo, rangedInfo, tankInfo, siegeInfo, supportInfo, economyInfo,
            Config.BuildZoneWidth, Config.BuildZoneHeight, plan: aiPlan);

        _matchStarted = true;
    }

    private static CompositionHint ResolveCompositionHint(BuildingData data)
    {
        if (data.CompositionHint != CompositionHint.Unknown)
            return data.CompositionHint;
        if (data.SpawnedUnit == null)
            return CompositionHint.Unknown;

        if (data.SpawnedUnit.UnitRole == "Swarm")
            return CompositionHint.Swarm;
        if (data.SpawnedUnit.UnitRole == "Tank" || data.SpawnedUnit.ArmorType == ArmorType.Heavy)
            return CompositionHint.Heavy;
        if (data.SpawnedUnit.UnitRole == "Siege" || data.SpawnedUnit.DamageType == DamageType.Siege)
            return CompositionHint.Splash;
        return CompositionHint.Mixed;
    }

    private static AiStrategyPlan ChooseAiStrategyPlan(RaceData? enemyRace)
    {
        string raceName = enemyRace?.RaceName ?? "";
        if (raceName.Contains("Orc"))
            return AiStrategyPlan.TempoRush;
        if (raceName.Contains("Undead"))
            return AiStrategyPlan.EconomyGreed;
        return AiStrategyPlan.TechCounterScaling;
    }

    private RaceData ChooseEnemyRace(RaceData selectedRace)
    {
        if (EnemyRace != null && EnemyRace != selectedRace)
            return EnemyRace;

        for (int i = 0; i < AvailableRaces.Length; i++)
        {
            if (AvailableRaces[i] != selectedRace)
                return AvailableRaces[i];
        }

        return selectedRace;
    }

    private void ApplyRaceTowerVisuals()
    {
        ApplyTowerSprite(_p1TowerSprite, PlayerRace?.TowerSpritePath);
        ApplyTowerSprite(_p2TowerSprite, EnemyRace?.TowerSpritePath);
        _p1TowerHpBar.Value = PlayerRace?.TowerHp ?? Config.TowerHp;
        _p1TowerHpBar.MaxValue = PlayerRace?.TowerHp ?? Config.TowerHp;
        _p2TowerHpBar.Value = EnemyRace?.TowerHp ?? Config.TowerHp;
        _p2TowerHpBar.MaxValue = EnemyRace?.TowerHp ?? Config.TowerHp;
    }

    private static void ApplyTowerSprite(Sprite2D sprite, string? spritePath)
    {
        if (string.IsNullOrEmpty(spritePath))
            return;

        var texture = GD.Load<Texture2D>(spritePath);
        if (texture != null)
            sprite.Texture = texture;
    }

    public static PostgameAnalysis BuildPostgameAnalysis(MatchSimulation sim, int humanPlayer, int enemyPlayer, SimpleAI ai)
    {
        int enemyPressureMs = sim.GetFirstInvestmentTime(enemyPlayer, StrategicRole.Pressure);
        int enemyEconomyMs = sim.GetFirstInvestmentTime(enemyPlayer, StrategicRole.Economy);
        int enemyTechMs = sim.GetFirstInvestmentTime(enemyPlayer, StrategicRole.Tech);
        int humanPressureMs = sim.GetFirstInvestmentTime(humanPlayer, StrategicRole.Pressure);
        int humanEconomyMs = sim.GetFirstInvestmentTime(humanPlayer, StrategicRole.Economy);
        int humanTechMs = sim.GetFirstInvestmentTime(humanPlayer, StrategicRole.Tech);
        bool humanWon = sim.GetWinner() == humanPlayer;

        string keyTiming = ai.Plan switch
        {
            AiStrategyPlan.TempoRush => $"Enemy pressure began at {FormatTime(enemyPressureMs)}.",
            AiStrategyPlan.EconomyGreed => $"Enemy economy began at {FormatTime(enemyEconomyMs)}.",
            AiStrategyPlan.TechCounterScaling => $"Enemy tech began at {FormatTime(enemyTechMs)}.",
            _ => "Enemy timing was unclear."
        };

        if (humanWon)
        {
            return new PostgameAnalysis
            {
                AiPlan = ai.PlanDisplayName,
                KeyEnemyTiming = keyTiming,
                LikelyMistake = "Winning read: you answered the enemy commitment before it paid off.",
                SuggestedAdaptation = "Try a greedier or faster tech response next run to test how much timing margin you had."
            };
        }

        if (ai.Plan == AiStrategyPlan.TempoRush && humanEconomyMs >= 0 &&
            (humanPressureMs < 0 || humanEconomyMs <= humanPressureMs))
        {
            return new PostgameAnalysis
            {
                AiPlan = ai.PlanDisplayName,
                KeyEnemyTiming = keyTiming,
                LikelyMistake = "Likely mistake: you built income while the enemy was preparing early pressure.",
                SuggestedAdaptation = "Open with pressure or defense first, then buy economy after the lane stabilizes."
            };
        }

        if (ai.Plan == AiStrategyPlan.EconomyGreed && enemyEconomyMs >= 0 &&
            (humanPressureMs < 0 || humanPressureMs > enemyEconomyMs + 45000))
        {
            return new PostgameAnalysis
            {
                AiPlan = ai.PlanDisplayName,
                KeyEnemyTiming = keyTiming,
                LikelyMistake = "Likely mistake: the enemy invested in economy and you did not punish before it paid off.",
                SuggestedAdaptation = "Delay your own economy and buy early lane pressure when the scout read shows enemy greed."
            };
        }

        if (ai.Plan == AiStrategyPlan.TechCounterScaling && enemyTechMs >= 0 &&
            (humanPressureMs < 0 || humanPressureMs > enemyTechMs))
        {
            return new PostgameAnalysis
            {
                AiPlan = ai.PlanDisplayName,
                KeyEnemyTiming = keyTiming,
                LikelyMistake = "Likely mistake: the enemy teched early and you gave it enough time to reach counter units.",
                SuggestedAdaptation = "Pressure before tech arrives, or tech into a different composition once the scout read turns high."
            };
        }

        if (humanPressureMs >= 0 && humanEconomyMs < 0 && humanTechMs < 0)
        {
            return new PostgameAnalysis
            {
                AiPlan = ai.PlanDisplayName,
                KeyEnemyTiming = keyTiming,
                LikelyMistake = "Likely mistake: your early pressure failed, and you did not transition into economy or tech.",
                SuggestedAdaptation = "If pressure stalls, pivot into income or a counter-tech building before repeating the same unit mix."
            };
        }

        return new PostgameAnalysis
        {
            AiPlan = ai.PlanDisplayName,
            KeyEnemyTiming = keyTiming,
            LikelyMistake = "Likely mistake: your timing response did not match the enemy commitment.",
            SuggestedAdaptation = "Use the Enemy Intent panel to choose one clear response: punish greed, defend rush, or pressure tech."
        };
    }

    private static string FormatTime(int elapsedMs)
    {
        if (elapsedMs < 0)
            return "not detected";
        int totalSec = elapsedMs / 1000;
        return $"{totalSec / 60}:{totalSec % 60:D2}";
    }
}

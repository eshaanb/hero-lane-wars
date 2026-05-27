using Godot;
using LaneWars.Buildings;
using LaneWars.Core;

namespace LaneWars.UI;

public partial class HUD : Control
{
    [Signal]
    public delegate void BuildingSelectedEventHandler(BuildingData data);

    private Label _goldLabel = null!;
    private Label _incomeLabel = null!;
    private Label _playerHpLabel = null!;
    private Label _enemyHpLabel = null!;
    private Label _timerLabel = null!;
    private Label _raceLabel = null!;
    private Label _buffLabel = null!;
    private Label _enemyIntentLabel = null!;
    private ProgressBar _playerHpBar = null!;
    private ProgressBar _enemyHpBar = null!;
    private PanelContainer _topBarCard = null!;
    private PanelContainer _intelCard = null!;

    public override void _Ready()
    {
        _topBarCard = GetNode<PanelContainer>("TopBarCard");
        _intelCard = GetNode<PanelContainer>("IntelCard");
        _goldLabel = GetNode<Label>("TopBarCard/Padding/TopBar/GoldLabel");
        _incomeLabel = GetNode<Label>("TopBarCard/Padding/TopBar/IncomeLabel");
        _playerHpLabel = GetNode<Label>("TopBarCard/Padding/TopBar/PlayerHpLabel");
        _enemyHpLabel = GetNode<Label>("TopBarCard/Padding/TopBar/EnemyHpLabel");
        _timerLabel = GetNode<Label>("TopBarCard/Padding/TopBar/TimerLabel");
        _raceLabel = GetNode<Label>("IntelCard/Padding/IntelPanel/RaceLabel");
        _buffLabel = GetNode<Label>("IntelCard/Padding/IntelPanel/BuffLabel");
        _enemyIntentLabel = GetNode<Label>("IntelCard/Padding/IntelPanel/EnemyIntentLabel");
        _playerHpBar = GetNode<ProgressBar>("TopBarCard/Padding/TopBar/PlayerHpBar");
        _enemyHpBar = GetNode<ProgressBar>("TopBarCard/Padding/TopBar/EnemyHpBar");

        _topBarCard.AddThemeStyleboxOverride("panel", UiChrome.CreatePanelStyle(new Color(0.08f, 0.10f, 0.14f, 0.92f)));
        _intelCard.AddThemeStyleboxOverride("panel", UiChrome.CreatePanelStyle(new Color(0.10f, 0.12f, 0.17f, 0.94f)));

        // Prevent HUD from consuming mouse clicks meant for the game world
        SetIgnoreMouse(this);
    }

    private static void SetIgnoreMouse(Node node)
    {
        if (node is Control c)
            c.MouseFilter = MouseFilterEnum.Ignore;
        foreach (var child in node.GetChildren())
            SetIgnoreMouse(child);
    }

    public void UpdateDisplay(MatchSimulation sim, int playerIndex)
    {
        int enemyIndex = 1 - playerIndex;
        _goldLabel.Text = $"Gold {sim.GetGold(playerIndex)}";

        int income = sim.GetIncomePerTick(playerIndex);
        int remainingMs = sim.IncomeTickMs - sim.IncomeAccumulatorMs;
        int remainingSec = (remainingMs + 999) / 1000;
        _incomeLabel.Text = $"+{income} in {remainingSec}s";

        int playerHp = sim.GetTowerHp(playerIndex);
        int enemyHp = sim.GetTowerHp(enemyIndex);
        _playerHpLabel.Text = $"Your Tower {playerHp}";
        _enemyHpLabel.Text = $"Enemy Tower {enemyHp}";
        _playerHpBar.MaxValue = sim.GetStartingTowerHp(playerIndex);
        _enemyHpBar.MaxValue = sim.GetStartingTowerHp(enemyIndex);
        _playerHpBar.Value = playerHp;
        _enemyHpBar.Value = enemyHp;

        _buffLabel.Text = $"Support bonus: +{sim.GetSupportDamageBonus(playerIndex)} damage";
        ScoutRead read = sim.GetScoutRead(enemyIndex);
        _enemyIntentLabel.Text =
            $"Enemy Intent: Econ {FormatSignal(read.Economy)} | Pressure {FormatSignal(read.Pressure)}\n" +
            $"Defense {FormatSignal(read.Defense)} | Tech {FormatSignal(read.Tech)} | {FormatComposition(read.CompositionHint)}";

        int totalSec = sim.SimTick.ElapsedMs / 1000;
        int mins = totalSec / 60;
        int secs = totalSec % 60;
        _timerLabel.Text = $"{mins}:{secs:D2}";
    }

    public void SetRaceNames(string playerRace, string enemyRace)
    {
        _raceLabel.Text = $"Race: {playerRace} vs {enemyRace}";
    }

    private static string FormatSignal(ScoutSignalLevel level)
    {
        return level switch
        {
            ScoutSignalLevel.High => "High",
            ScoutSignalLevel.Medium => "Med",
            _ => "Low"
        };
    }

    private static string FormatComposition(LaneWars.Buildings.CompositionHint hint)
    {
        return hint switch
        {
            LaneWars.Buildings.CompositionHint.Swarm => "Swarm",
            LaneWars.Buildings.CompositionHint.Heavy => "Heavy",
            LaneWars.Buildings.CompositionHint.Splash => "Splash",
            LaneWars.Buildings.CompositionHint.Mixed => "Mixed",
            _ => "Unknown"
        };
    }
}

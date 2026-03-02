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
    private Label _enemyBuildsLabel = null!;
    private ProgressBar _playerHpBar = null!;
    private ProgressBar _enemyHpBar = null!;

    public override void _Ready()
    {
        _goldLabel = GetNode<Label>("TopBar/GoldLabel");
        _incomeLabel = GetNode<Label>("TopBar/IncomeLabel");
        _playerHpLabel = GetNode<Label>("TopBar/PlayerHpLabel");
        _enemyHpLabel = GetNode<Label>("TopBar/EnemyHpLabel");
        _timerLabel = GetNode<Label>("TopBar/TimerLabel");
        _raceLabel = GetNode<Label>("IntelPanel/RaceLabel");
        _buffLabel = GetNode<Label>("IntelPanel/BuffLabel");
        _enemyBuildsLabel = GetNode<Label>("IntelPanel/EnemyBuildsLabel");
        _playerHpBar = GetNode<ProgressBar>("TopBar/PlayerHpBar");
        _enemyHpBar = GetNode<ProgressBar>("TopBar/EnemyHpBar");

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
        _goldLabel.Text = $"Gold: {sim.GetGold(playerIndex)}";

        int income = sim.GetIncomePerTick(playerIndex);
        int remainingMs = sim.IncomeTickMs - sim.IncomeAccumulatorMs;
        int remainingSec = (remainingMs + 999) / 1000;
        _incomeLabel.Text = $"${income} in {remainingSec}s";

        int playerHp = sim.GetTowerHp(playerIndex);
        int enemyHp = sim.GetTowerHp(enemyIndex);
        _playerHpLabel.Text = $"Your Tower: {playerHp}";
        _enemyHpLabel.Text = $"Enemy Tower: {enemyHp}";
        _playerHpBar.MaxValue = sim.GetStartingTowerHp(playerIndex);
        _enemyHpBar.MaxValue = sim.GetStartingTowerHp(enemyIndex);
        _playerHpBar.Value = playerHp;
        _enemyHpBar.Value = enemyHp;

        _buffLabel.Text = $"Forge Bonus: +{sim.GetSupportDamageBonus(playerIndex)} damage";
        _enemyBuildsLabel.Text = $"Enemy Builds: {sim.GetBuildingSummary(enemyIndex)}";

        int totalSec = sim.SimTick.ElapsedMs / 1000;
        int mins = totalSec / 60;
        int secs = totalSec % 60;
        _timerLabel.Text = $"{mins}:{secs:D2}";
    }

    public void SetRaceNames(string playerRace, string enemyRace)
    {
        _raceLabel.Text = $"Race: {playerRace} vs {enemyRace}";
    }
}

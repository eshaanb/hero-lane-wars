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
    private ProgressBar _playerHpBar = null!;
    private ProgressBar _enemyHpBar = null!;

    public override void _Ready()
    {
        _goldLabel = GetNode<Label>("TopBar/GoldLabel");
        _incomeLabel = GetNode<Label>("TopBar/IncomeLabel");
        _playerHpLabel = GetNode<Label>("TopBar/PlayerHpLabel");
        _enemyHpLabel = GetNode<Label>("TopBar/EnemyHpLabel");
        _timerLabel = GetNode<Label>("TopBar/TimerLabel");
        _playerHpBar = GetNode<ProgressBar>("TopBar/PlayerHpBar");
        _enemyHpBar = GetNode<ProgressBar>("TopBar/EnemyHpBar");
    }

    public void UpdateDisplay(MatchSimulation sim, int playerIndex)
    {
        int enemyIndex = 1 - playerIndex;
        _goldLabel.Text = $"Gold: {sim.GetGold(playerIndex)}";

        int income = 10 + sim.GetEconomyBuildingCount(playerIndex);
        _incomeLabel.Text = $"Income: {income}/tick";

        int playerHp = sim.GetBaseHp(playerIndex);
        int enemyHp = sim.GetBaseHp(enemyIndex);
        _playerHpLabel.Text = $"Your Base: {playerHp}";
        _enemyHpLabel.Text = $"Enemy Base: {enemyHp}";
        _playerHpBar.Value = playerHp;
        _enemyHpBar.Value = enemyHp;

        int totalSec = sim.SimTick.ElapsedMs / 1000;
        int mins = totalSec / 60;
        int secs = totalSec % 60;
        _timerLabel.Text = $"{mins}:{secs:D2}";
    }
}

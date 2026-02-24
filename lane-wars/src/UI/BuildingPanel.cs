using Godot;
using LaneWars.Buildings;
using LaneWars.Core;

namespace LaneWars.UI;

public partial class BuildingPanel : VBoxContainer
{
    [Export] public BuildingData[] AvailableBuildings { get; set; } = System.Array.Empty<BuildingData>();

    [Signal]
    public delegate void BuildingClickedEventHandler(BuildingData data);

    private MatchSimulation? _sim;
    private int _playerIndex;

    public void Initialize(MatchSimulation sim, int playerIndex)
    {
        _sim = sim;
        _playerIndex = playerIndex;
        CreateButtons();
    }

    private void CreateButtons()
    {
        foreach (var data in AvailableBuildings)
        {
            var btn = new Button();
            btn.Text = $"{data.BuildingName} ({data.GoldCost}g)";
            btn.CustomMinimumSize = new Vector2(150, 40);
            var capturedData = data;
            btn.Pressed += () => EmitSignal(SignalName.BuildingClicked, capturedData);
            AddChild(btn);
        }
    }

    public void UpdateAffordability(MatchSimulation sim, int playerIndex)
    {
        int gold = sim.GetGold(playerIndex);
        for (int i = 0; i < GetChildCount() && i < AvailableBuildings.Length; i++)
        {
            if (GetChild(i) is Button btn)
            {
                btn.Disabled = gold < AvailableBuildings[i].GoldCost;
            }
        }
    }
}

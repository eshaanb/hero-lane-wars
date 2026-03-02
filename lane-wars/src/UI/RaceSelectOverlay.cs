using Godot;
using LaneWars.Core;

namespace LaneWars.UI;

public partial class RaceSelectOverlay : Control
{
    [Signal]
    public delegate void RaceSelectedEventHandler(RaceData race);

    private Label _subtitleLabel = null!;
    private VBoxContainer _buttonList = null!;

    public override void _Ready()
    {
        _subtitleLabel = GetNode<Label>("Panel/VBox/SubTitle");
        _buttonList = GetNode<VBoxContainer>("Panel/VBox/Buttons");
        Visible = false;
    }

    public void Initialize(RaceData[] races)
    {
        foreach (Node child in _buttonList.GetChildren())
            child.QueueFree();

        if (races.Length == 0)
        {
            Visible = false;
            return;
        }

        _subtitleLabel.Text = "Choose a race to define your tech tree for this match.";

        for (int i = 0; i < races.Length; i++)
        {
            var race = races[i];
            var button = new Button();
            button.CustomMinimumSize = new Vector2(280, 88);
            button.Text = $"{race.RaceName}\n{race.TowerName}: {race.TowerAttackDamage} dmg / {race.TowerAttackRange} rng\n{BuildRaceSummary(race)}";
            button.TooltipText = string.IsNullOrEmpty(race.Description) ? race.RaceName : race.Description;
            button.Alignment = HorizontalAlignment.Left;
            var capturedRace = race;
            button.Pressed += () => EmitSignal(SignalName.RaceSelected, capturedRace);
            _buttonList.AddChild(button);
        }

        Visible = true;
    }

    public void Close()
    {
        Visible = false;
    }

    private static string BuildRaceSummary(RaceData race)
    {
        if (race.AvailableBuildings.Length == 0)
            return "No buildings";

        int previewCount = race.AvailableBuildings.Length < 3 ? race.AvailableBuildings.Length : 3;
        string[] names = new string[previewCount];
        for (int i = 0; i < previewCount; i++)
            names[i] = race.AvailableBuildings[i].BuildingName;

        string summary = string.Join(", ", names);
        if (race.AvailableBuildings.Length > previewCount)
            summary += ", ...";
        return summary;
    }
}

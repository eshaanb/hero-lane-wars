using Godot;
using LaneWars.Buildings;
using LaneWars.Core;
using System.Collections.Generic;
using System;

namespace LaneWars.UI;

public partial class BuildingPanel : ScrollContainer
{
    [Export] public BuildingData[] AvailableBuildings { get; set; } = System.Array.Empty<BuildingData>();

    [Signal]
    public delegate void BuildingClickedEventHandler(BuildingData data);

    private MatchSimulation? _sim;
    private int _playerIndex;
    private readonly List<Button> _buttons = new();
    private int _selectedIndex = -1;
    private VBoxContainer _list = null!;

    public void Initialize(MatchSimulation sim, int playerIndex)
    {
        _sim = sim;
        _playerIndex = playerIndex;
        MouseFilter = MouseFilterEnum.Stop;
        _list = GetNode<VBoxContainer>("List");
        CreateButtons();
    }

    private void CreateButtons()
    {
        for (int index = 0; index < AvailableBuildings.Length; index++)
        {
            var data = AvailableBuildings[index];
            var btn = new Button();
            btn.Text = FormatButtonLabel(data, false);
            btn.CustomMinimumSize = new Vector2(180, 48);
            btn.TooltipText = BuildTooltip(data);
            btn.Alignment = HorizontalAlignment.Left;
            var capturedData = data;
            int capturedIndex = index;
            btn.Pressed += () =>
            {
                SelectButton(capturedIndex);
                EmitSignal(SignalName.BuildingClicked, capturedData);
            };
            _list.AddChild(btn);
            _buttons.Add(btn);
        }
    }

    public void UpdateAffordability(MatchSimulation sim, int playerIndex)
    {
        int gold = sim.GetGold(playerIndex);
        for (int i = 0; i < _buttons.Count && i < AvailableBuildings.Length; i++)
        {
            var btn = _buttons[i];
            bool meetsRequirement = sim.MeetsRequirements(playerIndex, AvailableBuildings[i].RequiredBuildingNames);
            bool isAffordable = gold >= AvailableBuildings[i].GoldCost;
            bool canBuild = isAffordable && meetsRequirement;
            btn.Disabled = !canBuild;
            btn.Text = FormatButtonLabel(AvailableBuildings[i], i == _selectedIndex);
            btn.Modulate = canBuild ? Colors.White : new Color(0.7f, 0.7f, 0.7f);
            btn.TooltipText = BuildTooltip(AvailableBuildings[i], meetsRequirement);
        }
    }

    public void SetSelectedBuilding(BuildingData? data)
    {
        _selectedIndex = -1;
        if (data != null)
        {
            for (int i = 0; i < AvailableBuildings.Length; i++)
            {
                if (AvailableBuildings[i] == data)
                {
                    _selectedIndex = i;
                    break;
                }
            }
        }

        for (int i = 0; i < _buttons.Count && i < AvailableBuildings.Length; i++)
            _buttons[i].Text = FormatButtonLabel(AvailableBuildings[i], i == _selectedIndex);
    }

    private void SelectButton(int index)
    {
        _selectedIndex = index;
        for (int i = 0; i < _buttons.Count && i < AvailableBuildings.Length; i++)
            _buttons[i].Text = FormatButtonLabel(AvailableBuildings[i], i == _selectedIndex);
    }

    private static string FormatButtonLabel(BuildingData data, bool isSelected)
    {
        string prefix = isSelected ? "> " : "";
        string unlockText = data.UnlocksBuildingNames.Length > 0
            ? $" -> {string.Join("/", data.UnlocksBuildingNames)}"
            : "";
        string detail = data.IsEconomyBuilding
            ? data.SupportDamageBonus > 0
                ? $"+{data.SupportDamageBonus} spawn dmg"
                : $"+{data.IncomeBonus} income"
            : data.SpawnedUnit != null
                ? $"{data.SpawnedUnit.UnitName} / {data.SpawnedUnit.SpawnTimeMs / 1000.0f:0.#}s"
                : "No unit";

        return $"{prefix}{data.BuildingName} ({data.GoldCost}g)\n{detail}{unlockText}";
    }

    private static string BuildTooltip(BuildingData data, bool meetsRequirement = true)
    {
        string requirementText = data.RequiredBuildingNames.Length == 0
            ? ""
            : $" Requires {string.Join(", ", data.RequiredBuildingNames)}.";
        string unlockText = data.UnlocksBuildingNames.Length == 0
            ? ""
            : $" Unlocks {string.Join(", ", data.UnlocksBuildingNames)}.";
        string missingText = !meetsRequirement && data.RequiredBuildingNames.Length > 0
            ? " Requirement not met."
            : "";

        if (data.IsEconomyBuilding)
        {
            string effectText = data.SupportDamageBonus > 0
                ? $"adds +{data.SupportDamageBonus} damage to all future spawned units."
                : $"adds +{data.IncomeBonus} gold each income tick.";
            return $"{data.BuildingName}: costs {data.GoldCost} gold, {effectText}{requirementText}{unlockText}{missingText}";
        }

        if (data.SpawnedUnit == null)
            return $"{data.BuildingName}: costs {data.GoldCost} gold.{requirementText}{unlockText}{missingText}";

        string towerBonusText = data.SpawnedUnit.TowerDamageMultiplierPct != 100
            ? $" Tower DMG x{data.SpawnedUnit.TowerDamageMultiplierPct / 100.0f:0.#}."
            : "";
        return $"{data.BuildingName}: spawns {data.SpawnedUnit.UnitName} every {data.SpawnedUnit.SpawnTimeMs / 1000.0f:0.#}s. HP {data.SpawnedUnit.Hp}, DMG {data.SpawnedUnit.Damage}.{towerBonusText}{requirementText}{unlockText}{missingText}";
    }
}

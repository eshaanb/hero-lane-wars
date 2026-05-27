using Godot;
using LaneWars.Buildings;
using LaneWars.Core;
using System.Collections.Generic;

namespace LaneWars.UI;

public partial class BuildingPanel : PanelContainer
{
    [Export] public BuildingData[] AvailableBuildings { get; set; } = System.Array.Empty<BuildingData>();

    [Signal]
    public delegate void BuildingClickedEventHandler(BuildingData data);

    private MatchSimulation? _sim;
    private int _playerIndex;
    private readonly List<Button> _buttons = new();
    private int _selectedIndex = -1;
    private Label _statusLabel = null!;
    private VBoxContainer _list = null!;
    private ScrollContainer _scroll = null!;

    public void Initialize(MatchSimulation sim, int playerIndex)
    {
        _sim = sim;
        _playerIndex = playerIndex;
        MouseFilter = MouseFilterEnum.Stop;
        AddThemeStyleboxOverride("panel", UiChrome.CreatePanelStyle(new Color(0.08f, 0.09f, 0.13f, 0.95f)));
        _statusLabel = GetNode<Label>("Body/Content/Hint");
        _scroll = GetNode<ScrollContainer>("Body/Content/Scroll");
        _scroll.MouseFilter = MouseFilterEnum.Stop;
        _list = GetNode<VBoxContainer>("Body/Content/Scroll/List");
        ClearButtons();
        CreateButtons();
    }

    private void CreateButtons()
    {
        for (int index = 0; index < AvailableBuildings.Length; index++)
        {
            var data = AvailableBuildings[index];
            var btn = new Button();
            btn.Text = FormatButtonLabel(data, false);
            btn.CustomMinimumSize = new Vector2(0, 58);
            btn.SizeFlagsHorizontal = SizeFlags.ExpandFill;
            btn.TooltipText = BuildTooltip(data);
            btn.Alignment = HorizontalAlignment.Left;
            btn.TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis;
            ApplyBuildingIcon(btn, data);
            ApplyButtonTheme(btn);
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

    private void ClearButtons()
    {
        for (int i = 0; i < _buttons.Count; i++)
            _buttons[i].QueueFree();
        _buttons.Clear();
        _selectedIndex = -1;
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
            string tooltip = BuildTooltip(AvailableBuildings[i], meetsRequirement);
            if (!isAffordable)
                tooltip += $" Need {AvailableBuildings[i].GoldCost - gold} more gold.";
            btn.TooltipText = tooltip;
        }

        UpdateStatusLabel();
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

        UpdateStatusLabel();
    }

    private void SelectButton(int index)
    {
        _selectedIndex = index;
        for (int i = 0; i < _buttons.Count && i < AvailableBuildings.Length; i++)
            _buttons[i].Text = FormatButtonLabel(AvailableBuildings[i], i == _selectedIndex);

        UpdateStatusLabel();
    }

    private static string FormatButtonLabel(BuildingData data, bool isSelected)
    {
        string prefix = isSelected ? "[ " : "";
        string suffix = isSelected ? " ]" : "";
        string buildText = data.BuildDelayMs > 0
            ? $"{FormatSeconds(data.BuildDelayMs)} build · "
            : "";
        string detail = data.IncomeBonus > 0
            ? data.SupportDamageBonus > 0
                ? $"{buildText}+{data.SupportDamageBonus} spawn damage"
                : $"{buildText}+{data.IncomeBonus} income"
            : data.SupportDamageBonus > 0
                ? $"{buildText}+{data.SupportDamageBonus} spawn damage"
            : data.SpawnedUnit != null
                ? $"{buildText}{data.SpawnedUnit.UnitName} every {FormatSeconds(data.SpawnedUnit.SpawnTimeMs)}"
                : "No unit";

        return $"{prefix}{data.BuildingName}  {data.GoldCost}g{suffix}\n{detail}";
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

        if (data.IncomeBonus > 0 || data.SupportDamageBonus > 0)
        {
            string effectText = data.SupportDamageBonus > 0
                ? $"adds +{data.SupportDamageBonus} damage to all future spawned units."
                : $"adds +{data.IncomeBonus} gold each income tick.";
            string delayText = data.BuildDelayMs > 0
                ? $" Activates after {data.BuildDelayMs / 1000.0f:0.#}s."
                : "";
            return $"{data.BuildingName}: costs {data.GoldCost} gold, {effectText}{delayText}{requirementText}{unlockText}{missingText}";
        }

        if (data.SpawnedUnit == null)
            return $"{data.BuildingName}: costs {data.GoldCost} gold.{requirementText}{unlockText}{missingText}";

        string buildDelayText = data.BuildDelayMs > 0
            ? $" Build time {FormatSeconds(data.BuildDelayMs)}."
            : "";
        string towerBonusText = data.SpawnedUnit.TowerDamageMultiplierPct != 100
            ? $" Tower DMG x{data.SpawnedUnit.TowerDamageMultiplierPct / 100.0f:0.#}."
            : "";
        return $"{data.BuildingName}: spawns {data.SpawnedUnit.UnitName} every {FormatSeconds(data.SpawnedUnit.SpawnTimeMs)}.{buildDelayText} HP {data.SpawnedUnit.Hp}, DMG {data.SpawnedUnit.Damage}.{towerBonusText}{requirementText}{unlockText}{missingText}";
    }

    private void UpdateStatusLabel()
    {
        if (_statusLabel == null)
            return;

        if (_selectedIndex < 0 || _selectedIndex >= AvailableBuildings.Length)
        {
            _statusLabel.Text = "No blueprint selected";
            return;
        }

        var selected = AvailableBuildings[_selectedIndex];
        if (_sim == null)
        {
            _statusLabel.Text = $"Selected: {selected.BuildingName}";
            return;
        }

        if (!_sim.MeetsRequirements(_playerIndex, selected.RequiredBuildingNames))
        {
            _statusLabel.Text = $"Missing: {string.Join(", ", selected.RequiredBuildingNames)}";
            return;
        }

        int gold = _sim.GetGold(_playerIndex);
        _statusLabel.Text = gold >= selected.GoldCost
            ? $"Placing: {selected.BuildingName}"
            : $"Need {selected.GoldCost - gold}g for {selected.BuildingName}";
    }

    private static void ApplyButtonTheme(Button btn)
    {
        UiChrome.ApplyFantasyButtonTheme(btn);
        btn.AddThemeConstantOverride("h_separation", 8);
    }

    private static void ApplyBuildingIcon(Button btn, BuildingData data)
    {
        if (string.IsNullOrEmpty(data.SpritePath))
            return;

        var icon = GD.Load<Texture2D>(data.SpritePath);
        if (icon == null)
            return;

        btn.Icon = icon;
        btn.IconAlignment = HorizontalAlignment.Left;
        btn.VerticalIconAlignment = VerticalAlignment.Center;
        btn.ExpandIcon = false;
    }

    private static string FormatSeconds(int milliseconds)
    {
        if (milliseconds <= 0)
            return "0s";

        float seconds = milliseconds / 1000.0f;
        if (seconds < 10.0f && milliseconds % 1000 != 0)
            return $"{seconds:0.#}s";

        return $"{seconds}s";
    }
}

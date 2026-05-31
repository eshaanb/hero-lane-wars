using Godot;
using LaneWars.Core;
using System.Collections.Generic;

namespace LaneWars.UI;

/// <summary>
/// "Click your tower" panel for the exclusive tower-upgrade tree.
/// Built entirely in code (no .tscn) and added under the UI CanvasLayer at runtime.
/// Players commit to ONE branch and level it up.
/// </summary>
public partial class TowerUpgradePanel : PanelContainer
{
    private MatchSimulation? _sim;
    private int _playerIndex;
    private Label _hint = null!;
    private readonly Dictionary<TowerBranch, Button> _buttons = new();

    private static readonly (TowerBranch Branch, string Name, string Blurb)[] Branches =
    {
        (TowerBranch.Scattershot, "Scattershot", "AoE splash — melts swarms"),
        (TowerBranch.Ballista, "Ballista", "+damage & +range — kills elites"),
        (TowerBranch.Bulwark, "Bulwark", "+tower HP — survive aggression"),
    };

    public void Initialize(MatchSimulation sim, int playerIndex)
    {
        _sim = sim;
        _playerIndex = playerIndex;
        Visible = false;
        MouseFilter = MouseFilterEnum.Stop;
        CustomMinimumSize = new Vector2(300, 0);
        Position = new Vector2(20, 360);
        AddThemeStyleboxOverride("panel", UiChrome.CreatePanelStyle(new Color(0.08f, 0.09f, 0.13f, 0.96f)));

        var margin = new MarginContainer();
        margin.AddThemeConstantOverride("margin_left", 12);
        margin.AddThemeConstantOverride("margin_right", 12);
        margin.AddThemeConstantOverride("margin_top", 10);
        margin.AddThemeConstantOverride("margin_bottom", 10);
        AddChild(margin);

        var vbox = new VBoxContainer();
        vbox.AddThemeConstantOverride("separation", 8);
        margin.AddChild(vbox);

        var title = new Label { Text = "Tower Upgrades" };
        vbox.AddChild(title);

        foreach (var b in Branches)
        {
            var btn = new Button
            {
                CustomMinimumSize = new Vector2(0, 52),
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                Alignment = HorizontalAlignment.Left,
                TooltipText = b.Blurb,
                TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis
            };
            UiChrome.ApplyFantasyButtonTheme(btn);
            TowerBranch captured = b.Branch;
            btn.Pressed += () => OnBranchPressed(captured);
            vbox.AddChild(btn);
            _buttons[b.Branch] = btn;
        }

        _hint = new Label { Text = "" };
        _hint.AddThemeColorOverride("font_color", new Color(0.75f, 0.78f, 0.85f));
        _hint.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        vbox.AddChild(_hint);

        Refresh();
    }

    public void Toggle()
    {
        Visible = !Visible;
        if (Visible)
            Refresh();
    }

    public void HidePanel() => Visible = false;

    public bool ContainsViewportPoint(Vector2 point)
    {
        return Visible && GetGlobalRect().HasPoint(point);
    }

    public void Refresh()
    {
        if (_sim == null)
            return;

        TowerBranch chosen = _sim.GetTowerBranch(_playerIndex);
        int level = _sim.GetTowerUpgradeLevel(_playerIndex);
        int gold = _sim.GetGold(_playerIndex);

        foreach (var b in Branches)
        {
            Button btn = _buttons[b.Branch];
            int cost = _sim.GetTowerUpgradeCost(_playerIndex, b.Branch);
            bool lockedOut = chosen != TowerBranch.None && chosen != b.Branch;
            int branchLevel = chosen == b.Branch ? level : 0;
            bool maxed = chosen == b.Branch && cost < 0;

            string label;
            if (lockedOut)
                label = $"{b.Name}  —  locked (committed to {chosen})";
            else if (maxed)
                label = $"{b.Name}  Lv{branchLevel}  —  MAX";
            else
                label = $"{b.Name}  Lv{branchLevel}→{branchLevel + 1}  ·  {cost}g\n{b.Blurb}";

            btn.Text = label;
            btn.Disabled = lockedOut || maxed || cost < 0 || gold < cost;
        }

        _hint.Text = chosen == TowerBranch.None
            ? "Pick ONE specialization — you can't switch later."
            : $"Specialized: {chosen} (Lv{level}/{TowerUpgrades.MaxLevel})";
    }

    private void OnBranchPressed(TowerBranch branch)
    {
        if (_sim != null && _sim.UpgradeTower(_playerIndex, branch))
            Refresh();
    }
}

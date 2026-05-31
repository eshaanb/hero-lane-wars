using Godot;
using LaneWars.Units;

namespace LaneWars.UI;

/// <summary>
/// Hotkey (C) overlay showing the damage-type × armor-type counter matrix, color-coded,
/// so players can learn what beats what. Values come live from <see cref="CombatResolver"/>.
/// </summary>
public partial class CounterMatrixOverlay : PanelContainer
{
    private static readonly string[] ArmorNames = { "Light", "Medium", "Heavy", "Magical" };
    private static readonly string[] DamageNames = { "Physical", "Piercing", "Siege", "Magic" };

    public void Initialize()
    {
        Visible = false;
        MouseFilter = MouseFilterEnum.Ignore;
        Position = new Vector2(430, 120);
        CustomMinimumSize = new Vector2(420, 0);
        AddThemeStyleboxOverride("panel", UiChrome.CreatePanelStyle(new Color(0.07f, 0.08f, 0.12f, 0.97f)));

        var margin = new MarginContainer();
        margin.AddThemeConstantOverride("margin_left", 14);
        margin.AddThemeConstantOverride("margin_right", 14);
        margin.AddThemeConstantOverride("margin_top", 12);
        margin.AddThemeConstantOverride("margin_bottom", 12);
        AddChild(margin);

        var vbox = new VBoxContainer();
        vbox.AddThemeConstantOverride("separation", 8);
        margin.AddChild(vbox);

        vbox.AddChild(MakeLabel("Counters — damage (rows) vs armor (cols)", new Color("f3f7ff"), HorizontalAlignment.Left));

        var grid = new GridContainer { Columns = ArmorNames.Length + 1 };
        grid.AddThemeConstantOverride("h_separation", 6);
        grid.AddThemeConstantOverride("v_separation", 4);
        vbox.AddChild(grid);

        grid.AddChild(MakeCell("DMG \\ ARM", HeaderColor));
        foreach (var armor in ArmorNames)
            grid.AddChild(MakeCell(armor, HeaderColor));

        for (int d = 0; d < DamageNames.Length; d++)
        {
            grid.AddChild(MakeCell(DamageNames[d], HeaderColor));
            for (int a = 0; a < ArmorNames.Length; a++)
            {
                int pct = CombatResolver.CalculateDamage(100, d, a);
                grid.AddChild(MakeCell($"{pct}%", PctColor(pct)));
            }
        }

        vbox.AddChild(MakeLabel(
            "Piercing→Light · Siege→Heavy · Magic→Magical · Splash→swarms",
            new Color(0.72f, 0.78f, 0.88f), HorizontalAlignment.Left));
    }

    public void Toggle() => Visible = !Visible;
    public void HideOverlay() => Visible = false;

    private static readonly Color HeaderColor = new(0.78f, 0.83f, 0.92f);

    private static Color PctColor(int pct) =>
        pct > 100 ? new Color(0.5f, 0.95f, 0.55f) :
        pct < 100 ? new Color(1.0f, 0.5f, 0.5f) :
        new Color(0.8f, 0.8f, 0.84f);

    private static Label MakeCell(string text, Color color)
    {
        var label = MakeLabel(text, color, HorizontalAlignment.Center);
        label.CustomMinimumSize = new Vector2(74, 0);
        return label;
    }

    private static Label MakeLabel(string text, Color color, HorizontalAlignment align)
    {
        var label = new Label { Text = text, HorizontalAlignment = align };
        label.AddThemeColorOverride("font_color", color);
        return label;
    }
}

using Godot;

namespace LaneWars.UI;

internal static class UiChrome
{
    public static StyleBox CreatePanelStyle(Color color, float contentMargin = 8.0f)
    {
        return CreateFlatStyle(
            color,
            new Color(0.24f, 0.30f, 0.39f, 0.85f),
            contentMargin,
            4,
            hasShadow: true);
    }

    public static StyleBox CreateButtonStyle(Color color, float contentMargin = 8.0f)
    {
        return CreateFlatStyle(
            color,
            new Color(0.30f, 0.36f, 0.46f, 0.75f),
            contentMargin,
            3,
            hasShadow: false);
    }

    public static void ApplyFantasyButtonTheme(Button button)
    {
        button.AddThemeStyleboxOverride("normal", CreateButtonStyle(new Color(0.16f, 0.18f, 0.22f, 0.96f)));
        button.AddThemeStyleboxOverride("hover", CreateButtonStyle(new Color(0.22f, 0.27f, 0.34f, 0.98f)));
        button.AddThemeStyleboxOverride("pressed", CreateButtonStyle(new Color(0.31f, 0.38f, 0.50f, 1.0f)));
        button.AddThemeStyleboxOverride("disabled", CreateButtonStyle(new Color(0.10f, 0.11f, 0.13f, 0.78f)));
        button.AddThemeColorOverride("font_color", new Color("f3f4f6"));
        button.AddThemeColorOverride("font_hover_color", Colors.White);
        button.AddThemeColorOverride("font_pressed_color", Colors.White);
        button.AddThemeColorOverride("font_disabled_color", new Color("8d94a1"));
    }

    private static StyleBoxFlat CreateFlatStyle(Color color, Color borderColor, float contentMargin, int cornerRadius, bool hasShadow)
    {
        var style = new StyleBoxFlat
        {
            BgColor = color,
            BorderColor = borderColor,
            BorderWidthLeft = 1,
            BorderWidthTop = 1,
            BorderWidthRight = 1,
            BorderWidthBottom = 1,
            CornerRadiusTopLeft = cornerRadius,
            CornerRadiusTopRight = cornerRadius,
            CornerRadiusBottomRight = cornerRadius,
            CornerRadiusBottomLeft = cornerRadius
        };

        if (hasShadow)
        {
            style.ShadowColor = new Color(0.0f, 0.0f, 0.0f, 0.32f);
            style.ShadowSize = 4;
            style.ShadowOffset = new Vector2(0.0f, 2.0f);
        }

        style.SetContentMarginAll(contentMargin);
        return style;
    }
}

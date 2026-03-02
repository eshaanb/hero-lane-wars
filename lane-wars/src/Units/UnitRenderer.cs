using Godot;

namespace LaneWars.Units;

public partial class UnitRenderer : Node2D
{
    private static readonly System.Collections.Generic.Dictionary<string, Texture2D> TextureCache = new();

    private Sprite2D _sprite = null!;
    private ColorRect _healthBarBg = null!;
    private ColorRect _healthBarFill = null!;
    private int _ownerPlayer;
    private Texture2D? _defaultTexture;

    // Lane layout constants
    private const float LaneStartX = 200.0f;  // Left edge of lane in pixels
    private const float LaneY = 360.0f;       // Lane center Y
    private const float PixelsPerUnit = 0.8f;  // Scale sim units to pixels

    public void Initialize(UnitState unit)
    {
        _ownerPlayer = unit.OwnerPlayer;
    }

    public override void _Ready()
    {
        _sprite = GetNode<Sprite2D>("Sprite");
        _healthBarBg = GetNode<ColorRect>("HealthBarBg");
        _healthBarFill = GetNode<ColorRect>("HealthBarFill");

        _defaultTexture = GD.Load<Texture2D>("res://assets/sprites/units/footman_blue.png");
        _sprite.Modulate = Colors.White;
    }

    public void SyncFromSim(UnitState unit, int laneLengthUnits, Vector2 visualOffset)
    {
        // Convert integer position to screen position
        Position = CalculateScreenPosition(unit.PositionX, visualOffset);

        // Load sprite from sim state. If there is no dedicated enemy sprite, tint the unit red.
        string spritePath = ResolveSpritePath(unit.SpritePath, unit.OwnerPlayer);
        _sprite.Texture = LoadTexture(spritePath) ?? _defaultTexture;

        // Flip sprite based on direction
        _sprite.FlipH = unit.Direction < 0;
        _sprite.Modulate = unit.RecentAttackMs > 0
            ? new Color(1.0f, 1.0f, 0.8f)
            : Colors.White;

        // Update health bar
        float hpPercent = (float)unit.Hp / unit.MaxHp;
        _healthBarFill.Size = new Vector2(16 * hpPercent, 2);
        _healthBarFill.Color = hpPercent > 0.5f ? new Color(0, 1, 0) : new Color(1, 0, 0);
    }

    public static Vector2 CalculateScreenPosition(int positionX, Vector2 visualOffset)
    {
        float screenX = LaneStartX + positionX * PixelsPerUnit;
        return new Vector2(screenX, LaneY) + visualOffset;
    }

    private static string ResolveSpritePath(string baseSpritePath, int ownerPlayer)
    {
        if (string.IsNullOrEmpty(baseSpritePath))
            return "";

        if (ownerPlayer == 1 && baseSpritePath.Contains("_blue."))
            return baseSpritePath.Replace("_blue.", "_red.");

        return baseSpritePath;
    }

    private static Texture2D? LoadTexture(string spritePath)
    {
        if (string.IsNullOrEmpty(spritePath))
            return null;

        if (!TextureCache.TryGetValue(spritePath, out var texture))
        {
            texture = GD.Load<Texture2D>(spritePath);
            if (texture == null)
                return null;
            TextureCache[spritePath] = texture;
        }

        return texture;
    }
}

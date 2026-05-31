using Godot;

namespace LaneWars.Units;

public partial class UnitRenderer : Node2D
{
    private static readonly System.Collections.Generic.Dictionary<string, Texture2D> TextureCache = new();

    private Sprite2D _sprite = null!;
    private Sprite2D _outline = null!;
    private ColorRect _healthBarBg = null!;
    private ColorRect _healthBarFill = null!;
    private int _ownerPlayer;
    private Texture2D? _defaultTexture;

    // Lane layout constants
    private const float LaneStartX = 200.0f;  // Left edge of lane in pixels
    private const float LaneY = 360.0f;       // Lane center Y
    private const float PixelsPerUnit = 0.8f;  // Scale sim units to pixels
    private const float TowerInsetPx = 40.0f;  // keep units rendering in front of towers, not under them
    private const float BuildZoneCellSize = 32.0f;
    private const int SpawnEntryDurationMs = 700;
    private static readonly Vector2 PlayerBuildZoneOrigin = new(16.0f, 200.0f);
    private static readonly Vector2 EnemyBuildZoneOrigin = new(1040.0f, 200.0f);

    public void Initialize(UnitState unit)
    {
        _ownerPlayer = unit.OwnerPlayer;
    }

    public override void _Ready()
    {
        _sprite = GetNode<Sprite2D>("Sprite");
        _healthBarBg = GetNode<ColorRect>("HealthBarBg");
        _healthBarFill = GetNode<ColorRect>("HealthBarFill");

        _defaultTexture = GD.Load<Texture2D>("res://assets/sprites/units/swordsman_blue.png");
        _sprite.Modulate = Colors.White;

        // Team-colored silhouette behind the sprite so friend (blue) vs foe (red) reads at a glance.
        _outline = new Sprite2D
        {
            ZIndex = -1,
            Scale = new Vector2(1.32f, 1.32f),
            Modulate = _ownerPlayer == 0 ? new Color(0.3f, 0.65f, 1.0f) : new Color(1.0f, 0.38f, 0.38f)
        };
        AddChild(_outline);
    }

    public void SyncFromSim(UnitState unit, int laneLengthUnits, Vector2 visualOffset)
    {
        // Convert integer position to screen position
        Position = CalculateScreenPosition(unit, laneLengthUnits, visualOffset);

        // Load sprite from sim state. If there is no dedicated enemy sprite, tint the unit red.
        string spritePath = ResolveSpritePath(unit.SpritePath, unit.OwnerPlayer);
        _sprite.Texture = LoadTexture(spritePath) ?? _defaultTexture;

        // Keep the team-colored silhouette in sync with the sprite.
        _outline.Texture = _sprite.Texture;
        _outline.FlipH = unit.Direction < 0;

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

    public static Vector2 CalculateScreenPosition(int positionX, int laneLengthUnits, Vector2 visualOffset)
    {
        float screenX = LaneStartX + positionX * PixelsPerUnit;
        // Clamp so units stop in front of the towers (which sit at the lane endpoints) rather than under them.
        float minX = LaneStartX + TowerInsetPx;
        float maxX = LaneStartX + laneLengthUnits * PixelsPerUnit - TowerInsetPx;
        if (maxX > minX)
            screenX = Mathf.Clamp(screenX, minX, maxX);
        return new Vector2(screenX, LaneY) + visualOffset;
    }

    public static Vector2 CalculateScreenPosition(UnitState unit, int laneLengthUnits, Vector2 visualOffset)
    {
        Vector2 lanePosition = CalculateScreenPosition(unit.PositionX, laneLengthUnits, visualOffset);
        if (unit.SpawnGridX < 0 || unit.SpawnGridY < 0 || unit.AgeMs >= SpawnEntryDurationMs)
            return lanePosition;

        float t = Mathf.Clamp(unit.AgeMs / (float)SpawnEntryDurationMs, 0.0f, 1.0f);
        float eased = 1.0f - (1.0f - t) * (1.0f - t);
        return CalculateSpawnOrigin(unit).Lerp(lanePosition, eased);
    }

    private static Vector2 CalculateSpawnOrigin(UnitState unit)
    {
        Vector2 buildZoneOrigin = unit.OwnerPlayer == 0 ? PlayerBuildZoneOrigin : EnemyBuildZoneOrigin;
        int gridWidth = unit.SpawnGridWidth > 0 ? unit.SpawnGridWidth : 1;
        int gridHeight = unit.SpawnGridHeight > 0 ? unit.SpawnGridHeight : 1;
        return buildZoneOrigin + new Vector2(
            (unit.SpawnGridX + gridWidth * 0.5f) * BuildZoneCellSize,
            (unit.SpawnGridY + gridHeight * 0.5f) * BuildZoneCellSize);
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

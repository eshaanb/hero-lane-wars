using Godot;

namespace LaneWars.Units;

public partial class UnitRenderer : Node2D
{
    private Sprite2D _sprite = null!;
    private ColorRect _healthBarBg = null!;
    private ColorRect _healthBarFill = null!;
    private int _ownerPlayer;

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

        // Tint by owner
        if (_ownerPlayer == 0)
            _sprite.Modulate = new Color(0.4f, 0.6f, 1.0f); // Blue
        else
            _sprite.Modulate = new Color(1.0f, 0.4f, 0.4f); // Red
    }

    public void SyncFromSim(UnitState unit, int laneLengthUnits)
    {
        // Convert integer position to screen position
        float screenX = LaneStartX + unit.PositionX * PixelsPerUnit;
        Position = new Vector2(screenX, LaneY);

        // Flip sprite based on direction
        _sprite.FlipH = unit.Direction < 0;

        // Update health bar
        float hpPercent = (float)unit.Hp / unit.MaxHp;
        _healthBarFill.Size = new Vector2(16 * hpPercent, 2);
        _healthBarFill.Color = hpPercent > 0.5f ? new Color(0, 1, 0) : new Color(1, 0, 0);
    }
}

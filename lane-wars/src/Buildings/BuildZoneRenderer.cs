using Godot;
using LaneWars.Core;

namespace LaneWars.Buildings;

public partial class BuildZoneRenderer : Node2D
{
    private int _width;
    private int _height;
    private const float CellSize = 32.0f;
    private Texture2D _cellTexture = null!;
    private Texture2D _validTexture = null!;
    private Texture2D _invalidTexture = null!;

    public void Initialize(int width, int height)
    {
        _width = width;
        _height = height;
        _cellTexture = GD.Load<Texture2D>("res://assets/sprites/buildings/grid_cell.png");
        _validTexture = GD.Load<Texture2D>("res://assets/sprites/buildings/grid_cell_valid.png");
        _invalidTexture = GD.Load<Texture2D>("res://assets/sprites/buildings/grid_cell_invalid.png");
        DrawGrid();
    }

    private void DrawGrid()
    {
        for (int y = 0; y < _height; y++)
        {
            for (int x = 0; x < _width; x++)
            {
                var sprite = new Sprite2D();
                sprite.Texture = _cellTexture;
                sprite.Position = new Vector2(x * CellSize + CellSize / 2, y * CellSize + CellSize / 2);
                sprite.Name = $"Cell_{x}_{y}";
                AddChild(sprite);
            }
        }
    }

    public (int, int) WorldToGrid(Vector2 worldPos)
    {
        Vector2 local = ToLocal(worldPos);
        int gx = (int)(local.X / CellSize);
        int gy = (int)(local.Y / CellSize);
        if (gx < 0 || gx >= _width || gy < 0 || gy >= _height)
            return (-1, -1);
        return (gx, gy);
    }

    public void RefreshVisuals(MatchSimulation sim, int player)
    {
        // For now just mark occupied cells (could add building sprites later)
    }
}

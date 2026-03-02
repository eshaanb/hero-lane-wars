using Godot;
using LaneWars.Core;
using System.Collections.Generic;

namespace LaneWars.Buildings;

public partial class BuildZoneRenderer : Node2D
{
    private int _width;
    private int _height;
    private const float CellSize = 32.0f;
    private Texture2D _cellTexture = null!;
    private Texture2D _validTexture = null!;
    private Texture2D _invalidTexture = null!;
    private Sprite2D[,] _cells = null!;
    private readonly Dictionary<string, Texture2D> _buildingTextureCache = new();
    private bool _hasPreview;
    private int _previewX;
    private int _previewY;
    private int _previewWidth;
    private int _previewHeight;
    private bool _previewValid;

    public void Initialize(int width, int height)
    {
        _width = width;
        _height = height;
        _cellTexture = GD.Load<Texture2D>("res://assets/sprites/buildings/grid_cell.png");
        _validTexture = GD.Load<Texture2D>("res://assets/sprites/buildings/grid_cell_valid.png");
        _invalidTexture = GD.Load<Texture2D>("res://assets/sprites/buildings/grid_cell_invalid.png");
        _cells = new Sprite2D[width, height];
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
                _cells[x, y] = sprite;
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
        for (int y = 0; y < _height; y++)
        {
            for (int x = 0; x < _width; x++)
            {
                if (sim.IsCellOccupied(player, x, y))
                    _cells[x, y].Texture = ResolveBuildingTexture(sim.GetBuildingSpritePath(player, x, y));
                else
                    _cells[x, y].Texture = _cellTexture;
            }
        }

        if (!_hasPreview)
            return;

        for (int y = _previewY; y < _previewY + _previewHeight; y++)
        {
            for (int x = _previewX; x < _previewX + _previewWidth; x++)
            {
                if (x < 0 || y < 0 || x >= _width || y >= _height)
                    continue;

                _cells[x, y].Texture = _previewValid ? _validTexture : _invalidTexture;
            }
        }
    }

    public void SetPreview(int x, int y, int width, int height, bool isValid)
    {
        _hasPreview = true;
        _previewX = x;
        _previewY = y;
        _previewWidth = width;
        _previewHeight = height;
        _previewValid = isValid;
    }

    public void ClearPreview()
    {
        _hasPreview = false;
    }

    private Texture2D ResolveBuildingTexture(string spritePath)
    {
        if (string.IsNullOrEmpty(spritePath))
            return _cellTexture;

        if (!_buildingTextureCache.TryGetValue(spritePath, out var texture))
        {
            texture = GD.Load<Texture2D>(spritePath) ?? _cellTexture;
            _buildingTextureCache[spritePath] = texture;
        }

        return texture;
    }
}

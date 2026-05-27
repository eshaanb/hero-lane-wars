using Godot;
using LaneWars.Core;
using System.Collections.Generic;

namespace LaneWars.Buildings;

public partial class BuildZoneRenderer : Node2D
{
    [Export] public bool ObscureOccupiedCells { get; set; }

    private int _width;
    private int _height;
    private const float CellSize = 32.0f;
    private const float OverlayWidth = 92.0f;
    private const float OverlayHeight = 26.0f;
    private Texture2D _cellTexture = null!;
    private Texture2D _validTexture = null!;
    private Texture2D _invalidTexture = null!;
    private Sprite2D[,] _cells = null!;
    private readonly Dictionary<string, Texture2D> _buildingTextureCache = new();
    private readonly Dictionary<int, ConstructionOverlay> _constructionOverlays = new();
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
        HashSet<int> activeConstructionOverlays = new();

        for (int y = 0; y < _height; y++)
        {
            for (int x = 0; x < _width; x++)
            {
                if (sim.IsCellOccupied(player, x, y))
                {
                    bool hasBuildingInfo = sim.TryGetBuildingAt(player, x, y, out var buildingInfo);
                    Texture2D texture = ObscureOccupiedCells
                        ? _cellTexture
                        : ResolveBuildingTexture(hasBuildingInfo ? buildingInfo.SpritePath : sim.GetBuildingSpritePath(player, x, y));
                    _cells[x, y].Texture = texture;
                    _cells[x, y].Offset = ObscureOccupiedCells ? Vector2.Zero : CalculateCellBottomOffset(texture);
                    _cells[x, y].Modulate = ObscureOccupiedCells
                        ? new Color(0.42f, 0.46f, 0.54f, 1.0f)
                        : hasBuildingInfo && buildingInfo.IsUnderConstruction
                            ? new Color(0.66f, 0.76f, 1.0f, 0.72f)
                        : Colors.White;

                    if (!ObscureOccupiedCells &&
                        hasBuildingInfo &&
                        buildingInfo.IsUnderConstruction &&
                        buildingInfo.GridX == x &&
                        buildingInfo.GridY == y)
                    {
                        activeConstructionOverlays.Add(buildingInfo.BuildingId);
                        UpdateConstructionOverlay(buildingInfo);
                    }
                }
                else
                {
                    _cells[x, y].Texture = _cellTexture;
                    _cells[x, y].Offset = Vector2.Zero;
                    _cells[x, y].Modulate = Colors.White;
                }
            }
        }

        RemoveInactiveConstructionOverlays(activeConstructionOverlays);

        if (!_hasPreview)
            return;

        for (int y = _previewY; y < _previewY + _previewHeight; y++)
        {
            for (int x = _previewX; x < _previewX + _previewWidth; x++)
            {
                if (x < 0 || y < 0 || x >= _width || y >= _height)
                    continue;

                _cells[x, y].Texture = _previewValid ? _validTexture : _invalidTexture;
                _cells[x, y].Offset = Vector2.Zero;
                _cells[x, y].Modulate = Colors.White;
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

    private static Vector2 CalculateCellBottomOffset(Texture2D texture)
    {
        Vector2 size = texture.GetSize();
        if (size.Y <= CellSize)
            return Vector2.Zero;

        return new Vector2(0.0f, -(size.Y - CellSize) / 2.0f);
    }

    private void UpdateConstructionOverlay(BuildingRuntimeInfo info)
    {
        ConstructionOverlay overlay = GetOrCreateConstructionOverlay(info.BuildingId);
        float widthCells = info.GridWidth > 0 ? info.GridWidth : 1;
        float centerX = (info.GridX + widthCells * 0.5f) * CellSize;
        float topY = info.GridY * CellSize - OverlayHeight - 3.0f;

        overlay.Root.Position = new Vector2(centerX - OverlayWidth * 0.5f, topY);
        int remainingSec = (info.BuildDelayRemainingMs + 999) / 1000;
        overlay.Label.Text = $"{info.BuildingName} {remainingSec}s";
        overlay.Fill.Size = new Vector2(84.0f * Mathf.Clamp(info.ConstructionProgress, 0.0f, 1.0f), 4.0f);
    }

    private ConstructionOverlay GetOrCreateConstructionOverlay(int buildingId)
    {
        if (_constructionOverlays.TryGetValue(buildingId, out var overlay))
            return overlay;

        var root = new Control
        {
            MouseFilter = Control.MouseFilterEnum.Ignore,
            Size = new Vector2(OverlayWidth, OverlayHeight),
            ZIndex = 100
        };

        var back = new ColorRect
        {
            Color = new Color(0.04f, 0.06f, 0.10f, 0.88f),
            Position = Vector2.Zero,
            Size = new Vector2(OverlayWidth, OverlayHeight)
        };
        root.AddChild(back);

        var label = new Label
        {
            Position = new Vector2(4.0f, 0.0f),
            Size = new Vector2(OverlayWidth - 8.0f, 16.0f),
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            ClipText = true,
            TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis,
            MouseFilter = Control.MouseFilterEnum.Ignore
        };
        label.AddThemeColorOverride("font_color", new Color("f3f7ff"));
        root.AddChild(label);

        var barBg = new ColorRect
        {
            Color = new Color(0.16f, 0.20f, 0.28f, 1.0f),
            Position = new Vector2(4.0f, 18.0f),
            Size = new Vector2(84.0f, 4.0f)
        };
        root.AddChild(barBg);

        var fill = new ColorRect
        {
            Color = new Color(0.55f, 0.82f, 1.0f, 1.0f),
            Position = new Vector2(4.0f, 18.0f),
            Size = new Vector2(0.0f, 4.0f)
        };
        root.AddChild(fill);

        AddChild(root);
        overlay = new ConstructionOverlay(root, label, fill);
        _constructionOverlays[buildingId] = overlay;
        return overlay;
    }

    private void RemoveInactiveConstructionOverlays(HashSet<int> activeIds)
    {
        List<int> removeIds = new();
        foreach (var kvp in _constructionOverlays)
        {
            if (!activeIds.Contains(kvp.Key))
                removeIds.Add(kvp.Key);
        }

        for (int i = 0; i < removeIds.Count; i++)
        {
            int id = removeIds[i];
            _constructionOverlays[id].Root.QueueFree();
            _constructionOverlays.Remove(id);
        }
    }

    private readonly record struct ConstructionOverlay(Control Root, Label Label, ColorRect Fill);
}

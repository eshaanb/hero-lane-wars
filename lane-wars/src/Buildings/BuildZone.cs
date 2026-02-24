namespace LaneWars.Buildings;

using System.Collections.Generic;

public class BuildZone
{
    public int Width { get; }
    public int Height { get; }

    // Flat grid: cell value is building ID or null if empty.
    private readonly int?[] _grid;

    public BuildZone(int width, int height)
    {
        Width = width;
        Height = height;
        _grid = new int?[width * height];
    }

    /// <summary>
    /// Check whether a building of the given size can be placed at (x, y).
    /// (x, y) is the top-left corner. Returns false if out of bounds or overlapping.
    /// </summary>
    public bool CanPlace(int x, int y, int width, int height)
    {
        // Bounds check
        if (x < 0 || y < 0 || x + width > Width || y + height > Height)
            return false;

        // Overlap check
        for (int row = y; row < y + height; row++)
        {
            for (int col = x; col < x + width; col++)
            {
                if (_grid[row * Width + col].HasValue)
                    return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Place a building. Fills the grid cells with the buildingId.
    /// Returns false if placement is invalid.
    /// </summary>
    public bool Place(int x, int y, int width, int height, int buildingId)
    {
        if (!CanPlace(x, y, width, height))
            return false;

        for (int row = y; row < y + height; row++)
        {
            for (int col = x; col < x + width; col++)
            {
                _grid[row * Width + col] = buildingId;
            }
        }

        return true;
    }

    /// <summary>
    /// Remove a building by clearing all cells that contain the given ID.
    /// </summary>
    public void Remove(int buildingId)
    {
        for (int i = 0; i < _grid.Length; i++)
        {
            if (_grid[i].HasValue && _grid[i].Value == buildingId)
                _grid[i] = null;
        }
    }

    /// <summary>
    /// Count the number of distinct buildings currently placed on the grid.
    /// Uses ordered iteration over the flat array to collect unique IDs.
    /// </summary>
    public int GetBuildingCount()
    {
        List<int> seen = new();
        for (int i = 0; i < _grid.Length; i++)
        {
            if (_grid[i].HasValue)
            {
                int id = _grid[i].Value;
                bool found = false;
                for (int j = 0; j < seen.Count; j++)
                {
                    if (seen[j] == id)
                    {
                        found = true;
                        break;
                    }
                }
                if (!found)
                    seen.Add(id);
            }
        }
        return seen.Count;
    }

    /// <summary>Read a cell value (for testing / inspection).</summary>
    public int? GetCell(int x, int y)
    {
        if (x < 0 || y < 0 || x >= Width || y >= Height)
            return null;
        return _grid[y * Width + x];
    }
}

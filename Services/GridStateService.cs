using Microsoft.JSInterop;
using System.Text.Json;
using PencilVillageOnline.Models;

namespace PencilVillageOnline.Services;

public class GridStateService
{
    private readonly IJSRuntime _jsRuntime;
    private const string STORAGE_KEY = "grid-state";
    private const string TERRAIN_INITIALIZED_KEY = "terrain-initialized";

    public GridStateService(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;
    }

    public async Task SaveGridStateAsync(Dictionary<(int row, int col), Square> squares)
    {
        try
        {
            // Convert squares to serializable format
            var squareData = squares.Values.Select(square => new
            {
                Row = square.Row,
                Col = square.Col,
                Terrain = square.Terrain.ToString(),
                Building = square.Building.ToString()
            }).ToArray();

            var serializedData = JsonSerializer.Serialize(squareData);
            await _jsRuntime.InvokeVoidAsync("localStorage.setItem", STORAGE_KEY, serializedData);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error saving grid state: {ex.Message}");
        }
    }

    public async Task<Dictionary<(int row, int col), Square>> LoadGridStateAsync()
    {
        try
        {
            var serializedData = await _jsRuntime.InvokeAsync<string>("localStorage.getItem", STORAGE_KEY);
            if (string.IsNullOrEmpty(serializedData))
            {
                // Check if terrain was ever initialized
                var terrainInitialized = await _jsRuntime.InvokeAsync<string>("localStorage.getItem", TERRAIN_INITIALIZED_KEY);
                if (string.IsNullOrEmpty(terrainInitialized))
                {
                    // First time - generate default terrain
                    var defaultSquares = GenerateDefaultTerrain();
                    await SaveGridStateAsync(defaultSquares);
                    await _jsRuntime.InvokeVoidAsync("localStorage.setItem", TERRAIN_INITIALIZED_KEY, "true");
                    return defaultSquares;
                }
                return new Dictionary<(int row, int col), Square>();
            }

            using var document = JsonDocument.Parse(serializedData);
            var squares = new Dictionary<(int row, int col), Square>();

            foreach (var element in document.RootElement.EnumerateArray())
            {
                var row = element.GetProperty("Row").GetInt32();
                var col = element.GetProperty("Col").GetInt32();

                var terrainStr = element.GetProperty("Terrain").GetString();
                var buildingStr = element.GetProperty("Building").GetString();

                if (Enum.TryParse<TerrainType>(terrainStr, out var terrain) &&
                    Enum.TryParse<BuildingState>(buildingStr, out var building))
                {
                    var square = new Square(row, col, terrain, building);
                    squares[(row, col)] = square;
                }
            }

            return squares;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading grid state: {ex.Message}");
            return new Dictionary<(int row, int col), Square>();
        }
    }

    public async Task ClearGridStateAsync()
    {
        try
        {
            await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", STORAGE_KEY);
            await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", TERRAIN_INITIALIZED_KEY);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error clearing grid state: {ex.Message}");
        }
    }

    private Dictionary<(int row, int col), Square> GenerateDefaultTerrain()
    {
        var squares = new Dictionary<(int row, int col), Square>();

        // Grid is 57 columns wide, divide into 5 terrain columns
        // Column ranges: 0-10 (11), 11-22 (12), 23-34 (12), 35-46 (12), 47-56 (10)
        var terrainTypes = new[] { TerrainType.Woods, TerrainType.Rocky, TerrainType.Field, TerrainType.Scrub, TerrainType.Crag };
        var columnRanges = new[]
        {
            (start: 0, end: 10),   // Woods: 11 columns
            (start: 11, end: 22),  // Rocky: 12 columns  
            (start: 23, end: 34),  // Field: 12 columns
            (start: 35, end: 46),  // Scrub: 12 columns
            (start: 47, end: 56)   // Crag: 10 columns
        };

        for (int row = 0; row < 41; row++)
        {
            for (int col = 0; col < 57; col++)
            {
                var terrain = GetTerrainForColumn(col, terrainTypes, columnRanges);
                squares[(row, col)] = new Square(row, col, terrain);
            }
        }

        return squares;
    }

    private TerrainType GetTerrainForColumn(int col, TerrainType[] terrainTypes, (int start, int end)[] columnRanges)
    {
        for (int i = 0; i < columnRanges.Length; i++)
        {
            if (col >= columnRanges[i].start && col <= columnRanges[i].end)
            {
                return terrainTypes[i];
            }
        }

        // Fallback (shouldn't happen)
        return TerrainType.Field;
    }
}
using Microsoft.JSInterop;
using System.Text.Json;
using PencilVillageOnline.Models;

namespace PencilVillageOnline.Services;

public class GridStateService
{
    private readonly IJSRuntime _jsRuntime;
    private const string STORAGE_KEY = "grid-state";

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
                return new Dictionary<(int row, int col), Square>();

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
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error clearing grid state: {ex.Message}");
        }
    }
}
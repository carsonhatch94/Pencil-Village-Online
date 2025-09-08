using Microsoft.JSInterop;
using System.Text.Json;

namespace PencilVillageOnline.Services;

public class GridStateService
{
    private readonly IJSRuntime _jsRuntime;
    private const string STORAGE_KEY = "grid-state";

    public GridStateService(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;
    }

    public async Task SaveGridStateAsync(HashSet<(int row, int col)> activeSquares)
    {
        try
        {
            // Convert tuples to a simple array format for serialization
            var squareArray = activeSquares.Select(pos => new int[] { pos.row, pos.col }).ToArray();
            var serializedData = JsonSerializer.Serialize(squareArray);
            await _jsRuntime.InvokeVoidAsync("localStorage.setItem", STORAGE_KEY, serializedData);
        }
        catch (Exception ex)
        {
            // Log error or handle gracefully - for now just continue without saving
            Console.WriteLine($"Error saving grid state: {ex.Message}");
        }
    }

    public async Task<HashSet<(int row, int col)>> LoadGridStateAsync()
    {
        try
        {
            var serializedData = await _jsRuntime.InvokeAsync<string>("localStorage.getItem", STORAGE_KEY);
            if (string.IsNullOrEmpty(serializedData))
                return new HashSet<(int row, int col)>();

            var squareArray = JsonSerializer.Deserialize<int[][]>(serializedData);
            if (squareArray == null)
                return new HashSet<(int row, int col)>();

            // Convert back to tuples
            return squareArray
                .Where(arr => arr.Length == 2)
                .Select(arr => (row: arr[0], col: arr[1]))
                .ToHashSet();
        }
        catch (Exception ex)
        {
            // Log error or handle gracefully - for now return empty set
            Console.WriteLine($"Error loading grid state: {ex.Message}");
            return new HashSet<(int row, int col)>();
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
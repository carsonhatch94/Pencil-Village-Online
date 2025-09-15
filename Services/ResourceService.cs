using Microsoft.JSInterop;
using System.Text.Json;
using PencilVillageOnline.Models;

namespace PencilVillageOnline.Services
{
    public class ResourceService
    {
        private readonly IJSRuntime _jsRuntime;
        private readonly GridStateService _gridState;
        private const string RESOURCES_STORAGE_KEY = "player-resources";

        private readonly Resources _currentResources = new();
        public Resources CurrentResources => _currentResources;

        public event Action? OnResourcesChanged;

        public ResourceService(IJSRuntime jsRuntime, GridStateService gridState)
        {
            _jsRuntime = jsRuntime;
            _gridState = gridState;
        }

        // Depot management methods
        public async Task<bool> TryAddDepotAsync()
        {
            if (_currentResources.GetDepotCount() >= 4) return false;

            _currentResources.SetDepotCount(_currentResources.GetDepotCount() + 1);
            _currentResources.UpdateMaxStorageFromDepots();

            await SaveAndNotifyAsync();
            return true;
        }

        public async Task<bool> TryRemoveDepotAsync()
        {
            if (_currentResources.GetDepotCount() <= 0) return false;

            _currentResources.SetDepotCount(_currentResources.GetDepotCount() - 1);
            _currentResources.UpdateMaxStorageFromDepots();

            // Adjust resources if they exceed new max storage
            await AdjustResourcesToMaxStorage();

            await SaveAndNotifyAsync();
            return true;
        }

        private async Task AdjustResourcesToMaxStorage()
        {
            var maxStorage = _currentResources.GetMaxStorage();

            // Reduce each resource to max storage if it exceeds the limit
            if (_currentResources.GetWood() > maxStorage)
                _currentResources.SetWood(maxStorage);

            if (_currentResources.GetStone() > maxStorage)
                _currentResources.SetStone(maxStorage);

            if (_currentResources.GetGold() > maxStorage)
                _currentResources.SetGold(maxStorage);
        }

        // Business logic methods
        public async Task<bool> TryAddWoodAsync(int amount)
        {
            if (amount <= 0) return false;

            var newAmount = Math.Min(_currentResources.GetMaxStorage(),
                                   _currentResources.GetWood() + amount);
            _currentResources.SetWood(newAmount);

            await SaveAndNotifyAsync();
            return true;
        }

        public async Task<bool> TryAddStoneAsync(int amount)
        {
            if (amount <= 0) return false;

            var newAmount = Math.Min(_currentResources.GetMaxStorage(),
                                   _currentResources.GetStone() + amount);
            _currentResources.SetStone(newAmount);

            await SaveAndNotifyAsync();
            return true;
        }

        public async Task<bool> TryAddGoldAsync(int amount)
        {
            if (amount <= 0) return false;

            var newAmount = Math.Min(_currentResources.GetMaxStorage(),
                                   _currentResources.GetGold() + amount);
            _currentResources.SetGold(newAmount);

            await SaveAndNotifyAsync();
            return true;
        }

        public async Task<bool> TrySpendResourcesAsync(int wood, int stone, int gold)
        {
            if (!_currentResources.HasEnoughResources(wood, stone, gold))
                return false;

            _currentResources.SetWood(_currentResources.GetWood() - wood);
            _currentResources.SetStone(_currentResources.GetStone() - stone);
            _currentResources.SetGold(_currentResources.GetGold() - gold);

            await SaveAndNotifyAsync();
            return true;
        }

        public async Task UpdateMaxStorageFromDepotsAsync()
        {
            var squares = await _gridState.LoadGridStateAsync();
            var depotCount = CountDepots(squares);

            _currentResources.SetDepotCount(depotCount);
            _currentResources.UpdateMaxStorageFromDepots();

            // Adjust resources if they exceed new max storage
            await AdjustResourcesToMaxStorage();

            await SaveAndNotifyAsync();
        }

        // Persistence methods
        public async Task LoadResourcesAsync()
        {
            try
            {
                var serializedData = await _jsRuntime.InvokeAsync<string>("localStorage.getItem", RESOURCES_STORAGE_KEY);
                if (!string.IsNullOrEmpty(serializedData))
                {
                    var resourceData = JsonSerializer.Deserialize<ResourceData>(serializedData);
                    if (resourceData != null)
                    {
                        _currentResources.SetWood(resourceData.Wood);
                        _currentResources.SetStone(resourceData.Stone);
                        _currentResources.SetGold(resourceData.Gold);
                        _currentResources.SetDepotCount(resourceData.DepotCount);
                        _currentResources.UpdateMaxStorageFromDepots();

                        // Ensure resources don't exceed max storage after loading
                        await AdjustResourcesToMaxStorage();
                    }
                }
                else
                {
                    // Initialize with default values if no saved data
                    _currentResources.UpdateMaxStorageFromDepots();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading resources: {ex.Message}");
            }
        }

        private async Task SaveAndNotifyAsync()
        {
            await SaveResourcesAsync();
            OnResourcesChanged?.Invoke();
        }

        private async Task SaveResourcesAsync()
        {
            try
            {
                var resourceData = new ResourceData
                {
                    Wood = _currentResources.GetWood(),
                    Stone = _currentResources.GetStone(),
                    Gold = _currentResources.GetGold(),
                    DepotCount = _currentResources.GetDepotCount()
                };

                var serializedData = JsonSerializer.Serialize(resourceData);
                await _jsRuntime.InvokeVoidAsync("localStorage.setItem", RESOURCES_STORAGE_KEY, serializedData);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving resources: {ex.Message}");
            }
        }

        private int CountDepots(Dictionary<(int row, int col), Square> squares)
        {
            // For now, this counts all PartOfBuilding as depots
            // You can update this logic based on how you identify depots specifically
            return Math.Min(squares.Values.Count(s => s.Building == BuildingState.PartOfBuilding), 4);
        }

        private class ResourceData
        {
            public int Wood { get; set; }
            public int Stone { get; set; }
            public int Gold { get; set; }
            public int DepotCount { get; set; }
        }
    }
}
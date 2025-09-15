namespace PencilVillageOnline.Models
{
    public class Resources
    {
        private int Wood { get; set; }
        private int Stone { get; set; }
        private int Gold { get; set; }
        private int MaxStorage { get; set; } = 4;
        private int DepotCount { get; set; } = 0;

        // Get methods only
        public int GetWood() => Wood;
        public int GetStone() => Stone;
        public int GetGold() => Gold;
        public int GetMaxStorage() => MaxStorage;
        public int GetDepotCount() => DepotCount;

        // Internal set methods (only for service use)
        internal void SetWood(int amount) => Wood = amount;
        internal void SetStone(int amount) => Stone = amount;
        internal void SetGold(int amount) => Gold = amount;
        internal void SetMaxStorage(int amount) => MaxStorage = Math.Max(4, amount); // Minimum base storage of 4
        internal void SetDepotCount(int count) => DepotCount = Math.Max(0, Math.Min(count, 4)); // 0-4 depots max

        // Validation helpers
        public bool HasEnoughResources(int wood, int stone, int gold) =>
            Wood >= wood && Stone >= stone && Gold >= gold;

        public bool IsStorageFull() =>
            Wood >= MaxStorage || Stone >= MaxStorage || Gold >= MaxStorage;

        // Calculate max storage based on depot count
        internal void UpdateMaxStorageFromDepots() =>
            SetMaxStorage(4 + (DepotCount * 2));
    }
}
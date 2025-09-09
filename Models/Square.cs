namespace PencilVillageOnline.Models;

public class Square
{
    public int Row { get; set; }
    public int Col { get; set; }
    public TerrainType Terrain { get; set; } = TerrainType.Field;
    public BuildingState Building { get; set; } = BuildingState.None;

    public Square(int row, int col)
    {
        Row = row;
        Col = col;
    }

    public Square(int row, int col, TerrainType terrain, BuildingState building = BuildingState.None)
    {
        Row = row;
        Col = col;
        Terrain = terrain;
        Building = building;
    }

    public override bool Equals(object? obj)
    {
        return obj is Square square && Row == square.Row && Col == square.Col;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Row, Col);
    }

    public override string ToString()
    {
        return $"Square({Row}, {Col}) - {Terrain}, {Building}";
    }
}
using Godot;

// Author : Alexy HOUBLOUP

[GlobalClass]
public partial class TileLevelData : Resource
{
    [Export] public TileType tileType = TileType.None;
    [Export] public string character;

    [Export] public bool blockPath;

    [Export] public int layer;
    [Export] public Vector2I tileMapIndex;
}

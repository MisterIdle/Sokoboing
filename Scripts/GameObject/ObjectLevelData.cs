using Godot;

// Author : Alexy HOUBLOUP

[GlobalClass]
public partial class ObjectLevelData : Resource
{
    [Export] public TileType tileType = TileType.None;
    [Export] public string character;

    [Export] public int layer;

    [Export] public bool blockPath;

    [Export] public PackedScene scene;
}
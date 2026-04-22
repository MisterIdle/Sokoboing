using Godot;
using System;
using Com.IsartDigital.Sokoban;

public partial class FireworkStarGenerator : Node2D
{
    [Export] public PackedScene fireworkScene;

    public static FireworkStarGenerator Instance;

    private Vector2 screenSize;


    public override void _Ready()
    {
        if (Instance == null)
            Instance = this;
        else
        {
            QueueFree();
            return;
        }

        screenSize = GetViewportRect().Size;
    }

    public static FireworkStarGenerator GetInstance()
    {
        return Instance;
    }

    public void SpawnFirework(Vector2 pPosition)
    {
        Firework lFireworkInstance = fireworkScene.Instantiate<Firework>();
        lFireworkInstance.Position = pPosition;

        AddChild(lFireworkInstance);

        lFireworkInstance.Emitting = true;
    }

    protected override void Dispose(bool disposing)
    {
        Instance = null;
        base.Dispose(disposing);
    }
}
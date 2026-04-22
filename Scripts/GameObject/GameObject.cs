using Godot;
using System;

// Author : Alexy HOUBLOUP

namespace Com.IsartDigital.Sokoban;

public partial class GameObject : Area2D
{

    [Export] protected Texture2D placeholder;
    [Export] protected Texture2D final;

    public override void _Ready()
    {
        AreaEntered += OnCollideGameObject;
        BodyEntered += OnCollideTile;
        AreaExited += OnExitCollideGameObject;
        BodyExited += OnExitCollideTile;

        GameManager.OnVisualModeChanged += UpdateVisualMode;

        CallDeferred(nameof(UpdateVisualMode), GameManager.IsPlaceholderMode);

        GD.Print(Name + " is ready.");
    }

    /// <summary>
    /// Process method with float delta.
    /// </summary>
    /// <param name="pDelta"></param>
    public virtual void Update(float pDelta)
    {
    }

    /// <summary>
    /// Place the GameObject at a specific position in the grid.
    /// </summary>
    /// <param name="pPos"></param>
    public virtual void PlaceAt(Vector2I pPos)
    {
        Position = pPos;
    }

    public virtual void OnCollideTile(Node2D pBody) 
    {
        GD.Print(Name + " collided with tile: " + pBody.Name);
    }

    public virtual void OnCollideGameObject(Area2D pArea) 
    {
        GD.Print(Name + " collided with game object: " + pArea.Name);
    }

    public virtual void OnExitCollideGameObject(Area2D pArea) 
    {
        GD.Print(Name + " exited collision with game object: " + pArea.Name);
    }

    public virtual void OnExitCollideTile(Node2D pBody) 
    {
        GD.Print(Name + " exited collision with tile: " + pBody.Name);
    }

    protected virtual void UpdateVisualMode(bool pIsPlaceholder)
    {
    }

    protected override void Dispose(bool pDisposing)
    {
        GameManager.OnVisualModeChanged -= UpdateVisualMode;
        base.Dispose(pDisposing);
    }
}

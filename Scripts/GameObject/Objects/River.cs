using Godot;

namespace Com.IsartDigital.Sokoban;

public partial class River : GameObject
{
    [Export] private Sprite2D sprite;

    public override void _Ready()
    {
        base._Ready();

        if (sprite != null)
        {
            Effects.PlayIdleLoop(sprite, 0.06f, 1.5f, 0.8f);
        }
    }

    protected override void UpdateVisualMode(bool pIsPlaceholder)
    {
        base.UpdateVisualMode(pIsPlaceholder);

        if (sprite != null)
        {
            sprite.Texture = pIsPlaceholder ? placeholder : final;
        }
    }
}
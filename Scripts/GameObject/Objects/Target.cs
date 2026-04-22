using Godot;

namespace Com.IsartDigital.Sokoban;

public partial class Target : GameObject
{
    [Export] private Sprite2D sprite;

    public override void _Ready()
    {
        base._Ready();
        GameManager.GetInstance().NbTargetInLevel++;

        if (sprite != null)
        {
            Effects.PlayIdleLoop(sprite, 0.08f, 1.1f, 0.7f);
        }
    }

    public override void _ExitTree()
    {
        if (GameManager.GetInstance() != null)
            GameManager.GetInstance().NbTargetInLevel--;

        base._ExitTree();
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

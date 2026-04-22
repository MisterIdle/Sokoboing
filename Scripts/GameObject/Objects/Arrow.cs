using Godot;

namespace Com.IsartDigital.Sokoban;

public partial class Arrow : GameObject
{
    [Export] private Sprite2D sprite;
    [Export] private Sprite2D spriteEffect;

    static private string leftArrow = "<";
    static private string rightArrow = ">";
    static private string upArrow = "^";
    static private string downArrow = "v";

    public Vector2 direction;
    Tween tween;
    private bool isInAction;

    public override void _Ready()
    {
        base._Ready();
        AnimateIdleArrow();
    }

    protected override void UpdateVisualMode(bool pIsPlaceholder)
    {
        base.UpdateVisualMode(pIsPlaceholder);

        if (sprite != null)
        {
            sprite.Texture = pIsPlaceholder ? placeholder : final;
            spriteEffect.Texture = pIsPlaceholder ? placeholder : final;
        }
    }

    private void AnimateIdleArrow()
    {
        Vector2 lTransformedArrowSize = new Vector2(0.9f, 1.1f);
        float lDuration = 0.8f;

        tween?.Kill();

        tween = CreateTween();

        tween.TweenProperty(sprite, (string)Node2D.PropertyName.Scale, lTransformedArrowSize, lDuration)
             .SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.InOut);

        tween.TweenProperty(sprite, (string)Node2D.PropertyName.Scale, Vector2.One, lDuration)
             .SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.InOut);

        tween.Finished += AnimateIdleArrow;
    }

    private void AnimateUsedArrow()
    {
        float lScaleFactor = 0.3f;
        float lDuration = 0.2f;
        Vector2 lScale = RotationDegrees == 0 || RotationDegrees == 180 ? new Vector2(1 + lScaleFactor, 1 - lScaleFactor) : new Vector2(1 - lScaleFactor, 1 + lScaleFactor);

        isInAction = true;

        tween?.Kill();

        tween = CreateTween();
        tween.SetParallel(true);

        tween.TweenProperty(sprite, (string)Node2D.PropertyName.Scale, lScale, lDuration)
             .SetTrans(Tween.TransitionType.Quad).SetEase(Tween.EaseType.Out);

        tween.Chain().TweenProperty(sprite, (string)Node2D.PropertyName.Scale, Vector2.One, lDuration * 1.5f)
             .SetTrans(Tween.TransitionType.Elastic).SetEase(Tween.EaseType.Out);

        spriteEffect.Scale = Vector2.One * 0.8f;
        spriteEffect.SelfModulate = new Color(1, 1, 1, 0.5f);

        tween.Parallel().TweenProperty(spriteEffect, (string)Node2D.PropertyName.Scale, new Vector2(1.6f, 1.6f), lDuration * 2.5f)
             .SetTrans(Tween.TransitionType.Quad).SetEase(Tween.EaseType.Out);

        tween.Parallel().TweenProperty(spriteEffect, (string)Node2D.PropertyName.SelfModulate, new Color(1, 1, 1, 0), lDuration * 2.5f)
             .SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.Out);

        tween.Chain().TweenCallback(Callable.From(ActionFinished));
    }

    private void ActionFinished()
    {
        isInAction = false;
        AnimateIdleArrow();
    }

    public override void Update(float pDelta)
    {
        base.Update(pDelta);
    }

    public void IniatiateArrowBehavior(char pArrowDirection)
    {
        if (pArrowDirection.ToString() == rightArrow)
        {
            direction = Vector2.Right;
            RotationDegrees = 90;
        }

        if (pArrowDirection.ToString() == leftArrow)
        {
            direction = Vector2.Left;
            RotationDegrees = -90;
        }

        if (pArrowDirection.ToString() == upArrow)
        {
            direction = Vector2.Up;
            RotationDegrees = 0;
        }

        if (pArrowDirection.ToString() == downArrow)
        {
            direction = Vector2.Down;
            RotationDegrees = 180;
        }
    }

    public override void OnCollideGameObject(Area2D pArea)
    {
        if (pArea is Box lBox)
        {
            lBox.direction = (Vector2I)direction;
            AnimateUsedArrow();
        }
    }
}

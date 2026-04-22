// Help.cs
using Godot;

namespace Com.IsartDigital.Sokoban;

public partial class Help : Screen
{
    [ExportGroup("UI References")]
    [Export] private VBoxContainer helpA;
    [Export] private VBoxContainer helpB;
    [Export] private TextureButton nextBtn;
    [Export] private TextureButton backBtn;

    [ExportGroup("Animation Settings")]
    [Export] private float childTweenDuration = 0.6f;
    [Export] private float childDecalage = 0.15f;
    [Export] private Vector2 childStartOffset = new Vector2(0, 30f);

    private Color invisibleColor = new Color(1, 1, 1, 0);
    private Color visibleColor = new Color(1, 1, 1, 1);

    public override void _Ready()
    {
        base._Ready();
        if (backBtn != null) backBtn.Pressed += BackBtn_Pressed;
        if (nextBtn != null) nextBtn.Pressed += NextBtn_Pressed;
    }

    public override void Load()
    {
        base.Load();

        helpA.Visible = true;
        helpB.Visible = false;
        nextBtn.Visible = true;
        nextBtn.Disabled = false;

        HideChildren(helpA);
        HideChildren(helpB);
        backBtn.SelfModulate = invisibleColor;
        nextBtn.SelfModulate = invisibleColor;

        CallDeferred(nameof(PlayHelpAAnimation));
    }

    private void PlayHelpAAnimation()
    {
        AnimateContainerIn(helpA, true);
    }

    private void HideChildren(Container pContainer)
    {
        foreach (Node child in pContainer.GetChildren())
        {
            if (child is CanvasItem canvasChild)
                canvasChild.SelfModulate = invisibleColor;
        }
    }

    private void AnimateContainerIn(Container pContainer, bool pShowNextButton)
    {
        pContainer.Visible = true;
        Tween lTween = CreateTween();
        lTween.SetParallel(true);

        float currentDelay = 0.2f;

        foreach (Node child in pContainer.GetChildren())
        {
            if (child is Control controlChild)
            {
                Vector2 targetPos = controlChild.Position;

                lTween.TweenProperty(controlChild, (string)Control.PropertyName.Position, targetPos, childTweenDuration)
                      .From(targetPos + childStartOffset)
                      .SetTrans(Tween.TransitionType.Back)
                      .SetEase(Tween.EaseType.Out)
                      .SetDelay(currentDelay);

                lTween.TweenProperty(controlChild, (string)CanvasItem.PropertyName.SelfModulate, visibleColor, childTweenDuration)
                      .SetTrans(Tween.TransitionType.Linear)
                      .SetDelay(currentDelay);

                currentDelay += childDecalage;
            }
        }

        lTween.TweenProperty(backBtn, (string)CanvasItem.PropertyName.SelfModulate, visibleColor, 0.5f)
              .SetDelay(currentDelay);

        if (pShowNextButton && nextBtn != null)
        {
            lTween.TweenProperty(nextBtn, (string)CanvasItem.PropertyName.SelfModulate, visibleColor, 0.5f)
                  .SetDelay(currentDelay);
        }
    }

    private void NextBtn_Pressed()
    {
        nextBtn.Disabled = true;

        Tween lTween = CreateTween();

        lTween.SetParallel(true);
        float currentDelay = 0f;

        foreach (Node child in helpA.GetChildren())
        {
            if (child is Control controlChild)
            {
                Vector2 targetPos = controlChild.Position;

                lTween.TweenProperty(controlChild, (string)Control.PropertyName.Position, targetPos - childStartOffset, childTweenDuration / 2)
                      .SetTrans(Tween.TransitionType.Sine)
                      .SetEase(Tween.EaseType.In)
                      .SetDelay(currentDelay);

                lTween.TweenProperty(controlChild, (string)CanvasItem.PropertyName.SelfModulate, invisibleColor, childTweenDuration / 2)
                      .SetTrans(Tween.TransitionType.Linear)
                      .SetDelay(currentDelay);

                currentDelay += childDecalage / 2;
            }
        }

        lTween.TweenProperty(nextBtn, (string)CanvasItem.PropertyName.SelfModulate, invisibleColor, childTweenDuration / 2);

        lTween.Chain().TweenCallback(Callable.From(PrepareHelpB));
    }

    private void PrepareHelpB()
    {
        helpA.Visible = false;
        nextBtn.Visible = false;
        helpB.Visible = true;

        CallDeferred(nameof(PlayHelpBAnimation));
    }

    private void PlayHelpBAnimation()
    {
        AnimateContainerIn(helpB, false);
    }

    private void BackBtn_Pressed()
    {
        if (MenuManager.GetInstance().pendingGameLaunch)
        {
            MenuManager.GetInstance().StartPendingGame(this);
        }
        else
        {
            MenuManager.GetInstance().ChangeMenu(this, (int)MenuList.TITLE);
        }
    }
}

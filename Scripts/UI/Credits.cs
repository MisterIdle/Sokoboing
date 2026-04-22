using Godot;

namespace Com.IsartDigital.Sokoban;

public partial class Credits : Screen
{
    [ExportGroup("UI References")]
    [Export] private ScrollContainer scrollContainer;
    [Export] private Container creditsContainer;
    [Export] private TextureButton backBtn;

    [ExportGroup("Animation Settings")]
    [Export] private float childTweenDuration = 0.6f;
    [Export] private float childStartDelay = 0.2f;
    [Export] private float childDecalage = 0.15f;
    [Export] private Vector2 childStartOffset = new Vector2(0, 30f);
    [Export] private float buttonDelay = 1.2f;

    private Color invisibleColor = new Color(1, 1, 1, 0);
    private Color visibleColor = new Color(1, 1, 1, 1);

    [ExportGroup("Scroll Settings")]
    [Export] private float scrollDuration = 15f;
    [Export] private float startDelay = 1.0f;
    [Export] private float scrollBackDuration = 0.8f;
    [Export] private float pauseAtBottom = 1.0f;

    private Tween scrollTween;

    public override void _Ready()
    {
        base._Ready();

        if (backBtn != null)
        {
            backBtn.Pressed += BackBtn;
        }

        if (scrollContainer != null)
        {
            scrollContainer.MouseFilter = Control.MouseFilterEnum.Ignore;
            scrollContainer.GetVScrollBar().MouseFilter = Control.MouseFilterEnum.Ignore;
            scrollContainer.GetHScrollBar().MouseFilter = Control.MouseFilterEnum.Ignore;
        }
    }

    public override void Load()
    {
        base.Load();

        if (backBtn != null)
            backBtn.SelfModulate = invisibleColor;

        if (creditsContainer != null)
        {
            foreach (Node child in creditsContainer.GetChildren())
            {
                if (child is CanvasItem canvasChild)
                {
                    canvasChild.SelfModulate = invisibleColor;
                }
            }
        }

        CallDeferred(nameof(AnimateCredits));
        CallDeferred(nameof(StartAutoScroll));
    }

    private void StartAutoScroll()
    {
        if (scrollTween != null && scrollTween.IsValid())
        {
            scrollTween.Kill();
        }

        if (scrollContainer != null)
        {
            scrollContainer.ScrollVertical = 0;
            VScrollBar lScrollBar = scrollContainer.GetVScrollBar();
            double lMaxScroll = lScrollBar.MaxValue - lScrollBar.Page;

            scrollTween = CreateTween().SetLoops();

            scrollTween.TweenProperty(scrollContainer, "scroll_vertical", lMaxScroll, scrollDuration)
                       .SetTrans(Tween.TransitionType.Linear)
                       .SetDelay(startDelay);

            scrollTween.TweenProperty(scrollContainer, "scroll_vertical", 0, scrollBackDuration)
                       .SetTrans(Tween.TransitionType.Sine)
                       .SetEase(Tween.EaseType.InOut)
                       .SetDelay(pauseAtBottom);
        }
    }

    private void AnimateCredits()
    {
        Tween lTween = CreateTween();
        lTween.SetParallel(true);

        float currentDelay = childStartDelay;

        if (creditsContainer != null)
        {
            foreach (Node child in creditsContainer.GetChildren())
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
        }

        if (backBtn != null)
        {
            float finalButtonDelay = Mathf.Max(currentDelay, buttonDelay);
            lTween.TweenProperty(backBtn, (string)CanvasItem.PropertyName.SelfModulate, visibleColor, 0.5f)
                  .From(invisibleColor)
                  .SetDelay(finalButtonDelay);
        }
    }

    private void BackBtn()
    {
        if (scrollTween != null && scrollTween.IsValid())
        {
            scrollTween.Kill();
        }
        MenuManager.GetInstance().ChangeMenu(this, (int)MenuList.TITLE);
    }
}

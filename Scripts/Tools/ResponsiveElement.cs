using Godot;

[Tool]
public partial class ResponsiveElement : TextureButton
{
    public enum ExitDirection
    {
        NONE,
        LEFT,
        RIGHT,
        UP,
        DOWN
    }

    [ExportGroup("Animation")]
    [Export] public ExitDirection outDirection = ExitDirection.NONE;

    [ExportGroup("Hover Settings")]
    [Export] private Vector2 hoverScale = new Vector2(1.05f, 1.05f);
    [Export] private Color hoverColor = new Color(0.85f, 0.85f, 0.85f, 1f);
    [Export] private float hoverDuration = 0.15f;

    [ExportGroup("PC Layout")]
    [Export] private Vector2 pcAnchorMin;
    [Export] private Vector2 pcAnchorMax;
    [Export] private Vector2 pcOffsetMin;
    [Export] private Vector2 pcOffsetMax;

    [ExportGroup("Phone Layout")]
    [Export] private Vector2 phoneAnchorMin;
    [Export] private Vector2 phoneAnchorMax;
    [Export] private Vector2 phoneOffsetMin;
    [Export] private Vector2 phoneOffsetMax;

    [ExportGroup("Actions")]
    [Export]
    public bool saveCurrentAsPC
    {
        get => false;
        set
        {
            if (value)
            {
                pcAnchorMin = new Vector2(AnchorLeft, AnchorTop);
                pcAnchorMax = new Vector2(AnchorRight, AnchorBottom);
                pcOffsetMin = new Vector2(OffsetLeft, OffsetTop);
                pcOffsetMax = new Vector2(OffsetRight, OffsetBottom);
                GD.Print($"{Name} : PC layout save");
            }
        }
    }

    [Export]
    public bool saveCurrentAsPhone
    {
        get => false;
        set
        {
            if (value)
            {
                phoneAnchorMin = new Vector2(AnchorLeft, AnchorTop);
                phoneAnchorMax = new Vector2(AnchorRight, AnchorBottom);
                phoneOffsetMin = new Vector2(OffsetLeft, OffsetTop);
                phoneOffsetMax = new Vector2(OffsetRight, OffsetBottom);
                GD.Print($"{Name} : Phone Layout save");
            }
        }
    }

    private Vector2 baseScale;
    private Color baseColor;
    private Tween hoverTween;

    public override void _Ready()
    {
        if (Engine.IsEditorHint())
            return;

        string lGroupName = "ResponsiveUI";
        AddToGroup(lGroupName);

        baseScale = Scale;
        baseColor = SelfModulate;

        Resized += OnResized;
        MouseEntered += OnMouseEntered;
        MouseExited += OnMouseExited;

        GetTree().Root.SizeChanged += UpdateLayout;
        UpdateLayout();
    }

    private void OnResized()
    {
        PivotOffset = Size / 2;
    }

    private void UpdateLayout()
    {
        Vector2I lScreenSize = GetWindow().Size;
        bool lIsPortrait = lScreenSize.Y > lScreenSize.X;

        if (lIsPortrait)
            ApplyLayout(phoneAnchorMin, phoneAnchorMax, phoneOffsetMin, phoneOffsetMax);
        else
            ApplyLayout(pcAnchorMin, pcAnchorMax, pcOffsetMin, pcOffsetMax);
    }

    private void ApplyLayout(Vector2 lAnchorMin, Vector2 lAnchorMax, Vector2 lOffsetMin, Vector2 lOffsetMax)
    {
        AnchorLeft = lAnchorMin.X;
        AnchorTop = lAnchorMin.Y;
        AnchorRight = lAnchorMax.X;
        AnchorBottom = lAnchorMax.Y;

        OffsetLeft = lOffsetMin.X;
        OffsetTop = lOffsetMin.Y;
        OffsetRight = lOffsetMax.X;
        OffsetBottom = lOffsetMax.Y;
    }

    private void OnMouseEntered()
    {
        if (hoverTween != null && hoverTween.IsValid())
            hoverTween.Kill();

        hoverTween = CreateTween().SetParallel(true);
        hoverTween.TweenProperty(this, (string)Control.PropertyName.Scale, hoverScale, hoverDuration)
                  .SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.Out);
        hoverTween.TweenProperty(this, (string)CanvasItem.PropertyName.SelfModulate, hoverColor, hoverDuration)
                  .SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.Out);
    }

    private void OnMouseExited()
    {
        if (hoverTween != null && hoverTween.IsValid())
            hoverTween.Kill();

        hoverTween = CreateTween().SetParallel(true);
        hoverTween.TweenProperty(this, (string)Control.PropertyName.Scale, baseScale, hoverDuration)
                  .SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.Out);
        hoverTween.TweenProperty(this, (string)CanvasItem.PropertyName.SelfModulate, baseColor, hoverDuration)
                  .SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.Out);
    }

    public override void _ExitTree()
    {
        if (Engine.IsEditorHint())
            return;

        GetTree().Root.SizeChanged -= UpdateLayout;
    }
}
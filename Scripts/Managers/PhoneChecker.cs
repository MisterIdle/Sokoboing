using Godot;

namespace Com.IsartDigital.Sokoban;

public partial class PhoneChecker : Node2D
{
    [ExportGroup("Global Zoom")]

    [Export(PropertyHint.Range, "0.1, 5.0, 0.05")]
    public float pcScaleMultiplier = 1.0f;

    [Export(PropertyHint.Range, "0.1, 5.0, 0.05")]
    public float phonePortraitMultiplier = 1.0f;

    [Export(PropertyHint.Range, "0.1, 5.0, 0.05")]
    public float phoneLandscapeMultiplier = 1.5f;

    [ExportGroup("Detection Settings")]
    [Export] private int pcHeightThreshold = 600;

    public override void _Ready()
    {
        base._Ready();
        GetTree().Root.SizeChanged += UpdateGlobalZoom;

        CallDeferred(nameof(UpdateGlobalZoom));
    }

    public override void _Input(InputEvent pEvent)
    {
        if (pEvent.IsActionPressed(Input.UI_COMPUTER_VIEW))
            DisplayServer.WindowSetSize(new Vector2I(1280, 720));

        if (pEvent.IsActionPressed(Input.UI_PHONE_VIEW))
            DisplayServer.WindowSetSize(new Vector2I(320, 480));
    }

    public void UpdateGlobalZoom()
    {
        Vector2I lScreenSize = GetWindow().Size;
        bool lIsPortrait = lScreenSize.Y > lScreenSize.X;

        float lCurrentScale;

        if (lIsPortrait)
        {
            lCurrentScale = phonePortraitMultiplier;
        }
        else
        {
            string osName = OS.GetName();
            bool isMobileOS = osName == "Android" || osName == "iOS";
            bool isSmallScreen = lScreenSize.Y < pcHeightThreshold;

            if (isMobileOS || isSmallScreen)
            {
                lCurrentScale = phoneLandscapeMultiplier;
            }
            else
            {
                lCurrentScale = pcScaleMultiplier;
            }
        }

        GetWindow().ContentScaleFactor = lCurrentScale;
    }

    public override void _ExitTree()
    {
        GetTree().Root.SizeChanged -= UpdateGlobalZoom;
    }
}
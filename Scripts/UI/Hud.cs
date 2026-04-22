using System;
using Godot;
using Godot.Collections;

namespace Com.IsartDigital.Sokoban;

public partial class Hud : Screen
{
    [Export] public Label levelID;
    [Export] public Label levelName;
    [Export] public Label levelMaxPar;
    [Export] public Label stepsLabel;

    [Export] public ResponsiveElement backBtn;
    [Export] public ResponsiveElement undoBtn;
    [Export] public ResponsiveElement resetBtn;
    [Export] public ResponsiveElement redoBtn;

    private static Hud instance;
    private const string HUD_TEXT = "ID_LEVEL";
    private const string PAR_TEXT = "ID_PAR";
    private const string ID_NAME = "ID_LEVEL_NAME";
    private const string ID_MAX_PAR = "ID_MAX_PAR";

    [Export] public Json jsonLevelDesign;

    [ExportGroup("Animation Settings")]
    [Export] private float animDuration = 0.6f;
    [Export] private float animDecalage = 0.1f;

    private Color alphaZero = new Color(1, 1, 1, 0);
    private Color alphaOne = new Color(1, 1, 1, 1);

    public int Steps { get; set; }

    static public Hud GetInstance()
    {
        return instance;
    }

    public override void _Ready()
    {
        base._Ready();

        if (instance == null)
            instance = this;
        else
        {
            QueueFree();
            return;
        }

        backBtn.Connect(TextureButton.SignalName.Pressed, Callable.From(BackButtonPressed));
        undoBtn.Connect(TextureButton.SignalName.Pressed, Callable.From(UndoButtonPressed));
        resetBtn.Connect(TextureButton.SignalName.Pressed, Callable.From(ResetButtonPressed));
        redoBtn.Connect(TextureButton.SignalName.Pressed, Callable.From(RedoButtonPressed));

        Control[] elements = { levelID, levelName, levelMaxPar, stepsLabel, backBtn, undoBtn, resetBtn, redoBtn };
        foreach (Control element in elements)
        {
            if (element != null) element.Modulate = alphaZero;
        }
    }

    public override void _Process(double delta)
    {
        stepsLabel.Text = Tr(PAR_TEXT) + " " + Steps;
    }

    public void Launch(int pLevel)
    {
        string lDataLevel = "levelDesign";
        string lNameLevel = "name";
        string lParLevel = "par";

        LevelWinScreen.GetInstance().LevelNumberId = pLevel;

        if (levelID != null)
            levelID.Text = Tr(HUD_TEXT) + ": " + pLevel;

        if (levelName != null && jsonLevelDesign != null)
        {
            Dictionary lData = (Dictionary)jsonLevelDesign.Data;
            Godot.Collections.Array lLevelDesign = lData[lDataLevel].AsGodotArray();

            if (pLevel >= 0 && pLevel < lLevelDesign.Count)
            {
                Dictionary lLevelInfo = lLevelDesign[pLevel].AsGodotDictionary();

                string lName = lLevelInfo[lNameLevel].ToString();
                string lPar = lLevelInfo[lParLevel].ToString();

                LevelWinScreen.GetInstance().ParMinLevel = (int)Convert.ToInt64(lPar);
                levelName.Text = Tr(ID_NAME) + lName;
                levelMaxPar.Text = Tr(ID_MAX_PAR) + " " + lPar;
            }
        }

        CallDeferred(nameof(AnimateHud));
    }

    private void AnimateHud()
    {
        Tween lTween = CreateTween();
        lTween.SetParallel(true);
        float currentDelay = 0f;

        Control[] elements = { levelID, levelName, levelMaxPar, stepsLabel, backBtn, undoBtn, resetBtn, redoBtn };

        foreach (Control element in elements)
        {
            if (element != null)
            {
                element.PivotOffset = element.Size / 2;

                lTween.TweenProperty(element, (string)CanvasItem.PropertyName.Modulate, alphaOne, animDuration)
                      .SetTrans(Tween.TransitionType.Linear)
                      .From(alphaZero)
                      .SetDelay(currentDelay);

                lTween.TweenProperty(element, (string)Control.PropertyName.Scale, Vector2.One, animDuration)
                      .SetTrans(Tween.TransitionType.Elastic)
                      .SetEase(Tween.EaseType.Out)
                      .From(new Vector2(0.3f, 0.3f))
                      .SetDelay(currentDelay);

                currentDelay += animDecalage;
            }
        }
    }

    private void BackButtonPressed()
    {
        if (GameManager.GetInstance().GetGameState() != GameState.PLAYER_MOVE)
            return;

        GridManager.GetInstance()?.DespawnToMenu();
    }

    private void UndoButtonPressed()
    {
        if (GameManager.GetInstance().GetGameState() != GameState.PLAYER_MOVE)
            return;

        Movable.Undo();
    }

    private void ResetButtonPressed()
    {
        if (GameManager.GetInstance().GetGameState() != GameState.PLAYER_MOVE)
            return;

        Movable.Restart();
        Steps = 0;
    }

    private void RedoButtonPressed()
    {
        if (GameManager.GetInstance().GetGameState() != GameState.PLAYER_MOVE)
            return;

        Movable.Redo();
    }

    protected override void Dispose(bool pDisposing)
    {
        instance = null;
        base.Dispose(pDisposing);
    }
}

using Godot;
using System.Collections.Generic;

namespace Com.IsartDigital.Sokoban;

public partial class SelectorScreen : Screen
{
    private static SelectorScreen instance;
    public static SelectorScreen GetInstance() => instance;

    [ExportGroup("UI References")]
    [Export] public int levelNumber;
    [Export] private Label title;
    [Export] private GridContainer container;
    [Export] private TextureButton backButton;
    [Export] private TextureButton unlockButton;
    [Export] private PackedScene packedStar;

    [ExportGroup("Grid Layout Settings")]
    [Export] private int columnsPortrait = 3;
    [Export] private int columnsLandscape = 5;
    [Export] private Vector2 buttonMinimumSize = new Vector2(150, 150);
    [Export] private int buttonMargin = 30;

    [ExportGroup("Typography")]
    [Export] private Font customFont;
    [Export] private int levelButtonFontSize = 25;

    [ExportGroup("Colors Settings")]
    [Export] private Color lockedColor = new Color(0.2f, 0.2f, 0.2f, 1f);
    [Export] private Color toDoColor = new Color(0.5f, 0.5f, 0.5f, 1f);
    [Export] private Color finishedColor = new Color(0.3f, 0.7f, 0.3f, 1f);
    [Export] private Color starUnlockedColor = new Color(1f, 1f, 0f, 1f);
    [Export] private Color starLockedColor = new Color(1f, 1f, 1f, 1f);
    [Export] private Color transparentFactor = new Color(0, 0, 0, -1f);
    [Export] private Color visibleColor = new Color(0, 0, 0, 1f);

    [ExportGroup("Animation Settings")]
    [Export] private float shakeDistance = 35f;
    [Export] private float shakeDuration = 0.05f;
    [Export] private float titleTweenDuration = 2.0f;
    [Export] private float buttonTweenDuration = 0.4f;
    [Export] private float buttonStartTime = 0.4f;
    [Export] private float buttonDecalage = 0.07f;
    [Export] private Vector2 buttonTweenStartOffset = new Vector2(0, -70f);

    [ExportGroup("Game Flow Settings")]
    [Export] private float levelStartDelay = 3.0f;
    [Export] private int maxStars = 3;
    [Export] private float starSpacingMultiplier = 1.2f;

    private const string LEVEL_TEXT = "ID_LEVEL";
    private const string TUTO_TEXT = "ID_TUTO";
    private const string FONT_KEY = "font";
    private const string FONT_SIZE_KEY = "font_size";
    private const string H_SEPARATION_KEY = "h_separation";
    private const string V_SEPARATION_KEY = "v_separation";

    private List<bool> unlockedLevels = new List<bool>();
    private List<int> starNumber = new List<int>();
    private Dictionary<Button, int> levels = new Dictionary<Button, int>();
    private Dictionary<Button, Tween> idleTweens = new Dictionary<Button, Tween>();

    private int levelPlayed;

    private Color alphaZero = new Color(1, 1, 1, 0);
    private Color alphaOne = new Color(1, 1, 1, 1);

    public override void _Ready()
    {
        if (instance == null) instance = this;
        else
        {
            QueueFree();
            return;
        }

        base._Ready();

        backButton.Pressed += BackButtonPressed;
        unlockButton.Pressed += UnlockButtonPressed;

        if (backButton != null) backButton.Modulate = alphaZero;
        if (unlockButton != null) unlockButton.Modulate = alphaZero;

        unlockedLevels.Add(true);
        starNumber.Add(0);
        for (int i = 0; i < levelNumber; i++)
        {
            unlockedLevels.Add(false);
            starNumber.Add(0);
        }

        container.AddThemeConstantOverride(H_SEPARATION_KEY, buttonMargin);
        container.AddThemeConstantOverride(V_SEPARATION_KEY, buttonMargin);

        for (int i = 0; i <= levelNumber; i++)
        {
            Button lButton = i == 0 ? CreateButton(buttonMinimumSize, TUTO_TEXT, i, container) : CreateButton(buttonMinimumSize, LEVEL_TEXT, i, container);

            lButton.AddThemeFontOverride(FONT_KEY, customFont);
            lButton.AddThemeFontSizeOverride(FONT_SIZE_KEY, levelButtonFontSize);

            UpdateButtonState(lButton, i);

            int levelIndex = i;
            lButton.Pressed += () => LaunchLevel(levelIndex, lButton, lButton.Position);

            levels.Add(lButton, i);
        }

        GetTree().Root.SizeChanged += UpdateGridColumns;
        UpdateGridColumns();
    }

    private void UpdateButtonState(Button pButton, int pLevel)
    {
        Color lTargetColor = lockedColor;
        if (unlockedLevels[pLevel])
        {
            lTargetColor = starNumber[pLevel] > 0 ? finishedColor : toDoColor;
        }

        StyleBoxFlat lStyle = new StyleBoxFlat();
        lStyle.BgColor = lTargetColor;
        lStyle.CornerRadiusTopLeft = 8;
        lStyle.CornerRadiusTopRight = 8;
        lStyle.CornerRadiusBottomLeft = 8;
        lStyle.CornerRadiusBottomRight = 8;

        pButton.AddThemeStyleboxOverride("normal", lStyle);
        pButton.AddThemeStyleboxOverride("hover", lStyle);
        pButton.AddThemeStyleboxOverride("pressed", lStyle);
        pButton.AddThemeStyleboxOverride("focus", lStyle);

        ManageIdleAnimation(pButton, pLevel);
    }

    private void ManageIdleAnimation(Button pButton, int pLevel)
    {
        if (idleTweens.ContainsKey(pButton))
        {
            if (idleTweens[pButton] != null && idleTweens[pButton].IsValid()) idleTweens[pButton].Kill();
            idleTweens.Remove(pButton);
            pButton.Scale = Vector2.One;
        }

        if (unlockedLevels[pLevel] && starNumber[pLevel] == 0)
        {
            Tween lTween = pButton.CreateTween().SetLoops();
            float lAnimDuration = 0.8f;

            lTween.TweenProperty(pButton, (string)Control.PropertyName.Scale, new Vector2(1.06f, 1.06f), lAnimDuration)
                  .SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.InOut);
            lTween.TweenProperty(pButton, (string)Control.PropertyName.Scale, Vector2.One, lAnimDuration)
                  .SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.InOut);

            idleTweens.Add(pButton, lTween);
        }
    }

    private void UpdateGridColumns()
    {
        if (container == null) return;

        Vector2I lScreenSize = GetWindow().Size;
        container.Columns = lScreenSize.Y > lScreenSize.X ? columnsPortrait : columnsLandscape;
    }

    private Button CreateButton(Vector2 pSize, string pText, int pNum, Container pContainer)
    {
        Button lButton = new Button();
        lButton.CustomMinimumSize = pSize;
        lButton.PivotOffset = pSize / 2;

        string translatedText = Tr(pText);

        if (pText == LEVEL_TEXT)
            lButton.Text = translatedText + " " + pNum;
        else
            lButton.Text = translatedText;

        pContainer.AddChild(lButton);
        return lButton;
    }

    private void LaunchLevel(int pNum, Button pButtonClicked, Vector2 pPosition)
    {
        if (unlockedLevels[pNum])
        {
            levelPlayed = pNum;

            if (pNum == 0 && starNumber[0] == 0)
            {
                MenuManager.GetInstance().pendingGameLaunch = true;
                MenuManager.GetInstance().pendingLevelId = pNum;
                MenuManager.GetInstance().ChangeMenu(this, (int)MenuList.HELP);
                return;
            }

            GameManager.GetInstance().gameFinished = false;
            GameManager.GetInstance().NbTargetInLevel = 0;
            Hud.GetInstance().Steps = 0;
            MenuManager.GetInstance().ChangeMenu(this, (int)MenuList.IN_GAME);
            GameManager.GetInstance().Visible = true;
            Hud.GetInstance().Launch(pNum);
            GridManager.GetInstance().LoadLevel(pNum);

            Timer lTimer = new Timer();
            lTimer.WaitTime = levelStartDelay;
            lTimer.Autostart = false;
            lTimer.OneShot = true;
            lTimer.Timeout += () => { GameManager.GetInstance().NbTargetActive = 0; };
            AddChild(lTimer);
            lTimer.Start();
        }
        else
        {
            if (pButtonClicked.HasMeta("is_shaking") && (bool)pButtonClicked.GetMeta("is_shaking")) return;
            pButtonClicked.SetMeta("is_shaking", true);

            Vector2 actualPosition = pButtonClicked.Position;
            Vector2 lShakeDistanceVec = new Vector2(shakeDistance, 0);
            Tween lTween = CreateTween();

            lTween.TweenProperty(pButtonClicked, (string)Control.PropertyName.Position, actualPosition + lShakeDistanceVec, shakeDuration).SetTrans(Tween.TransitionType.Linear).From(actualPosition);
            lTween.TweenProperty(pButtonClicked, (string)Control.PropertyName.Position, actualPosition - lShakeDistanceVec / 1.5f, shakeDuration).SetTrans(Tween.TransitionType.Linear);
            lTween.TweenProperty(pButtonClicked, (string)Control.PropertyName.Position, actualPosition + lShakeDistanceVec / 2, shakeDuration).SetTrans(Tween.TransitionType.Linear);
            lTween.TweenProperty(pButtonClicked, (string)Control.PropertyName.Position, actualPosition - lShakeDistanceVec / 3, shakeDuration).SetTrans(Tween.TransitionType.Linear);
            lTween.TweenProperty(pButtonClicked, (string)Control.PropertyName.Position, actualPosition, shakeDuration).SetTrans(Tween.TransitionType.Linear);

            lTween.Finished += () =>
            {
                pButtonClicked.Position = actualPosition;
                pButtonClicked.SetMeta("is_shaking", false);
            };
        }
    }

    public void WinUpdate(int pStarNumber)
    {
        starNumber[levelPlayed] = pStarNumber;
        foreach (var (lButton, lLevel) in levels)
        {
            if (lLevel == levelPlayed) UpdateButtonState(lButton, lLevel);
        }
    }

    public void UnlockLevel(int pLevel)
    {
        if (pLevel < 0 || pLevel >= unlockedLevels.Count)
            return;

        unlockedLevels[pLevel] = true;

        foreach (var (lButton, lLevel) in levels)
        {
            if (lLevel == pLevel)
                UpdateButtonState(lButton, lLevel);
        }
    }

    public bool AreAllLevelsUnlocked()
    {
        foreach (var (lButton, lLevel) in levels)
            if (!unlockedLevels[lLevel]) return false;
        return true;
    }

    private void SyncWithSaveData()
    {
        if (Saving.GetInstance() == null) return;

        for (int i = 0; i <= levelNumber; i++)
        {
            int score = Saving.GetInstance().GetLevelScore(i);
            int stars = 0;

            if (score >= 5000) stars = 3;
            else if (score >= 2000) stars = 2;
            else if (score >= 1000 || score > 0) stars = 1;

            if (stars > 0)
            {
                starNumber[i] = stars;
                unlockedLevels[i] = true;

                if (i + 1 <= levelNumber)
                {
                    unlockedLevels[i + 1] = true;
                }
            }
        }

        foreach (var (lButton, lLevel) in levels)
        {
            UpdateButtonState(lButton, lLevel);
        }
    }

    public override void Load()
    {
        SyncWithSaveData();

        base.Load();

        if (backButton != null) backButton.Modulate = alphaZero;
        if (unlockButton != null) unlockButton.Modulate = alphaZero;

        Tween lLabelTween = CreateTween();

        if (title != null)
        {
            title.PivotOffset = title.Size / 2;
            title.Scale = new Vector2(2.5f, 2.5f);

            lLabelTween.TweenProperty(title, (string)Control.PropertyName.Scale, Vector2.One, titleTweenDuration)
                       .SetTrans(Tween.TransitionType.Elastic)
                       .SetEase(Tween.EaseType.Out);
        }

        Tween lUiTween = CreateTween();
        lUiTween.SetParallel(true);

        if (backButton != null)
        {
            lUiTween.TweenProperty(backButton, (string)CanvasItem.PropertyName.Modulate, alphaOne, 0.5f)
                    .SetDelay(buttonStartTime).From(alphaZero);
        }

        if (unlockButton != null)
        {
            lUiTween.TweenProperty(unlockButton, (string)CanvasItem.PropertyName.Modulate, alphaOne, 0.5f)
                    .SetDelay(buttonStartTime).From(alphaZero);
        }

        foreach (Button lButton in levels.Keys) lButton.SelfModulate += transparentFactor;

        CallDeferred(nameof(AnimateButtons));
    }

    private void AnimateButtons()
    {
        float lButtonActualDecalage = 0f;

        foreach (var (lButton, lLevel) in levels)
        {
            Vector2 targetPosition = lButton.Position;
            Tween lLevelTween = CreateTween();
            lLevelTween.SetParallel(true);

            lLevelTween.TweenProperty(lButton, (string)Control.PropertyName.Position, targetPosition, buttonTweenDuration)
                       .SetTrans(Tween.TransitionType.Elastic).From(targetPosition + buttonTweenStartOffset)
                       .SetDelay(buttonStartTime + lButtonActualDecalage).SetEase(Tween.EaseType.Out);

            lLevelTween.TweenProperty(lButton, (string)CanvasItem.PropertyName.SelfModulate, lButton.SelfModulate + visibleColor, buttonTweenDuration)
                       .SetTrans(Tween.TransitionType.Linear).SetDelay(buttonStartTime + lButtonActualDecalage);

            CreateStars(lButton, targetPosition, lLevel);
            lButtonActualDecalage += buttonDecalage;
        }
    }

    private void CreateStars(Button pButton, Vector2 pPosition, int pLevel)
    {
        if (pButton.GetChildCount() > 0)
        {
            int starIndex = 0;
            foreach (Node child in pButton.GetChildren())
            {
                if (child is TextureRect existingStar)
                {
                    existingStar.SelfModulate = starNumber[pLevel] >= starIndex + 1 ? starUnlockedColor : starLockedColor;
                    starIndex++;
                }
            }
            return;
        }

        for (int i = 0; i < maxStars; i++)
        {
            TextureRect lStar = (TextureRect)packedStar.Instantiate();
            float lSpace = lStar.Size.X * starSpacingMultiplier;
            lStar.Position = new Vector2(-lSpace + (lSpace * i) - lStar.Size.X / 2, lStar.Size.Y / 2);
            lStar.SelfModulate = starNumber[pLevel] >= i + 1 ? starUnlockedColor : starLockedColor;
            lStar.ZIndex = 1;
            pButton.AddChild(lStar);
        }
    }

    private void BackButtonPressed() => MenuManager.GetInstance().ChangeMenu(this, ((int)MenuList.TITLE));

    private void UnlockButtonPressed()
    {
        for (int i = 1; i <= levelNumber; i++) unlockedLevels[i] = true;
        foreach (var (lButton, lLevel) in levels) UpdateButtonState(lButton, lLevel);
    }

    public override void _ExitTree()
    {
        base._ExitTree();
        GetTree().Root.SizeChanged -= UpdateGridColumns;
    }

    protected override void Dispose(bool pDisposing)
    {
        instance = null;
        base.Dispose(pDisposing);
    }

    public override void _Notification(int what)
    {
        if (what == NotificationTranslationChanged)
        {
            UpdateButtonsText();
        }
    }

    private void UpdateButtonsText()
    {
        foreach (var (lButton, lLevel) in levels)
        {
            if (lLevel == 0)
                lButton.Text = Tr(TUTO_TEXT);
            else
                lButton.Text = Tr(LEVEL_TEXT) + " " + lLevel;
        }
    }
}

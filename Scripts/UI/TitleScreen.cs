using Godot;

namespace Com.IsartDigital.Sokoban;

public partial class TitleScreen : Screen
{
    static private Label pl;

    [Export] private TextureRect title;
    [Export] private TextureButton playBtn;
    [Export] private Label playLabel;
    [Export] private TextureButton helpBtn;
    [Export] private Label helpLabel;
    [Export] private TextureButton scoreBtn;
    [Export] private Label scoreLabel;
    [Export] private TextureButton creditBtn;
    [Export] private Label creditLabel;
    [Export] private TextureButton quitBtn;
    [Export] private TextureButton musicBtn;
    [Export] private Label playerConnected;

    private Tween titleTween;

    private Color invisibleColor;
    private Color visibleColor;

    private const string ID_CURRENT_USER = "ID_CURRENT_USER";

    public override void _Ready()
    {
        base._Ready();

        invisibleColor = new Color(1, 1, 1, 0);
        visibleColor = new Color(1, 1, 1, 1);

        playBtn.Modulate = invisibleColor;
        helpBtn.Modulate = invisibleColor;
        scoreBtn.Modulate = invisibleColor;
        creditBtn.Modulate = invisibleColor;
        quitBtn.Modulate = invisibleColor;
        playerConnected.Modulate = invisibleColor;

        if (musicBtn != null) musicBtn.Modulate = invisibleColor;

        if (playLabel != null) playLabel.Modulate = invisibleColor;
        if (helpLabel != null) helpLabel.Modulate = invisibleColor;
        if (scoreLabel != null) scoreLabel.Modulate = invisibleColor;
        if (creditLabel != null) creditLabel.Modulate = invisibleColor;

        playBtn.Pressed += GoToLevel;
        helpBtn.Pressed += GoToHelp;
        scoreBtn.Pressed += GoToScores;
        creditBtn.Pressed += GoToCredits;
        quitBtn.Pressed += OnQuitPressed;

        pl = playerConnected;

        TranslationServer.SetLocale("en");
        pl.Text = Tr(ID_CURRENT_USER);

        if (title != null)
        {
            title.PivotOffset = title.Size / 2;
        }

        GetViewport().SizeChanged += OnScreenResized;
    }

    static public void SetUserName()
    {
        if (pl != null)
            pl.Text += Saving.GetInstance().playerName;
    }

    public override void Load()
    {
        base.Load();

        playBtn.Modulate = invisibleColor;
        helpBtn.Modulate = invisibleColor;
        scoreBtn.Modulate = invisibleColor;
        creditBtn.Modulate = invisibleColor;
        quitBtn.Modulate = invisibleColor;
        playerConnected.Modulate = invisibleColor;

        if (musicBtn != null) musicBtn.Modulate = invisibleColor;

        if (playLabel != null) playLabel.Modulate = invisibleColor;
        if (helpLabel != null) helpLabel.Modulate = invisibleColor;
        if (scoreLabel != null) scoreLabel.Modulate = invisibleColor;
        if (creditLabel != null) creditLabel.Modulate = invisibleColor;

        Vector2 lButtonsSize = playBtn.CustomMinimumSize;
        float lTitleTweenDuration = 2f;
        float lButtonsStartDuration = 0.7f;
        Vector2 lButtonTweenScale = new Vector2(lButtonsSize.X / 3, lButtonsSize.Y);
        float lButtonDuration = 1f;
        float lButtonDelay = 0.2f;

        Tween lTween = CreateTween();
        lTween.SetParallel();

        if (title != null)
        {
            title.Scale = new Vector2(2.5f, 2.5f);
            lTween.TweenProperty(title, (string)Control.PropertyName.Scale, Vector2.One, lTitleTweenDuration)
                  .SetTrans(Tween.TransitionType.Elastic)
                  .SetEase(Tween.EaseType.Out);
        }

        AnimateButton(lTween, playBtn, playLabel, lButtonsStartDuration, lButtonsSize, lButtonTweenScale, lButtonDuration);
        AnimateButton(lTween, helpBtn, helpLabel, lButtonsStartDuration + lButtonDelay, lButtonsSize, lButtonTweenScale, lButtonDuration);
        AnimateButton(lTween, scoreBtn, scoreLabel, lButtonsStartDuration + (lButtonDelay * 2), lButtonsSize, lButtonTweenScale, lButtonDuration);
        AnimateButton(lTween, creditBtn, creditLabel, lButtonsStartDuration + (lButtonDelay * 3), lButtonsSize, lButtonTweenScale, lButtonDuration);

        FadeElement(lTween, quitBtn, lButtonsStartDuration + (lButtonDelay * 4), lButtonDuration);
        FadeElement(lTween, musicBtn, lButtonsStartDuration + (lButtonDelay * 5), lButtonDuration);
        FadeElement(lTween, playerConnected, lButtonsStartDuration + (lButtonDelay * 7), lButtonDuration);

        AnimateTitleIdle();
    }

    private void AnimateButton(Tween tween, TextureButton btn, Label label, float delay, Vector2 targetSize, Vector2 startScale, float duration)
    {
        tween.TweenProperty(btn, (string)CanvasItem.PropertyName.Modulate, visibleColor, duration / 2)
             .From(invisibleColor)
             .SetDelay(delay);

        if (label != null)
        {
            tween.TweenProperty(label, (string)CanvasItem.PropertyName.Modulate, visibleColor, duration / 2)
                 .From(invisibleColor)
                 .SetDelay(delay);
        }
    }

    private void FadeElement(Tween tween, CanvasItem item, float delay, float duration)
    {
        if (item != null)
        {
            tween.TweenProperty(item, (string)CanvasItem.PropertyName.Modulate, visibleColor, duration / 2)
                 .From(invisibleColor)
                 .SetDelay(delay);
        }
    }

    private void AnimateTitleIdle()
    {
        if (title == null) return;

        titleTween?.Kill();

        title.PivotOffset = title.Size / 2;

        float angle = 0.05f;

        titleTween = CreateTween();
        titleTween.SetLoops();

        titleTween.TweenProperty(title, "rotation", angle, 1.5f)
            .SetTrans(Tween.TransitionType.Sine)
            .SetEase(Tween.EaseType.InOut);

        titleTween.TweenProperty(title, "rotation", -angle, 1.5f)
            .SetTrans(Tween.TransitionType.Sine)
            .SetEase(Tween.EaseType.InOut);
    }

    private void GoToLevel()
    {
        MenuManager.GetInstance().ChangeMenu(this, (int)MenuList.SELECTOR);
    }

    private void GoToHelp()
    {
        MenuManager.GetInstance().ChangeMenu(this, (int)MenuList.HELP);
    }

    private void GoToScores()
    {
        MenuManager.GetInstance().ChangeMenu(this, (int)MenuList.HIGHSCORES);
        HighScoresScreen.GetInstance().DisplayingScore();
    }

    private void GoToCredits()
    {
        MenuManager.GetInstance().ChangeMenu(this, (int)MenuList.CREDITS);
    }

    private void OnQuitPressed()
    {
        GetTree().Quit();
    }

    private void SwitchLanguage()
    {
        string current = TranslationServer.GetLocale();
        TranslationServer.SetLocale(current.StartsWith("en") ? "fr" : "en");
        pl.Text = Tr(ID_CURRENT_USER) + Saving.GetInstance().playerName;
    }

    private void OnScreenResized()
    {
        AnimateTitleIdle();
    }

    public override void _ExitTree()
    {
        GetViewport().SizeChanged -= OnScreenResized;
    }
}

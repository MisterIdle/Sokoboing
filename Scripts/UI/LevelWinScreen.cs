using Com.IsartDigital.Shmup.NManager;
using Godot;
using System.Collections.Generic;

namespace Com.IsartDigital.Sokoban;

public partial class LevelWinScreen : Screen
{
    private static LevelWinScreen instance;
    public static LevelWinScreen GetInstance() => instance;

    [ExportGroup("UI References")]
    [Export] private Label scoreLabel;
    [Export] private Control stars;
    [Export] private PackedScene fireworks;

    [ExportGroup("Score Settings")]
    [Export] private int scoreOneStar = 1000;
    [Export] private int scoreTwoStars = 2000;
    [Export] private int scoreThreeStars = 5000;
    [Export] private int maxScore = 10000;

    [ExportGroup("Animation Settings")]
    [Export] private float starAnimDelay = 0.4f;
    [Export] private float starAnimDuration = 0.5f;
    [Export] private float starAnimStartYOffset = -250f;

    public List<int> levelsScores = new();
    public int ParMinLevel { get; set; }
    public int LevelNumberId { get; set; }
    public int scoreLevel;

    private const string SCORE_TEXT = "ID_SCORE";

    public override void _Ready()
    {
        if (instance != null)
        {
            QueueFree();
            return;
        }

        instance = this;

        int totalLevels = SelectorScreen.GetInstance().levelNumber;
        levelsScores = new List<int>(new int[totalLevels + 1]);
    }

    public void LaunchWin()
    {
        int earnedStars = CalculateStars();
        SelectorScreen.GetInstance().UnlockLevel(LevelNumberId + 1);

        foreach (Control star in stars.GetChildren())
        {
            star.Visible = false;
        }

        StarsDisplay(earnedStars);
        ScoreDisplay(earnedStars);
    }

    private void StarsDisplay(int pEarnedStars)
    {
        if (pEarnedStars <= 0) return;

        Tween tween = CreateTween();
        tween.SetParallel(true);

        int starCount = Mathf.Min(pEarnedStars, stars.GetChildCount());

        for (int i = 0; i < starCount; i++)
        {
            TextureRect star = stars.GetChild<TextureRect>(i);
            float delay = starAnimDelay * i;

            tween.TweenProperty(star, (string)Node2D.PropertyName.Position, star.Position, starAnimDuration)
                 .From(new Vector2(star.Position.X, starAnimStartYOffset))
                 .SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.In).SetDelay(delay);

            tween.TweenProperty(star, (string)Node2D.PropertyName.Scale, star.Scale, starAnimDuration)
                 .From(star.Scale * 2f)
                 .SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.In).SetDelay(delay);

            tween.TweenProperty(star, (string)CanvasItem.PropertyName.Visible, true, 0f)
                 .From(false)
                 .SetDelay(delay);

            SpawnFirework(star.GlobalPosition, delay);
        }
    }

    private void SpawnFirework(Vector2 position, float delay)
    {
        if (delay <= 0f)
        {
            GpuParticles2D fireworkInstance = fireworks.Instantiate<GpuParticles2D>();
            fireworkInstance.Position = position;
            GetParent().AddChild(fireworkInstance);
            return;
        }

        Timer timer = new Timer();
        timer.WaitTime = delay;
        timer.OneShot = true;
        AddChild(timer);
        timer.Start();

        timer.Timeout += () =>
        {
            GpuParticles2D fireworkInstance = fireworks.Instantiate<GpuParticles2D>();
            fireworkInstance.Position = position;
            GetParent().AddChild(fireworkInstance);
            fireworkInstance.Emitting = true;

            timer.QueueFree();
        };
    }

    public int CalculateStars()
    {
        int currentSteps = Hud.GetInstance().Steps;

        if (currentSteps == 0) return 0;
        if (currentSteps <= ParMinLevel) return 3;
        if (currentSteps <= ParMinLevel * 1.5f) return 2;

        return 1;
    }

    public void ScoreDisplay(int pEarnedStars)
    {
        int currentSteps = Hud.GetInstance().Steps;

        switch (pEarnedStars)
        {
            case 3:
                scoreLevel = scoreThreeStars + (ParMinLevel - currentSteps) * 100;
                break;
            case 2:
                int limitTwoStars = Mathf.FloorToInt(ParMinLevel * 1.5f);
                scoreLevel = scoreTwoStars + (limitTwoStars - currentSteps) * 50;
                break;
            case 1:
                scoreLevel = scoreOneStar;
                break;
            default:
                scoreLevel = 0;
                break;
        }

        scoreLevel = Mathf.Clamp(scoreLevel, 0, maxScore);

        Saving.GetInstance().SaveLevelScore(LevelNumberId, scoreLevel);
        scoreLabel.Text = $"{Tr(SCORE_TEXT)} : {scoreLevel}";
        SelectorScreen.GetInstance().WinUpdate(pEarnedStars);

        levelsScores[LevelNumberId] = scoreLevel;
    }

    private void OnButtonHighscorePressed()
    {
        MenuManager.GetInstance().ChangeMenu(this, (int)MenuList.HIGHSCORES);
        HighScoresScreen.GetInstance().DisplayingScore();
    }

    private void OnButtonMenuPressed()
    {
        MenuManager.GetInstance().ChangeMenu(this, (int)MenuList.SELECTOR);
        AudioManager.GetInstance().PlayMusic(AudioManager.MusicType.UiMusic);
    }
    protected override void Dispose(bool pDisposing)
    {
        if (instance == this) instance = null;
        base.Dispose(pDisposing);
    }
}

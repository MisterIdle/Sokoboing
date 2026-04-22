using Com.IsartDigital.Shmup.NManager;
using Com.IsartDigital.Sokoban;
using Godot;
using System;

namespace Com.IsartDigital.Sokoban;

public partial class GameManager : Node2D
{
    private static GameManager instance;
    public static GameManager GetInstance() => instance;

    [ExportGroup("Win Animation Settings")]
    [Export(PropertyHint.Range, "0.1,5.0,0.1")]
    private float delayBeforeFireworks = 1.0f;

    [Export(PropertyHint.Range, "1,10,1")]
    private int fireworksBurstCount = 3;

    public GameState gameState = GameState.MENU;

    public static bool IsPlaceholderMode { get; private set; } = false;
    public static event Action<bool> OnVisualModeChanged;

    [Export] public Camera2D camera;

    public bool gameFinished;

    public int NbTargetInLevel { get; set; } = 0;

    private int nbTargetActive = 0;

    public int NbTargetActive
    {
        get => nbTargetActive;
        set
        {
            nbTargetActive = value;
            CheckWinCondition();
        }
    }

    public override void _Ready()
    {
        if (instance == null)
            instance = this;
        else
        {
            QueueFree();
            return;
        }

        AudioManager.GetInstance().PlayMusic(AudioManager.MusicType.UiMusic);
        ChangeGameState(GameState.PLAYER_MOVE);
    }

    public override void _Input(InputEvent pEvent)
    {
        if (pEvent.IsActionPressed(Input.PLACEHOLDER))
        {
            IsPlaceholderMode = !IsPlaceholderMode;
            OnVisualModeChanged?.Invoke(IsPlaceholderMode);

            GD.Print("Placeholder mode: " + IsPlaceholderMode);
        }
    }

    public void CheckWinCondition()
    {
        if (gameFinished) return;

        if (NbTargetActive == NbTargetInLevel && NbTargetInLevel > 0)
        {
            gameFinished = true;

            ChangeGameState(GameState.MENU);

            Timer lDelayTimer = new Timer();
            lDelayTimer.WaitTime = delayBeforeFireworks;
            lDelayTimer.OneShot = true;
            lDelayTimer.Timeout += StartFireworksAndWin;
            AddChild(lDelayTimer);
            lDelayTimer.Start();
        }
    }

    private void StartFireworksAndWin()
    {
        ChangeGameState(GameState.MENU);
        AudioManager.GetInstance().PlayMusic(AudioManager.MusicType.JingleWin);
        FireworkGenerator.Instance.LaunchBurst(fireworksBurstCount);
    }

    public void LaunchWin()
    {
        int currentLevelId = LevelWinScreen.GetInstance().LevelNumberId;

        if (currentLevelId >= SelectorScreen.GetInstance().levelNumber)
        {
            int lEarnedStars = LevelWinScreen.GetInstance().CalculateStars();
            LevelWinScreen.GetInstance().ScoreDisplay(lEarnedStars);

            MenuManager.GetInstance().ChangeMenu(Hud.GetInstance(), (int)MenuList.FINAL_WIN);
            FinalWinScreen.GetInstance().LevelNumberId = currentLevelId;
            FinalWinScreen.GetInstance().LaunchWin();
        }
        else
        {
            MenuManager.GetInstance().ChangeMenu(Hud.GetInstance(), (int)MenuList.WIN);
            LevelWinScreen.GetInstance().LevelNumberId = currentLevelId;
            LevelWinScreen.GetInstance().LaunchWin();
        }

        GridManager.GetInstance()?.ClearGrid();

        NbTargetInLevel = 0;
        NbTargetActive = 0;
        gameFinished = false;
    }

    public void ChangeGameState(GameState pGameState)
    {
        gameState = pGameState;
    }

    public GameState GetGameState()
    {
        return gameState;
    }

    protected override void Dispose(bool pDisposing)
    {
        instance = null;
        base.Dispose(pDisposing);
    }
}

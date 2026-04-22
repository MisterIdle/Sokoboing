using Godot;
using System;
using Com.IsartDigital.Sokoban;

public partial class FireworkGenerator : Node2D
{
    [Export] public PackedScene fireworkScene;
    [Export] private float time = 0.2f;

    public static FireworkGenerator Instance;

    private Vector2 screenSize;
    private RandomNumberGenerator rng = new RandomNumberGenerator();

    private Timer burstTimer;
    private int fireworksToSpawn = 0;

    public override void _Ready()
    {
        if (Instance == null)
            Instance = this;
        else
        {
            QueueFree();
            return;
        }

        screenSize = GetViewportRect().Size;
        rng.Randomize();

        burstTimer = new Timer();
        burstTimer.WaitTime = time;
        burstTimer.OneShot = false;
        burstTimer.Timeout += OnTimerTimeout;
        AddChild(burstTimer);
    }

    public void LaunchBurst(int count)
    {
        if (count <= 0)
        {
            GameManager.GetInstance().LaunchWin();
            return;
        }

        fireworksToSpawn = count;

        SpawnFirework();
        fireworksToSpawn--;

        if (fireworksToSpawn > 0)
        {
            burstTimer.Start();
        }
        else
        {
            GameManager.GetInstance().LaunchWin();
        }
    }

    private void OnTimerTimeout()
    {
        SpawnFirework();
        fireworksToSpawn--;

        if (fireworksToSpawn <= 0)
        {
            burstTimer.Stop();
            GameManager.GetInstance().LaunchWin();
        }
    }

    private void SpawnFirework()
    {
        Firework fireworkInstance = fireworkScene.Instantiate<Firework>();

        float randomY = rng.RandfRange(screenSize.Y * 0.2f, screenSize.Y * 0.6f);
        fireworkInstance.Position = new Vector2(rng.RandfRange(0, screenSize.X), randomY);

        AddChild(fireworkInstance);

        fireworkInstance.Emitting = true;
    }

    protected override void Dispose(bool disposing)
    {
        Instance = null;
        base.Dispose(disposing);
    }
}
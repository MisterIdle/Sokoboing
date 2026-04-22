using Godot;

namespace Com.IsartDigital.Sokoban;

public partial class MenuManager : Control
{
    static private MenuManager instance;
    static public MenuManager GetInstance() => instance;

    public bool pendingGameLaunch = false;
    public int pendingLevelId = -1;

    public override void _Ready()
    {
        if (instance == null) instance = this;
        else
        {
            QueueFree();
            return;
        }

        ChangeMenu(null, 0);
    }

    public void ChangeMenu(Control lastMenu, int pIndex)
    {
        if (lastMenu != null)
            lastMenu.Visible = false;

        GetChild<Control>(pIndex).Visible = true;
        if (GetChild<Control>(pIndex) is Screen lScreen) lScreen.Load();
    }

    public void StartPendingGame(Control currentMenu)
    {
        if (!pendingGameLaunch || pendingLevelId == -1) return;

        GameManager.GetInstance().gameFinished = false;
        GameManager.GetInstance().NbTargetInLevel = 0;
        Hud.GetInstance().Steps = 0;

        ChangeMenu(currentMenu, (int)MenuList.IN_GAME);

        GameManager.GetInstance().Visible = true;
        Hud.GetInstance().Launch(pendingLevelId);
        GridManager.GetInstance().LoadLevel(pendingLevelId);

        Timer lTimer = new Timer();
        lTimer.WaitTime = 3.0f;
        lTimer.Autostart = false;
        lTimer.OneShot = true;
        lTimer.Timeout += () => { GameManager.GetInstance().NbTargetActive = 0; };
        AddChild(lTimer);
        lTimer.Start();

        pendingGameLaunch = false;
        pendingLevelId = -1;
    }

    protected override void Dispose(bool pDisposing)
    {
        instance = null;
        base.Dispose(pDisposing);
    }
}

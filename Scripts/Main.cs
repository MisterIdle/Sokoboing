using Godot;

namespace Com.IsartDigital.Sokoban;

public partial class Main : Node2D
{
    private static Main instance;

    public static Main GetInstance() => instance;

    public override void _Ready()
    {
        if (instance == null)
            instance = this;
        else
        {
            QueueFree();
            return;
        }
    }

	public override void _Process(double pDelta)
	{
		float lDelta = (float)pDelta;
	}

	protected override void Dispose(bool pDisposing)
	{
		instance = null;
	}
}

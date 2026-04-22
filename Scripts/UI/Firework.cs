using Godot;

public partial class Firework : GpuParticles2D
{
    public override void _Ready()
    {
        Finished += QueueFree; 
    }
}

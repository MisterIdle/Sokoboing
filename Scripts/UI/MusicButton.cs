using Godot;
using System;

public partial class MusicButton : Node2D
{
    [Export] private TextureButton musicBtn;
    [Export] private Texture2D musicOnTexture;
    [Export] private Texture2D musicOffTexture;

    private string busName = "Master";

    public override void _Ready()
    {
        musicBtn.Pressed += ToggleMaster;

        UpdateButtonIcon();
    }

    private void ToggleMaster()
    {
        int index = AudioServer.GetBusIndex(busName);
        bool isMuted = AudioServer.IsBusMute(index);

        AudioServer.SetBusMute(index, !isMuted);

        UpdateButtonIcon();
    }

    public void UpdateButtonIcon()
    {
        if (musicBtn == null || musicOnTexture == null || musicOffTexture == null)
            return;

        int index = AudioServer.GetBusIndex(busName);

        bool isMuted = AudioServer.IsBusMute(index);

        musicBtn.TextureNormal = isMuted ? musicOffTexture : musicOnTexture;
    }
}
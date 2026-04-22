using Com.IsartDigital.Shmup.NManager;
using Godot;
using System;

// Author : Léana Borgne

namespace Com.IsartDigital.Sokoban;

public partial class Screen : Control
{
    public virtual void Load()
    {
        AudioManager.GetInstance().Play(AudioManager.AudioType.WooshCalmShort);
    }
}  

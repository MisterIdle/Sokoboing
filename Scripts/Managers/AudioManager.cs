using Godot;
using System;

namespace Com.IsartDigital.Shmup.NManager;

public partial class AudioManager : Node
{
    public enum AudioType
    {
        FootStep,
        Hit,
        WooshCalmShort,
        WooshCalm,
        WooshFast,
    }

    public enum MusicType
    {
        None,
        UiMusic,
        JingleWin,
    }

    [Export] private AudioStream ambiance;

    [ExportGroup("Player")]
    [Export] private AudioStream footStep;
    [Export] private AudioStream hit;

    [ExportGroup("UI")]
    [Export] private AudioStream wooshCalmShort;
    [Export] private AudioStream wooshCalm;
    [Export] private AudioStream wooshFast;
    [Export] private AudioStream bubble;

    [ExportGroup("Music")]
    [Export] private AudioStream musicUi;
    [Export] private AudioStream jingleWin;

    private AudioStreamPlayer musicPlayer;
    private Tween musicTween;

    private const string BUS_MASTER = "Master";
    private const float DEFAULT_PITCH = 1.0f;
    private const float VOLUME_MUTED_DB = -80.0f;
    private const float VOLUME_NORMAL_DB = 0.0f;

    private const float DEFAULT_FADE_TIME = 0.0f;
    private const float STOP_MUSIC_FADE_TIME = 1f;
    private const int FADE_TIME_DIVIDER = 2;

    private bool isMusicLooping = true;

    private static AudioManager instance;
    public static AudioManager GetInstance() => instance;

    public override void _Ready()
    {
        if (instance == null)
        {
            instance = this;
            ProcessMode = ProcessModeEnum.Always;

            musicPlayer = new AudioStreamPlayer();
            musicPlayer.Bus = BUS_MASTER;
            AddChild(musicPlayer);
            musicPlayer.Finished += OnMusicFinished;
        }
        else
        {
            QueueFree();
        }
    }

    public void Play(AudioType pType, float pPitch = DEFAULT_PITCH)
    {
        AudioStream lStream = ResolveStream(pType);
        if (lStream != null)
            PlayStream(lStream, pPitch);
    }

    private AudioStream ResolveStream(AudioType pType)
    {
        return pType switch
        {
            AudioType.FootStep => footStep,
            AudioType.Hit => hit,
            AudioType.WooshCalmShort => wooshCalmShort,
            AudioType.WooshCalm => wooshCalm,
            AudioType.WooshFast => wooshFast,
            _ => null,
        };
    }

    private void PlayStream(AudioStream pStream, float pPitch)
    {
        AudioStreamPlayer lAudio = new AudioStreamPlayer();
        lAudio.Stream = pStream;
        lAudio.Bus = BUS_MASTER;
        lAudio.PitchScale = Mathf.Max(pPitch, 0.01f);

        lAudio.Finished += lAudio.QueueFree;
        AddChild(lAudio);
        lAudio.Play();
    }

    public void PlayRandomPitch(AudioType pType, float min = 0.95f, float max = 1.05f)
    {
        float pitch = (float)GD.RandRange(min, max);
        Play(pType, pitch);
    }

    public void PlayMusic(MusicType pType, bool pLoop = true, float pFadeTime = DEFAULT_FADE_TIME, float pPitch = DEFAULT_PITCH)
    {
        AudioStream lNewStream = ResolveMusic(pType);
        isMusicLooping = pLoop;

        if (musicPlayer.Stream == lNewStream && musicPlayer.Playing) return;

        if (musicTween != null && musicTween.IsValid())
            musicTween.Kill();

        musicTween = CreateTween();

        musicTween.TweenProperty(musicPlayer, "volume_db", VOLUME_MUTED_DB, pFadeTime / FADE_TIME_DIVIDER)
          .SetTrans(Tween.TransitionType.Sine)
          .SetEase(Tween.EaseType.In);

        musicTween.TweenCallback(Callable.From(() =>
        {
            musicPlayer.Stop();
            musicPlayer.Stream = lNewStream;
            musicPlayer.PitchScale = pPitch;

            if (lNewStream != null)
            {
                musicPlayer.VolumeDb = VOLUME_MUTED_DB;
                musicPlayer.Play();
            }
        }));

        if (lNewStream != null)
        {
            musicTween.TweenProperty(musicPlayer, "volume_db", VOLUME_NORMAL_DB, pFadeTime / FADE_TIME_DIVIDER)
              .SetTrans(Tween.TransitionType.Sine)
              .SetEase(Tween.EaseType.Out);
        }
    }

    public void StopMusic(float pFadeTime = STOP_MUSIC_FADE_TIME)
    {
        PlayMusic(MusicType.None, false, pFadeTime);
    }

    private void OnMusicFinished()
    {
        if (musicPlayer.Stream == jingleWin)
        {
            PlayMusic(MusicType.UiMusic);
            return;
        }

        if (isMusicLooping && musicPlayer.Stream != null)
        {
            musicPlayer.Play();
        }
    }

    private AudioStream ResolveMusic(MusicType pType)
    {
        return pType switch
        {
            MusicType.UiMusic => musicUi,
            MusicType.JingleWin => jingleWin,
            _ => null,
        };
    }

    public void SetMusicPaused(bool pIsPaused) => musicPlayer.StreamPaused = pIsPaused;

    public bool IsMusicPlaying() => musicPlayer != null && musicPlayer.Playing;
}

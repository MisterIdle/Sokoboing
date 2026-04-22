using Godot;
using System.Collections.Generic;

namespace Com.IsartDigital.Sokoban;

public static class Effects
{
    private static string PROP_SCALE = Node2D.PropertyName.Scale;
    private static string PROP_POS = Node2D.PropertyName.Position;
    private static string PROP_OFFSET = Camera2D.PropertyName.Offset;

    private static Dictionary<Node2D, Tween> activeTweens = new Dictionary<Node2D, Tween>();

    private static Tween CreateSafeTween(Node2D pTarget)
    {
        if (activeTweens.ContainsKey(pTarget) && activeTweens[pTarget] != null && activeTweens[pTarget].IsValid())
        {
            activeTweens[pTarget].Kill();
        }
        Tween lTween = pTarget.CreateTween();
        activeTweens[pTarget] = lTween;
        return lTween;
    }

    private static Vector2 GetBaseScale(Node2D pTarget)
    {
        if (!pTarget.HasMeta("base_scale"))
            pTarget.SetMeta("base_scale", new Vector2(Mathf.Abs(pTarget.Scale.X), Mathf.Abs(pTarget.Scale.Y)));

        return (Vector2)pTarget.GetMeta("base_scale");
    }

    public static void PlayBlockedEffect(Node2D pTarget, float pDuration = 0.1f, float pStretchX = 1.3f, float pStretchY = 0.7f)
    {
        if (pTarget == null) return;

        float signX = pTarget.Scale.X < 0 ? -1f : 1f;
        Vector2 baseScale = GetBaseScale(pTarget);

        Tween lTween = CreateSafeTween(pTarget);

        lTween.TweenProperty(pTarget, PROP_SCALE, new Vector2(signX * baseScale.X * pStretchX, baseScale.Y * pStretchY), pDuration)
              .SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.Out);

        lTween.Chain().TweenProperty(pTarget, PROP_SCALE, new Vector2(signX * baseScale.X, baseScale.Y), pDuration)
              .SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.In);
    }

    public static void PlayPushEffect(Node2D pTarget, Vector2I pDirection, float pDuration = 0.1f, float pSquash = 0.85f, float pStretch = 1.15f)
    {
        if (pTarget == null) return;

        float signX = pTarget.Scale.X < 0 ? -1f : 1f;
        Vector2 baseScale = GetBaseScale(pTarget);

        Vector2 lStretch = pDirection.X != 0
            ? new Vector2(baseScale.X * pSquash, baseScale.Y * pStretch)
            : new Vector2(baseScale.X * pStretch, baseScale.Y * pSquash);

        Tween lTween = CreateSafeTween(pTarget);

        lTween.TweenProperty(pTarget, PROP_SCALE, new Vector2(signX * lStretch.X, lStretch.Y), pDuration)
              .SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.Out);

        lTween.Chain().TweenProperty(pTarget, PROP_SCALE, new Vector2(signX * baseScale.X, baseScale.Y), pDuration)
              .SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.In);
    }

    public static void PlayScalePulse(Node2D pTarget, Vector2 pScale, float pDuration)
    {
        if (pTarget == null) return;

        float signX = pTarget.Scale.X < 0 ? -1f : 1f;
        Vector2 baseScale = GetBaseScale(pTarget);
        float lHalfDuration = pDuration / 2f;

        Tween lTween = CreateSafeTween(pTarget);

        lTween.TweenProperty(pTarget, PROP_SCALE, new Vector2(signX * baseScale.X * pScale.X, baseScale.Y * pScale.Y), lHalfDuration)
              .SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.Out);

        lTween.Chain().TweenProperty(pTarget, PROP_SCALE, new Vector2(signX * baseScale.X, baseScale.Y), lHalfDuration)
              .SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.In);
    }

    public static void PlayBouncePulse(Node2D pTarget, Vector2 pScale, float pDuration)
    {
        if (pTarget == null) return;

        float signX = pTarget.Scale.X < 0 ? -1f : 1f;
        Vector2 baseScale = GetBaseScale(pTarget);
        Tween lTween = CreateSafeTween(pTarget);

        lTween.TweenProperty(pTarget, PROP_SCALE, new Vector2(signX * baseScale.X * pScale.X, baseScale.Y * pScale.Y), pDuration)
              .SetTrans(Tween.TransitionType.Quad).SetEase(Tween.EaseType.Out);

        lTween.Chain().TweenProperty(pTarget, PROP_SCALE, new Vector2(signX * baseScale.X, baseScale.Y), pDuration)
              .SetTrans(Tween.TransitionType.Quad).SetEase(Tween.EaseType.In);
    }

    public static void PlayIdleLoop(Node2D pTarget, float pStrength, float pDuration = 0.8f, float pSquashMultiplier = 0.5f)
    {
        if (pTarget == null) return;

        float signX = pTarget.Scale.X < 0 ? -1f : 1f;
        Vector2 baseScale = GetBaseScale(pTarget);
        Tween lTween = CreateSafeTween(pTarget).SetLoops();

        Vector2 squashScale = new Vector2(signX * baseScale.X * (1f + (pStrength * pSquashMultiplier)), baseScale.Y * (1f - pStrength));
        Vector2 normalScale = new Vector2(signX * baseScale.X, baseScale.Y);

        lTween.TweenProperty(pTarget, PROP_SCALE, squashScale, pDuration)
              .SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.InOut);

        lTween.TweenProperty(pTarget, PROP_SCALE, normalScale, pDuration)
              .SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.InOut);
    }

    public static void PlayWalkHop(Node2D pTarget, float pDuration, float pHopHeight = 8f, float pAirStretchX = 0.9f, float pAirStretchY = 1.1f, float pLandSquashX = 1.15f, float pLandSquashY = 0.85f)
    {
        if (pTarget == null) return;

        float signX = pTarget.Scale.X < 0 ? -1f : 1f;
        Vector2 baseScale = GetBaseScale(pTarget);

        float halfDuration = pDuration / 2f;
        float quarterDuration = pDuration / 4f;

        Tween lTween = CreateSafeTween(pTarget);

        lTween.TweenProperty(pTarget, PROP_POS, new Vector2(0, -pHopHeight), halfDuration)
              .SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.Out);
        lTween.Parallel().TweenProperty(pTarget, PROP_SCALE, new Vector2(signX * baseScale.X * pAirStretchX, baseScale.Y * pAirStretchY), halfDuration)
              .SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.Out);

        lTween.Chain().TweenProperty(pTarget, PROP_POS, Vector2.Zero, halfDuration)
              .SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.In);
        lTween.Parallel().TweenProperty(pTarget, PROP_SCALE, new Vector2(signX * baseScale.X * pLandSquashX, baseScale.Y * pLandSquashY), halfDuration)
              .SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.In);

        lTween.Chain().TweenProperty(pTarget, PROP_SCALE, new Vector2(signX * baseScale.X, baseScale.Y), quarterDuration)
              .SetTrans(Tween.TransitionType.Quad).SetEase(Tween.EaseType.Out);
    }

    public static void PlayCameraShake(Camera2D pCamera, float pIntensity = 3.0f, bool pIsHorizontal = false, float pStepDuration = 0.1f, float pDampening1 = 0.7f, float pDampening2 = 0.3f)
    {
        if (pCamera == null) return;

        Tween lTween = CreateSafeTween(pCamera);
        Vector2 shakeDirection = pIsHorizontal ? new Vector2(pIntensity, 0) : new Vector2(0, pIntensity);

        lTween.TweenProperty(pCamera, PROP_OFFSET, shakeDirection, pStepDuration)
              .SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.Out);

        lTween.TweenProperty(pCamera, PROP_OFFSET, -shakeDirection * pDampening1, pStepDuration)
              .SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.InOut);

        lTween.TweenProperty(pCamera, PROP_OFFSET, shakeDirection * pDampening2, pStepDuration)
              .SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.InOut);

        lTween.TweenProperty(pCamera, PROP_OFFSET, Vector2.Zero, pStepDuration)
              .SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.InOut);
    }
}
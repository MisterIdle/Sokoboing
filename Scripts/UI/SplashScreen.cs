using Com.IsartDigital.Sokoban;
using Godot;
using System.Reflection.Emit;

// Author : 

namespace Com.IsartDigital.Sokoban
{
	
	public partial class SplashScreen : Control
	{
        private float chrono;
        private float splashScreenTime = 1.5f;

		public override void _Process(double pDelta)
		{
			float lDelta = (float)pDelta;

			chrono += lDelta;
			if(chrono >= splashScreenTime )
			{
				Animation();
                SetProcess(false);
            }
		}

		private void Animation()
		{
			PivotOffset = Size / 2;

            Tween lTween = CreateTween();
			lTween.Finished += OnTweenFinished;
            lTween.SetTrans(Tween.TransitionType.Linear).SetEase(Tween.EaseType.Out);
			lTween.TweenProperty(this, "scale", Scale * 2, 1f);

			lTween.SetTrans(Tween.TransitionType.Quint).SetEase(Tween.EaseType.Out);
            lTween.TweenProperty(this, "scale", Vector2.Zero, 0.5f);

        }

		private void OnTweenFinished()
		{
            MenuManager.GetInstance().ChangeMenu(this, (int)MenuList.LOGIN);
        }
	}
}

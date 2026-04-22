using Godot;
using Godot.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Com.IsartDigital.Sokoban
{
    public partial class HighScoresScreen : Screen
    {
        static private HighScoresScreen instance;

        private List<int> bestScore = new List<int>();
        private List<string> bestScoreName = new List<string>();
        private List<Vector2> posLabel = new List<Vector2>();
        private List<Tween> tweenPlaying = new List<Tween>();

        private Array<Node> allLabel;

        string firstScoreName;
        float waitTime = 0;
        string textToDisplay = "";
        string defaultText = "";

        int posPlayer;
        Vector2 startScale;
        float idleRotate = Mathf.Pi / 32;
        float idleScale = 1.1f;

        [Export] Color outlineColor = new Color(0, 0, 0, 1);
        [Export] Color highlightColor = new Color(0.882f, 0.753f, 0, 1);
        [Export] ResponsiveElement backButton;
        [Export] Label notOnBoard;
        [Export] Control scoreBoard;
        [Export] Node2D startPos;

        private Color alphaZero = new Color(1, 1, 1, 0);
        private Color alphaOne = new Color(1, 1, 1, 1);

        private void BackButtonPressed()
        {
            ResetTween();
            MenuManager.GetInstance().ChangeMenu(this, ((int)MenuList.TITLE));
            ResetHighlight();
        }

        private void ResetTween()
        {
            for (int i = tweenPlaying.Count - 1; i >= 0; i--)
            {
                tweenPlaying[i].Kill();
                tweenPlaying.RemoveAt(i);
            }

            if (posPlayer >= 0 && posPlayer < allLabel.Count)
            {
                Label lLabel = (Label)allLabel[posPlayer];
                if (startScale != Vector2.Zero)
                {
                    lLabel.Rotation = 0;
                    lLabel.Scale = startScale;
                }
            }
        }

        public override void _Ready()
        {
            instance = this;
            allLabel = scoreBoard.GetChildren();
            GetPosLabel();
            backButton.Connect(TextureButton.SignalName.Pressed, Callable.From(BackButtonPressed));

            if (backButton != null) backButton.Modulate = alphaZero;
            if (notOnBoard != null) notOnBoard.Modulate = alphaZero;
        }

        static public HighScoresScreen GetInstance()
        {
            return instance;
        }

        public void DisplayingScore()
        {
            SearchingBestScoreInData();

            List<int> lBestScore = new List<int>();
            List<string> lBestScoreName = new List<string>();

            (lBestScore, lBestScoreName) = SortingList();

            Array<Node> lScore = scoreBoard.GetChildren();

            posPlayer = -1;

            for (int i = 0; i < 10; i++)
            {
                if (lBestScore.Count - 1 >= i)
                {
                    textToDisplay = i + 1 + ". " + lBestScoreName[i] + ": " + lBestScore[i];
                    if (lBestScoreName[i].ToUpper() == Saving.GetInstance().playerName.ToUpper())
                    {
                        HighlightPlayerName((Label)lScore[i]);
                        posPlayer = i;
                    }
                }
                else
                {
                    textToDisplay = i + 1 + ". " + defaultText;
                }
                ((Label)lScore[i]).Text = textToDisplay;
            }

            string lPlayerName = Saving.GetInstance().playerName.ToUpper();

            if (!lBestScoreName.Contains(lPlayerName))
            {
                Dictionary lDico = Saving.GetInstance().Parsing();
                notOnBoard.Text = lPlayerName + ": " + Saving.GetInstance().CalculateScore(lPlayerName);
                notOnBoard.Visible = true;
                HighlightPlayerName(notOnBoard);
            }
            else
            {
                notOnBoard.Visible = false;
            }
        }

        private void ResetHighlight()
        {
            Array<Node> lScore = scoreBoard.GetChildren();
            foreach (Label lLabel in lScore)
            {
                lLabel.AddThemeColorOverride("font_color", new Color(1, 1, 1, 1));
                lLabel.AddThemeConstantOverride("outline_size", 0);
                lLabel.AddThemeColorOverride("font_outline_color", outlineColor);
            }
            if (notOnBoard != null)
            {
                notOnBoard.AddThemeColorOverride("font_color", new Color(1, 1, 1, 1));
                notOnBoard.AddThemeConstantOverride("outline_size", 0);
            }
        }

        private void HighlightPlayerName(Label lScore)
        {
            lScore.AddThemeColorOverride("font_color", highlightColor);
            lScore.AddThemeConstantOverride("outline_size", 20);
            lScore.AddThemeColorOverride("font_outline_color", outlineColor);
        }

        private void GetPosLabel()
        {
            foreach (Label lLabel in allLabel)
            {
                posLabel.Add(lLabel.Position);
            }
        }

        private void SetUpAnimation()
        {
            tweenPlaying.Clear();
            foreach (Label lLabel in allLabel)
            {
                lLabel.Position = startPos.Position;
                lLabel.Modulate = alphaZero;
            }

            if (backButton != null) backButton.Modulate = alphaZero;
            if (notOnBoard != null) notOnBoard.Modulate = alphaZero;
        }

        public override void Load()
        {
            base.Load();
            SetUpAnimation();

            if (backButton != null)
            {
                Tween btnTween = CreateTween();
                btnTween.TweenProperty(backButton, (string)CanvasItem.PropertyName.Modulate, alphaOne, 0.5f).SetDelay(0.5f);
                tweenPlaying.Add(btnTween);
            }

            if (notOnBoard != null && notOnBoard.Visible)
            {
                Tween notOnBoardTween = CreateTween();
                notOnBoardTween.TweenProperty(notOnBoard, (string)CanvasItem.PropertyName.Modulate, alphaOne, 0.5f).SetDelay(1.5f);
                tweenPlaying.Add(notOnBoardTween);
            }

            for (int i = allLabel.Count - 1; i >= 0; i--)
            {
                waitTime += 0.15f;
                Tween lTween = CreateTween();
                lTween.SetParallel(true);
                lTween.SetTrans(Tween.TransitionType.Back).SetEase(Tween.EaseType.Out);

                lTween.TweenProperty(allLabel[i], "position", posLabel[i], 0.8f).SetDelay(waitTime);
                lTween.TweenProperty(allLabel[i], "modulate", alphaOne, 0.6f).SetDelay(waitTime);

                tweenPlaying.Add(lTween);

                if (i == 0)
                {
                    lTween.Chain().TweenCallback(Callable.From(OnLastTweenFinished));
                }
            }
            waitTime = 0f;
        }

        private void OnLastTweenFinished()
        {
            if (posPlayer < 0 || posPlayer >= allLabel.Count) return;

            ResetTween();
            Node lLabel = allLabel[posPlayer];
            ((Label)lLabel).PivotOffset = new Vector2(((Label)lLabel).Size.X / 2, ((Label)lLabel).Size.Y / 2);

            startScale = ((Label)lLabel).Scale;

            Tween lTween = CreateTween();
            tweenPlaying.Add(lTween);
            lTween.SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.Out);

            lTween.TweenProperty(lLabel, "scale", startScale * idleScale, 2);
            lTween.Parallel().TweenProperty(lLabel, "rotation", ((Label)lLabel).Rotation - idleRotate, 2);

            lTween.TweenProperty(lLabel, "scale", startScale, 2).SetDelay(0.1f);
            lTween.Parallel().TweenProperty(lLabel, "rotation", 0, 2).SetDelay(0.1f);

            lTween.TweenProperty(lLabel, "scale", startScale * idleScale, 2);
            lTween.Parallel().TweenProperty(lLabel, "rotation", ((Label)lLabel).Rotation + idleRotate, 2);

            lTween.TweenProperty(lLabel, "scale", startScale, 2).SetDelay(0.1f);
            lTween.Parallel().TweenProperty(lLabel, "rotation", 0, 2).SetDelay(0.1f);

            lTween.Finished += RestartingAnimation;
        }

        private void RestartingAnimation()
        {
            OnLastTweenFinished();
        }

        private void SearchingBestScoreInData()
        {
            Dictionary lDico = Saving.GetInstance().Parsing();

            foreach (string pName in lDico.Keys)
            {
                int lScore = Saving.GetInstance().CalculateScore(pName);
                if (bestScore.Count > 9)
                {
                    for (int i = bestScore.Count - 1; i >= 0; i--)
                    {
                        if (bestScore[i] < lScore)
                        {
                            ReplacingScore(lScore, pName);
                            break;
                        }
                    }
                }
                else
                {
                    bestScore.Add(lScore);
                    bestScoreName.Add(pName);
                }
            }
        }

        private void ReplacingScore(int pScore, string pName)
        {
            int lMin = int.MaxValue;
            int lIndex = 0;
            for (int i = 0; i < bestScore.Count; i++)
            {
                if (bestScore[i] < lMin)
                {
                    lMin = bestScore[i];
                    lIndex = i;
                }
            }
            bestScore.Remove(lMin);
            bestScore.Add(pScore);
            bestScoreName.RemoveAt(lIndex);
            bestScoreName.Add(pName);
        }

        private (List<int>, List<string>) SortingList()
        {
            List<int> lBestScore = new List<int>();
            List<string> lBestScoreName = new List<string>();

            while (bestScore.Count != 0)
            {
                int lMax = 0;
                int index = 0;
                for (int i = 0; i < bestScore.Count; i++)
                {
                    if (bestScore[i] > lMax)
                    {
                        lMax = bestScore[i];
                        index = i;
                    }
                }
                lBestScore.Add(lMax);
                bestScore.RemoveAt(index);
                lBestScoreName.Add(bestScoreName[index].ToUpper());
                bestScoreName.RemoveAt(index);
            }
            return (lBestScore, lBestScoreName);
        }
    }
}
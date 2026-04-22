using Godot;
using System.Collections.Generic;

namespace Com.IsartDigital.Sokoban
{
    public partial class FinalWinScreen : Screen
    {
        [Export] Label scoreLabel;
        [Export] Control stars;

        private static FinalWinScreen instance;
        public static FinalWinScreen GetInstance() => instance;

        public int ParMinLevel { get; set; }
        public int LevelNumberId { get; set; }

        public int scoreLevel;

        private const int SCORE_ONE_STARS = 1000;
        private const int SCORE_TWO_STARS = 2000;
        private const int SCORE_THREE_STARS = 5000;

        private const string SCORE = "ID_SCORE";

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

        public void LaunchWin()
        {
            SelectorScreen.GetInstance().UnlockLevel(LevelNumberId + 1);

            foreach (TextureRect lStar in stars.GetChildren())
                lStar.Visible = false;

            ScoreDisplay();
        }

        private void ScoreDisplay()
        {
            scoreLevel = 0;
            foreach (int score in LevelWinScreen.GetInstance().levelsScores)
            {
                scoreLevel += score;
            }

            Saving.GetInstance().SaveLevelScore(LevelNumberId, scoreLevel);
            scoreLabel.Text = Tr(SCORE) + " : " + scoreLevel;
        }

        #region BUTTON
        private void OnButtonHighscorePressed()
        {
            MenuManager.GetInstance().ChangeMenu(this, ((int)MenuList.HIGHSCORES));
            HighScoresScreen.GetInstance().DisplayingScore();
        }

        private void OnButtonRetryPressed() { }

        private void OnButtonMenuPressed()
        {
            MenuManager.GetInstance().ChangeMenu(this, ((int)MenuList.SELECTOR));
        }

        private void OnButtonNextLevelPressed() { }
        #endregion

        protected override void Dispose(bool pDisposing)
        {
            instance = null;
            base.Dispose(pDisposing);
        }
    }
}

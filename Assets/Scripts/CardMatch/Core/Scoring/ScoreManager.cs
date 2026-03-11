using CardMatch.Config;

namespace CardMatch.Core.Scoring
{
    public class ScoreManager
    {
        private readonly GameConfig gameConfig;

        public int CurrentScore { get; private set; }
        public int CurrentCombo { get; private set; }

        public ScoreManager(GameConfig config)
        {
            gameConfig = config;
            CurrentScore = 0;
            CurrentCombo = 0;
        }

        public void Reset()
        {
            CurrentScore = 0;
            CurrentCombo = 0;
        }

        public void Restore(int score, int combo)
        {
            CurrentScore = score;
            CurrentCombo = combo;
        }

        public void RegisterMatch()
        {
            int points = gameConfig.matchPoints;

            if (gameConfig.enableCombo)
            {
                points += CurrentCombo * gameConfig.comboBonusPerStep;
                CurrentCombo++;
            }

            CurrentScore += points;
        }

        public void RegisterMismatch()
        {
            CurrentScore -= gameConfig.mismatchPenalty;

            if (CurrentScore < 0)
            {
                CurrentScore = 0;
            }

            CurrentCombo = 0;
        }
    }
}
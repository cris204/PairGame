using UnityEngine;

namespace CardMatch.Config
{
    [CreateAssetMenu(fileName = "GameConfig", menuName = "CardMatch/Game Config")]
    public class GameConfig : ScriptableObject
    {
        [Header("Board")]
        [Min(2)] public int defaultRows = 4;
        [Min(2)] public int defaultColumns = 4;
        [Min(0f)] public float boardSpacing = 16f;
        [Min(0f)] public float boardPadding = 16f;

        [Header("Card Animation")]
        [Min(0.01f)] public float flipDuration = 0.2f;
        [Min(0f)] public float initialRevealDuration = 1.5f;
        [Min(0f)] public float mismatchRevealDuration = 0.8f;

        [Header("Scoring")]
        public int matchPoints = 100;
        public int mismatchPenalty = 10;
        public bool enableCombo = true;
        public int comboBonusPerStep = 25;

        [Header("Autosave")]
        public bool autoSaveOnStateChange = true;

        [Header("Audio")]
        public float sfxVolume = 1f;
    }
}

using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace CardMatch.Presentation.HUD
{
    public class HUDView : MonoBehaviour
    {
        private const string ScoreFormat = "Score: {0}";
        private const string ComboFormat = "Combo: x{0}";

        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private TextMeshProUGUI scoreText;
        [SerializeField] private TextMeshProUGUI comboText;
        [SerializeField] private TextMeshProUGUI gameOverText;
        [SerializeField] private Button newGameButton;
        [SerializeField] private float comboScalePerStep = 0.05f;
        [SerializeField] private float comboMaxScaleBonus = 0.35f;
        [SerializeField] private float comboPulseScale = 1.18f;
        [SerializeField] private float comboPulseDuration = 0.18f;
        [SerializeField] private float comboShakePerStep = 0.6f;
        [SerializeField] private float comboMaxShake = 10f;
        [SerializeField] private float comboShakeSpeed = 18f;
        [SerializeField] private Color comboBaseColor = Color.white;
        [SerializeField] private Color comboFlashColor = new Color(1f, 0.92f, 0.35f, 1f);
        [SerializeField] private float comboFlashDuration = 0.24f;

        private RectTransform comboRectTransform;
        private Vector2 comboDefaultAnchoredPosition;
        private Vector3 comboDefaultScale;
        private Coroutine comboPulseCoroutine;
        private Coroutine comboFlashCoroutine;
        private int currentComboValue;
        private int lastAnimatedComboValue;
        private float comboPulseMultiplier = 1f;

        private void Awake()
        {
            ResolveComboVisualReferences();
            Hide();
            SetGameOverVisible(false);
        }

        private void Update()
        {
            UpdateComboMotion();
        }
        
        
        public void Show()
        {
            canvasGroup.interactable = true;
            canvasGroup.alpha = 1;
            canvasGroup.blocksRaycasts = true;
        }
        
        public void Hide()
        {
            canvasGroup.interactable = false;
            canvasGroup.alpha = 0;
            canvasGroup.blocksRaycasts = false;
        }

        public void BindNewGameAction(UnityAction action)
        {
            if (newGameButton == null)
            {
                return;
            }

            newGameButton.onClick.RemoveAllListeners();

            if (action != null)
            {
                newGameButton.onClick.AddListener(action);
            }
        }

        public void UpdateHud(int score, int combo, bool showCombo, bool showGameOver)
        {
            UpdateScore(score);
            UpdateCombo(combo, showCombo);
            SetGameOverVisible(showGameOver);
        }

        private void UpdateScore(int score)
        {
            if (scoreText == null)
            {
                return;
            }

            scoreText.text = string.Format(ScoreFormat, score);
        }

        private void UpdateCombo(int combo, bool showCombo)
        {
            if (comboText == null)
            {
                return;
            }

            bool shouldShowCombo = showCombo && combo > 0;
            comboText.gameObject.SetActive(shouldShowCombo);

            if (!shouldShowCombo)
            {
                currentComboValue = 0;
                lastAnimatedComboValue = 0;
                comboPulseMultiplier = 1f;
                ResetComboVisuals();
                return;
            }

            currentComboValue = combo;
            comboText.text = string.Format(ComboFormat, combo);

            if (combo > lastAnimatedComboValue)
            {
                StartComboPulse();
                StartComboFlash();
            }

            lastAnimatedComboValue = combo;
        }

        private void SetGameOverVisible(bool isVisible)
        {
            if (gameOverText == null)
            {
                return;
            }

            gameOverText.gameObject.SetActive(isVisible);
        }

        private void ResolveComboVisualReferences()
        {
            if (comboText == null)
            {
                return;
            }

            comboRectTransform = comboText.rectTransform;
            comboDefaultAnchoredPosition = comboRectTransform.anchoredPosition;
            comboDefaultScale = comboRectTransform.localScale;
            comboText.color = comboBaseColor;
        }

        private void UpdateComboMotion()
        {
            if (comboText == null || comboRectTransform == null)
            {
                return;
            }

            if (!comboText.gameObject.activeSelf || currentComboValue <= 0)
            {
                return;
            }

            float scaleBonus = Mathf.Min(currentComboValue * comboScalePerStep, comboMaxScaleBonus);
            float targetScale = 1f + scaleBonus;
            float shakeStrength = Mathf.Min(Mathf.Max(0, currentComboValue - 1) * comboShakePerStep, comboMaxShake);
            float time = Time.unscaledTime * comboShakeSpeed;
            float offsetX = Mathf.Sin(time) * shakeStrength;
            float offsetY = Mathf.Cos(time * 1.31f) * shakeStrength * 0.65f;

            comboRectTransform.localScale = comboDefaultScale * targetScale * comboPulseMultiplier;
            comboRectTransform.anchoredPosition = comboDefaultAnchoredPosition + new Vector2(offsetX, offsetY);
        }

        private void StartComboPulse()
        {
            if (comboPulseCoroutine != null)
            {
                StopCoroutine(comboPulseCoroutine);
            }

            comboPulseCoroutine = StartCoroutine(PlayComboPulse());
        }

        private IEnumerator PlayComboPulse()
        {
            float duration = comboPulseDuration;

            if (duration <= 0f)
            {
                comboPulseMultiplier = 1f;
                comboPulseCoroutine = null;
                yield break;
            }

            float halfDuration = duration * 0.5f;
            float elapsed = 0f;

            while (elapsed < halfDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / halfDuration);
                comboPulseMultiplier = Mathf.Lerp(1f, comboPulseScale, t);
                yield return null;
            }

            elapsed = 0f;

            while (elapsed < halfDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / halfDuration);
                comboPulseMultiplier = Mathf.Lerp(comboPulseScale, 1f, t);
                yield return null;
            }

            comboPulseMultiplier = 1f;
            comboPulseCoroutine = null;
        }

        private void StartComboFlash()
        {
            if (comboFlashCoroutine != null)
            {
                StopCoroutine(comboFlashCoroutine);
            }

            comboFlashCoroutine = StartCoroutine(PlayComboFlash());
        }

        private IEnumerator PlayComboFlash()
        {
            float duration = comboFlashDuration;

            if (duration <= 0f)
            {
                comboText.color = comboBaseColor;
                comboFlashCoroutine = null;
                yield break;
            }

            float halfDuration = duration * 0.5f;
            float elapsed = 0f;

            while (elapsed < halfDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / halfDuration);
                comboText.color = Color.Lerp(comboBaseColor, comboFlashColor, t);
                yield return null;
            }

            elapsed = 0f;

            while (elapsed < halfDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / halfDuration);
                comboText.color = Color.Lerp(comboFlashColor, comboBaseColor, t);
                yield return null;
            }

            comboText.color = comboBaseColor;
            comboFlashCoroutine = null;
        }

        private void ResetComboVisuals()
        {
            if (comboPulseCoroutine != null)
            {
                StopCoroutine(comboPulseCoroutine);
                comboPulseCoroutine = null;
            }

            if (comboFlashCoroutine != null)
            {
                StopCoroutine(comboFlashCoroutine);
                comboFlashCoroutine = null;
            }

            if (comboRectTransform == null)
            {
                return;
            }

            comboRectTransform.localScale = comboDefaultScale;
            comboRectTransform.anchoredPosition = comboDefaultAnchoredPosition;
            comboText.color = comboBaseColor;
        }

    }
}

using System;
using System.Collections;
using CardMatch.Config;
using CardMatch.Core.Data;
using UnityEngine;
using UnityEngine.UI;

namespace CardMatch.Presentation.Cards
{
    public class CardPresenter : MonoBehaviour
    {
        public event Action<CardPresenter> OnCardSelected;

        [SerializeField] private Button selectButton;
        [SerializeField] private RectTransform flipRoot;
        [SerializeField] private GameObject frontFaceRoot;
        [SerializeField] private GameObject backFaceRoot;
        [SerializeField] private Image frontImage;
        [SerializeField] private Image backImage;
        [SerializeField] private CanvasGroup cardCanvasGroup;
        [SerializeField] private Image feedbackOverlay;
        [SerializeField] private RectTransform burstEffectRoot;
        [SerializeField] private float matchPulseScale = 1.1f;
        [SerializeField] private float matchPulseDuration = 0.18f;
        [SerializeField] private float matchDisappearDuration = 0.2f;
        [SerializeField] private float mismatchShakeDuration = 0.18f;
        [SerializeField] private float mismatchShakeStrength = 14f;
        [SerializeField] private float mismatchFlashAlpha = 0.35f;
        [SerializeField] private Color mismatchFlashColor = new Color(1f, 0.3f, 0.3f, 1f);
        [SerializeField] private Color matchBurstColor = new Color(1f, 0.9f, 0.35f, 1f);
        [SerializeField] private Color disappearBurstColor = new Color(1f, 0.6f, 0.2f, 1f);

        private GameConfig gameConfig;
        private Coroutine matchFeedbackCoroutine;
        private Coroutine mismatchFeedbackCoroutine;
        private Coroutine burstEffectCoroutine;
        private bool isAnimating;
        private bool isInteractable = true;
        private RectTransform[] burstSpokeTransforms;
        private Image[] burstSpokeImages;

        public CardRuntimeData RuntimeData { get; private set; }

        public bool IsAnimating
        {
            get { return isAnimating; }
        }

        public bool IsInteractable
        {
            get { return isInteractable; }
        }

        public void Initialize(CardRuntimeData runtimeData, GameConfig config, Sprite frontSprite, Sprite backSprite)
        {
            RuntimeData = runtimeData;
            gameConfig = config;

            CacheSerializedReferences();
            RestoreVisibleState();

            if (frontImage != null)
            {
                frontImage.sprite = frontSprite;
            }

            if (backImage != null)
            {
                backImage.sprite = backSprite;
            }

            if (selectButton != null)
            {
                selectButton.onClick.RemoveListener(HandleButtonClicked);
                selectButton.onClick.AddListener(HandleButtonClicked);
            }

            if (RuntimeData.IsMatched)
            {
                ApplyMatchedStateImmediate();
                SetInteractable(false);
                return;
            }

            ApplyVisualStateImmediate(RuntimeData.IsRevealed);
            SetInteractable(true);
        }

        public void PlayMismatchFeedback()
        {
            if (!isActiveAndEnabled)
            {
                return;
            }

            if (mismatchFeedbackCoroutine != null)
            {
                StopCoroutine(mismatchFeedbackCoroutine);
            }

            mismatchFeedbackCoroutine = StartCoroutine(PlayMismatchSequence());
        }

        public void SetInteractable(bool value)
        {
            isInteractable = value;

            if (selectButton != null)
            {
                selectButton.interactable = value;
            }
        }

        public bool CanBeSelected()
        {
            if (RuntimeData == null)
            {
                return false;
            }

            if (!isInteractable)
            {
                return false;
            }

            if (isAnimating)
            {
                return false;
            }

            if (RuntimeData.IsMatched)
            {
                return false;
            }

            if (RuntimeData.IsRevealed)
            {
                return false;
            }

            return true;
        }

        public void RevealImmediate()
        {
            if (RuntimeData == null)
            {
                return;
            }

            RestoreVisibleState();
            RuntimeData.IsRevealed = true;
            ApplyVisualStateImmediate(true);
        }

        public void HideImmediate()
        {
            if (RuntimeData == null)
            {
                return;
            }

            RestoreVisibleState();
            RuntimeData.IsRevealed = false;
            ApplyVisualStateImmediate(false);
        }

        public void MarkAsMatchedImmediate()
        {
            if (RuntimeData == null)
            {
                return;
            }

            RuntimeData.IsMatched = true;
            RuntimeData.IsRevealed = true;
            ApplyMatchedStateImmediate();
            SetInteractable(false);
        }

        public IEnumerator PlayReveal()
        {
            if (RuntimeData == null)
            {
                yield break;
            }

            RestoreVisibleState();
            RuntimeData.IsRevealed = true;
            yield return PlayFlipAnimation(true);
        }

        public IEnumerator PlayHide()
        {
            if (RuntimeData == null)
            {
                yield break;
            }

            RestoreVisibleState();
            RuntimeData.IsRevealed = false;
            yield return PlayFlipAnimation(false);
        }

        public void MarkAsMatched()
        {
            if (RuntimeData == null)
            {
                return;
            }

            RuntimeData.IsMatched = true;
            RuntimeData.IsRevealed = true;
            SetInteractable(false);

            if (matchFeedbackCoroutine != null)
            {
                StopCoroutine(matchFeedbackCoroutine);
            }

            if (isActiveAndEnabled)
            {
                matchFeedbackCoroutine = StartCoroutine(PlayMatchSequence());
            }
            else
            {
                ApplyMatchedStateImmediate();
            }
        }

        public void HandleButtonClicked()
        {
            if (!CanBeSelected())
            {
                return;
            }

            OnCardSelected?.Invoke(this);
        }

        private void CacheSerializedReferences()
        {
            CacheBurstSpokes();
        }

        private void CacheBurstSpokes()
        {
            if (burstEffectRoot == null)
            {
                burstSpokeTransforms = Array.Empty<RectTransform>();
                burstSpokeImages = Array.Empty<Image>();
                return;
            }

            int childCount = burstEffectRoot.childCount;
            burstSpokeTransforms = new RectTransform[childCount];
            burstSpokeImages = new Image[childCount];

            for (int i = 0; i < childCount; i++)
            {
                RectTransform spokeTransform = burstEffectRoot.GetChild(i) as RectTransform;
                Image spokeImage = spokeTransform != null ? spokeTransform.GetComponent<Image>() : null;

                burstSpokeTransforms[i] = spokeTransform;
                burstSpokeImages[i] = spokeImage;
            }
        }

        private IEnumerator PlayFlipAnimation(bool revealFront)
        {
            if (flipRoot == null)
            {
                ApplyVisualStateImmediate(revealFront);
                yield break;
            }

            isAnimating = true;

            float duration = gameConfig != null ? gameConfig.flipDuration : 0.2f;
            float halfDuration = duration * 0.5f;
            float elapsed = 0f;
            Vector3 originalScale = flipRoot.localScale;

            while (elapsed < halfDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / halfDuration);
                float x = Mathf.Lerp(1f, 0f, t);
                flipRoot.localScale = new Vector3(x, originalScale.y, originalScale.z);
                yield return null;
            }

            ApplyVisualStateImmediate(revealFront);
            elapsed = 0f;

            while (elapsed < halfDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / halfDuration);
                float x = Mathf.Lerp(0f, 1f, t);
                flipRoot.localScale = new Vector3(x, originalScale.y, originalScale.z);
                yield return null;
            }

            flipRoot.localScale = originalScale;
            isAnimating = false;
        }

        private void ApplyVisualStateImmediate(bool showFront)
        {
            if (frontFaceRoot != null)
            {
                frontFaceRoot.SetActive(showFront);
            }

            if (backFaceRoot != null)
            {
                backFaceRoot.SetActive(!showFront);
            }

            if (flipRoot != null)
            {
                Vector3 scale = flipRoot.localScale;
                flipRoot.localScale = new Vector3(1f, scale.y, scale.z);
            }
        }

        private IEnumerator PlayMatchSequence()
        {
            RestoreVisibleState();
            CreateBurstEffect(matchBurstColor, 7, 44f, 0.24f, 34f, 6f);

            if (flipRoot != null)
            {
                Vector3 originalScale = flipRoot.localScale;
                Vector3 expandedScale = new Vector3(
                    originalScale.x * matchPulseScale,
                    originalScale.y * matchPulseScale,
                    originalScale.z);

                yield return AnimateScale(originalScale, expandedScale, matchPulseDuration);
                yield return AnimateScale(expandedScale, originalScale, matchPulseDuration);
                flipRoot.localScale = originalScale;
            }

            yield return PlayDisappear();
            matchFeedbackCoroutine = null;
        }

        private IEnumerator PlayDisappear()
        {
            CreateBurstEffect(disappearBurstColor, 9, 56f, 0.22f, 42f, 7f);

            if (cardCanvasGroup == null)
            {
                yield break;
            }

            float duration = matchDisappearDuration;

            if (duration <= 0f)
            {
                cardCanvasGroup.alpha = 0f;
                cardCanvasGroup.blocksRaycasts = false;
                cardCanvasGroup.interactable = false;
                yield break;
            }

            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                cardCanvasGroup.alpha = Mathf.Lerp(1f, 0f, t);
                yield return null;
            }

            cardCanvasGroup.alpha = 0f;
            cardCanvasGroup.blocksRaycasts = false;
            cardCanvasGroup.interactable = false;
        }

        private IEnumerator PlayMismatchSequence()
        {
            if (feedbackOverlay != null)
            {
                feedbackOverlay.color = new Color(
                    mismatchFlashColor.r,
                    mismatchFlashColor.g,
                    mismatchFlashColor.b,
                    mismatchFlashAlpha);
            }

            CreateBurstEffect(mismatchFlashColor, 5, 20f, 0.16f, 18f, 5f);

            RectTransform shakeTarget = flipRoot != null ? flipRoot : transform as RectTransform;
            Vector2 basePosition = Vector2.zero;

            if (shakeTarget != null)
            {
                basePosition = shakeTarget.anchoredPosition;
            }

            float duration = mismatchShakeDuration;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float damping = 1f - t;

                if (shakeTarget != null)
                {
                    float offsetX = Mathf.Sin(elapsed * 70f) * mismatchShakeStrength * damping;
                    float offsetY = Mathf.Cos(elapsed * 45f) * mismatchShakeStrength * 0.2f * damping;
                    shakeTarget.anchoredPosition = basePosition + new Vector2(offsetX, offsetY);
                }

                if (feedbackOverlay != null)
                {
                    float overlayAlpha = Mathf.Lerp(mismatchFlashAlpha, 0f, t);
                    feedbackOverlay.color = new Color(
                        mismatchFlashColor.r,
                        mismatchFlashColor.g,
                        mismatchFlashColor.b,
                        overlayAlpha);
                }

                yield return null;
            }

            if (shakeTarget != null)
            {
                shakeTarget.anchoredPosition = basePosition;
            }

            if (feedbackOverlay != null)
            {
                feedbackOverlay.color = new Color(
                    mismatchFlashColor.r,
                    mismatchFlashColor.g,
                    mismatchFlashColor.b,
                    0f);
            }

            mismatchFeedbackCoroutine = null;
        }

        private void ApplyMatchedStateImmediate()
        {
            ApplyVisualStateImmediate(true);

            if (flipRoot != null)
            {
                flipRoot.localScale = new Vector3(1f, flipRoot.localScale.y, flipRoot.localScale.z);
            }

            if (cardCanvasGroup != null)
            {
                cardCanvasGroup.alpha = 0f;
                cardCanvasGroup.blocksRaycasts = false;
                cardCanvasGroup.interactable = false;
            }
        }

        private IEnumerator AnimateScale(Vector3 from, Vector3 to, float duration)
        {
            if (flipRoot == null)
            {
                yield break;
            }

            if (duration <= 0f)
            {
                flipRoot.localScale = to;
                yield break;
            }

            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                flipRoot.localScale = Vector3.Lerp(from, to, t);
                yield return null;
            }

            flipRoot.localScale = to;
        }

        private void RestoreVisibleState()
        {
            if (cardCanvasGroup != null)
            {
                cardCanvasGroup.alpha = 1f;
                cardCanvasGroup.blocksRaycasts = true;
                cardCanvasGroup.interactable = true;
            }

            if (feedbackOverlay != null)
            {
                feedbackOverlay.color = new Color(
                    mismatchFlashColor.r,
                    mismatchFlashColor.g,
                    mismatchFlashColor.b,
                    0f);
            }

            ResetBurstEffectVisuals();

            if (flipRoot != null)
            {
                flipRoot.anchoredPosition = Vector2.zero;
            }
        }

        private void CreateBurstEffect(Color color, int spokeCount, float distance, float duration, float length, float thickness)
        {
            if (!isActiveAndEnabled)
            {
                return;
            }

            if (burstSpokeTransforms == null || burstSpokeImages == null || burstSpokeTransforms.Length == 0)
            {
                return;
            }

            if (burstEffectCoroutine != null)
            {
                StopCoroutine(burstEffectCoroutine);
            }

            ResetBurstEffectVisuals();
            burstEffectCoroutine = StartCoroutine(AnimateBurstEffect(color, spokeCount, distance, duration, length, thickness));
        }

        private IEnumerator AnimateBurstEffect(
            Color color,
            int spokeCount,
            float distance,
            float duration,
            float length,
            float thickness)
        {
            int activeSpokeCount = Mathf.Min(spokeCount, burstSpokeTransforms.Length);

            if (activeSpokeCount <= 0)
            {
                burstEffectCoroutine = null;
                yield break;
            }

            if (duration <= 0f)
            {
                ResetBurstEffectVisuals();
                burstEffectCoroutine = null;
                yield break;
            }

            Vector2[] directions = new Vector2[activeSpokeCount];

            for (int i = 0; i < activeSpokeCount; i++)
            {
                RectTransform spokeTransform = burstSpokeTransforms[i];
                Image spokeImage = burstSpokeImages[i];

                if (spokeTransform == null || spokeImage == null)
                {
                    continue;
                }

                float angle = 360f * i / activeSpokeCount;
                float radians = angle * Mathf.Deg2Rad;
                directions[i] = new Vector2(Mathf.Cos(radians), Mathf.Sin(radians));
                spokeTransform.sizeDelta = new Vector2(length, thickness);
                spokeTransform.anchoredPosition = Vector2.zero;
                spokeTransform.localScale = Vector3.one;
                spokeTransform.localRotation = Quaternion.Euler(0f, 0f, angle);
                spokeImage.color = color;
            }

            for (int i = activeSpokeCount; i < burstSpokeTransforms.Length; i++)
            {
                RectTransform spokeTransform = burstSpokeTransforms[i];
                Image spokeImage = burstSpokeImages[i];

                if (spokeTransform == null || spokeImage == null)
                {
                    continue;
                }

                spokeTransform.anchoredPosition = Vector2.zero;
                spokeTransform.localScale = Vector3.one;
                spokeImage.color = new Color(color.r, color.g, color.b, 0f);
            }

            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float moveT = Mathf.SmoothStep(0f, 1f, t);
                float alpha = Mathf.Lerp(1f, 0f, t);
                float scale = Mathf.Lerp(1f, 0.7f, t);

                for (int i = 0; i < activeSpokeCount; i++)
                {
                    RectTransform spokeTransform = burstSpokeTransforms[i];
                    Image spokeImage = burstSpokeImages[i];

                    if (spokeTransform == null || spokeImage == null)
                    {
                        continue;
                    }

                    spokeTransform.anchoredPosition = directions[i] * distance * moveT;
                    spokeTransform.localScale = new Vector3(scale, 1f, 1f);
                    spokeImage.color = new Color(color.r, color.g, color.b, alpha);
                }

                yield return null;
            }

            ResetBurstEffectVisuals();
            burstEffectCoroutine = null;
        }

        private void ResetBurstEffectVisuals()
        {
            if (burstSpokeTransforms == null || burstSpokeImages == null)
            {
                return;
            }

            for (int i = 0; i < burstSpokeTransforms.Length; i++)
            {
                RectTransform spokeTransform = burstSpokeTransforms[i];
                Image spokeImage = burstSpokeImages[i];

                if (spokeTransform == null || spokeImage == null)
                {
                    continue;
                }

                spokeTransform.anchoredPosition = Vector2.zero;
                spokeTransform.localScale = Vector3.one;
                spokeTransform.localRotation = Quaternion.identity;
                spokeImage.color = new Color(1f, 1f, 1f, 0f);
            }
        }
    }
}

using System.Collections;
using UnityEngine;

namespace CardMatch.Presentation.UI
{
    [RequireComponent(typeof(RectTransform))]
    [RequireComponent(typeof(CanvasGroup))]
    public class UIAnimatedPanel : MonoBehaviour
    {
        [SerializeField] private RectTransform animatedTarget;
        [SerializeField] private CanvasGroup animatedCanvasGroup;
        [SerializeField] private float showDuration = 0.3f;
        [SerializeField] private float hideDuration = 0.2f;
        [SerializeField] private float startScale = 0.94f;
        [SerializeField] private float startYOffset = -18f;

        private Coroutine animationCoroutine;
        private Vector2 defaultAnchoredPosition;

        private void Awake()
        {
            EnsureReferences();
            defaultAnchoredPosition = animatedTarget.anchoredPosition;
        }

        public void ShowAnimated()
        {
            gameObject.SetActive(true);
            StartAnimation(true);
        }

        public void HideAnimated()
        {
            if (!gameObject.activeInHierarchy)
            {
                gameObject.SetActive(false);
                return;
            }

            StartAnimation(false);
        }

        private void StartAnimation(bool isShowing)
        {
            EnsureReferences();

            if (!isShowing && !isActiveAndEnabled)
            {
                gameObject.SetActive(false);
                return;
            }

            if (animationCoroutine != null)
            {
                StopCoroutine(animationCoroutine);
            }

            animationCoroutine = StartCoroutine(Animate(isShowing));
        }

        private IEnumerator Animate(bool isShowing)
        {
            yield return new WaitForEndOfFrame();
            float duration = isShowing ? showDuration : hideDuration;
            float startTime = Time.realtimeSinceStartup;

            Vector2 fromPosition = isShowing
                ? defaultAnchoredPosition + new Vector2(0f, startYOffset)
                : animatedTarget.anchoredPosition;
            Vector2 toPosition = isShowing
                ? defaultAnchoredPosition
                : defaultAnchoredPosition + new Vector2(0f, startYOffset * 0.4f);

            Vector3 fromScale = isShowing
                ? new Vector3(startScale, startScale, 1f)
                : animatedTarget.localScale;
            Vector3 toScale = isShowing
                ? Vector3.one
                : new Vector3(startScale, startScale, 1f);

            float fromAlpha = isShowing ? 0f : animatedCanvasGroup.alpha;
            float toAlpha = isShowing ? 1f : 0f;

            animatedCanvasGroup.interactable = false;
            animatedCanvasGroup.blocksRaycasts = false;
            animatedTarget.anchoredPosition = fromPosition;
            animatedTarget.localScale = fromScale;
            animatedCanvasGroup.alpha = fromAlpha;

            if (duration <= 0f)
            {
                ApplyAnimationEndState(isShowing, toPosition, toScale, toAlpha);
                yield break;
            }

            while ((Time.realtimeSinceStartup - startTime) < duration)
            {
                float elapsed = Time.realtimeSinceStartup - startTime;
                float t = Mathf.Clamp01(elapsed / duration);

                animatedTarget.anchoredPosition = Vector2.Lerp(fromPosition, toPosition, t);
                animatedTarget.localScale = Vector3.Lerp(fromScale, toScale, t);
                animatedCanvasGroup.alpha = Mathf.Lerp(fromAlpha, toAlpha, t);
                yield return null;
            }

            ApplyAnimationEndState(isShowing, toPosition, toScale, toAlpha);
        }

        private void ApplyAnimationEndState(bool isShowing, Vector2 targetPosition, Vector3 targetScale, float targetAlpha)
        {
            animatedTarget.anchoredPosition = targetPosition;
            animatedTarget.localScale = targetScale;
            animatedCanvasGroup.alpha = targetAlpha;
            animatedCanvasGroup.interactable = isShowing;
            animatedCanvasGroup.blocksRaycasts = isShowing;
            animationCoroutine = null;

            if (!isShowing)
            {
                gameObject.SetActive(false);
            }
        }
        private void EnsureReferences()
        {
            if (animatedTarget == null)
            {
                animatedTarget = GetComponent<RectTransform>();
            }

            if (animatedCanvasGroup == null)
            {
                animatedCanvasGroup = GetComponent<CanvasGroup>();
            }
        }
    }
}

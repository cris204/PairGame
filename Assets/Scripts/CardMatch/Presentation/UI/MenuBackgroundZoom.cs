using System.Collections;
using UnityEngine;

namespace CardMatch.Presentation.UI
{
    public class MenuBackgroundZoom : MonoBehaviour
    {
        [SerializeField] private RectTransform zoomTarget;
        [SerializeField] private float startScale = 1f;
        [SerializeField] private float endScale = 1.2f;
        [SerializeField] private float zoomDuration = 0.45f;
        [SerializeField] private bool useUnscaledTime = true;

        private Coroutine zoomCoroutine;

        public float ZoomDuration
        {
            get { return zoomDuration; }
        }

        public bool UseUnscaledTime
        {
            get { return useUnscaledTime; }
        }

        private void Awake()
        {
            ResolveTarget();
        }

        public void PlayZoom()
        {
            ResolveTarget();

            if (zoomTarget == null)
            {
                return;
            }

            if (zoomCoroutine != null)
            {
                StopCoroutine(zoomCoroutine);
            }

            zoomCoroutine = StartCoroutine(AnimateZoom());
        }

        public void ResetToStartScale()
        {
            ResolveTarget();

            if (zoomTarget == null)
            {
                return;
            }

            zoomTarget.localScale = new Vector3(startScale, startScale, 1f);
        }

        private IEnumerator AnimateZoom()
        {
            ResetToStartScale();

            if (zoomDuration <= 0f)
            {
                zoomTarget.localScale = new Vector3(endScale, endScale, 1f);
                zoomCoroutine = null;
                yield break;
            }

            float elapsed = 0f;

            while (elapsed < zoomDuration)
            {
                elapsed += useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / zoomDuration);
                float scale = Mathf.Lerp(startScale, endScale, t);
                zoomTarget.localScale = new Vector3(scale, scale, 1f);
                yield return null;
            }

            zoomTarget.localScale = new Vector3(endScale, endScale, 1f);
            zoomCoroutine = null;
        }

        private void ResolveTarget()
        {
            if (zoomTarget == null)
            {
                zoomTarget = transform as RectTransform;
            }
        }
    }
}

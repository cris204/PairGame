using UnityEngine;

namespace CardMatch.Presentation.UI
{
    [RequireComponent(typeof(RectTransform))]
    public class UIPulseMotion : MonoBehaviour
    {
        [SerializeField] private float pulseScale = 1.02f;
        [SerializeField] private float pulseSpeed = 1.1f;
        [SerializeField] private float phaseOffset = 0f;
        [SerializeField] private bool useUnscaledTime = true;

        private RectTransform rectTransform;
        private Vector3 defaultScale;

        private void Awake()
        {
            rectTransform = GetComponent<RectTransform>();
            defaultScale = rectTransform.localScale;
        }

        private void OnEnable()
        {
            if (rectTransform == null)
            {
                rectTransform = GetComponent<RectTransform>();
            }

            defaultScale = rectTransform.localScale;
        }

        private void Update()
        {
            float time = useUnscaledTime ? Time.unscaledTime : Time.time;
            float wave = (Mathf.Sin((time + phaseOffset) * pulseSpeed * Mathf.PI * 2f) + 1f) * 0.5f;
            float scaleFactor = Mathf.Lerp(1f, pulseScale, wave);
            rectTransform.localScale = defaultScale * scaleFactor;
        }
    }
}

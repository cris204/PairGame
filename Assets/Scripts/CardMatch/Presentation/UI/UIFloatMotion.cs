using UnityEngine;

namespace CardMatch.Presentation.UI
{
    [RequireComponent(typeof(RectTransform))]
    public class UIFloatMotion : MonoBehaviour
    {
        [SerializeField] private float amplitude = 4f;
        [SerializeField] private float frequency = 0.6f;
        [SerializeField] private float phaseOffset = 0f;
        [SerializeField] private bool useUnscaledTime = true;

        private RectTransform rectTransform;
        private Vector2 defaultAnchoredPosition;

        private void Awake()
        {
            rectTransform = GetComponent<RectTransform>();
            defaultAnchoredPosition = rectTransform.anchoredPosition;
        }

        private void OnEnable()
        {
            if (rectTransform == null)
            {
                rectTransform = GetComponent<RectTransform>();
            }

            defaultAnchoredPosition = rectTransform.anchoredPosition;
        }

        private void Update()
        {
            float time = useUnscaledTime ? Time.unscaledTime : Time.time;
            float offsetY = Mathf.Sin((time + phaseOffset) * frequency * Mathf.PI * 2f) * amplitude;
            rectTransform.anchoredPosition = defaultAnchoredPosition + new Vector2(0f, offsetY);
        }
    }
}

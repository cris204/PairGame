using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace CardMatch.Presentation.UI
{
    public class StartMenuView : MonoBehaviour
    {
        [SerializeField] private GameObject rootContainer;
        [SerializeField] private Button newGameButton;
        [SerializeField] private Button loadGameButton;
        [SerializeField] private MenuBackgroundZoom backgroundZoom;
        [SerializeField] private bool playBackgroundZoomOnShow = true;

        private UIAnimatedPanel animatedPanel;
        private Action onNewGameRequested;
        private Action onLoadGameRequested;
        private Coroutine newGameTransitionCoroutine;
        private bool isStartingNewGame;

        private void Awake()
        {
            if (rootContainer == null)
            {
                rootContainer = gameObject;
            }

            ResolveAnimatedPanel();
            ResolveBackgroundZoom();
            BindButtons();
        }

        public void Initialize(Action onNewGame, Action onLoadGame)
        {
            onNewGameRequested = onNewGame;
            onLoadGameRequested = onLoadGame;
            BindButtons();
        }

        public void SetLoadAvailable(bool isAvailable)
        {
            if (loadGameButton == null)
            {
                return;
            }

            loadGameButton.gameObject.SetActive(isAvailable);
            loadGameButton.interactable = isAvailable;
        }

        public void Show()
        {
            if (rootContainer == null)
            {
                return;
            }

            ResolveAnimatedPanel();
            ResolveBackgroundZoom();

            if (animatedPanel != null)
            {
                animatedPanel.ShowAnimated();
            }
            else
            {
                rootContainer.SetActive(true);
            }
        }

        public void Hide()
        {
            if (rootContainer == null)
            {
                return;
            }

            if (newGameTransitionCoroutine != null)
            {
                StopCoroutine(newGameTransitionCoroutine);
                newGameTransitionCoroutine = null;
            }

            isStartingNewGame = false;

            ResolveAnimatedPanel();

            if (animatedPanel != null)
            {
                animatedPanel.HideAnimated();
                return;
            }

            rootContainer.SetActive(false);
        }

        private void BindButtons()
        {
            if (newGameButton != null)
            {
                newGameButton.onClick.RemoveListener(HandleNewGameClicked);
                newGameButton.onClick.AddListener(HandleNewGameClicked);
            }

            if (loadGameButton != null)
            {
                loadGameButton.onClick.RemoveListener(HandleLoadGameClicked);
                loadGameButton.onClick.AddListener(HandleLoadGameClicked);
            }
        }

        private void HandleNewGameClicked()
        {
            if (isStartingNewGame)
            {
                return;
            }

            if (newGameTransitionCoroutine != null)
            {
                StopCoroutine(newGameTransitionCoroutine);
            }

            newGameTransitionCoroutine = StartCoroutine(PlayNewGameTransition());
        }

        private void HandleLoadGameClicked()
        {
            onLoadGameRequested?.Invoke();
        }

        private void ResolveAnimatedPanel()
        {
            if (rootContainer == null)
            {
                return;
            }

            if (animatedPanel != null)
            {
                return;
            }

            animatedPanel = rootContainer.GetComponent<UIAnimatedPanel>();

            if (animatedPanel == null)
            {
                animatedPanel = rootContainer.GetComponentInChildren<UIAnimatedPanel>(true);
            }
        }

        private IEnumerator PlayNewGameTransition()
        {
            isStartingNewGame = true;
            SetButtonsInteractable(false);

            if (backgroundZoom != null)
            {
                backgroundZoom.PlayZoom();

                if (backgroundZoom.ZoomDuration > 0f)
                {
                    if (backgroundZoom.UseUnscaledTime)
                    {
                        yield return new WaitForSecondsRealtime(backgroundZoom.ZoomDuration);
                    }
                    else
                    {
                        yield return new WaitForSeconds(backgroundZoom.ZoomDuration);
                    }
                }
            }

            onNewGameRequested?.Invoke();
            isStartingNewGame = false;
            newGameTransitionCoroutine = null;
        }

        private void ResolveBackgroundZoom()
        {
            if (backgroundZoom != null)
            {
                return;
            }

            backgroundZoom = GetComponentInChildren<MenuBackgroundZoom>(true);
        }

        private void SetButtonsInteractable(bool isInteractable)
        {
            if (newGameButton != null)
            {
                newGameButton.interactable = isInteractable;
            }

            if (loadGameButton != null && loadGameButton.gameObject.activeSelf)
            {
                loadGameButton.interactable = isInteractable;
            }
        }
    }
}

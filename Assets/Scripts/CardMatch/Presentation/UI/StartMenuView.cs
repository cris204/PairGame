using System;
using UnityEngine;
using UnityEngine.UI;

namespace CardMatch.Presentation.UI
{
    public class StartMenuView : MonoBehaviour
    {
        [SerializeField] private GameObject rootContainer;
        [SerializeField] private Button newGameButton;
        [SerializeField] private Button loadGameButton;

        private UIAnimatedPanel animatedPanel;
        private Action onNewGameRequested;
        private Action onLoadGameRequested;

        private void Awake()
        {
            if (rootContainer == null)
            {
                rootContainer = gameObject;
            }

            ResolveAnimatedPanel();
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

            if (animatedPanel != null)
            {
                animatedPanel.ShowAnimated();
                return;
            }

            rootContainer.SetActive(true);
        }

        public void Hide()
        {
            if (rootContainer == null)
            {
                return;
            }

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
            onNewGameRequested?.Invoke();
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
    }
}

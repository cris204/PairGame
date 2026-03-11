using UnityEngine;
using System.Collections;
using CardMatch.Config;
using CardMatch.Core.Data;
using CardMatch.Core.Save;
using CardMatch.Core.Scoring;
using CardMatch.Presentation.Audio;
using CardMatch.Presentation.Board;
using CardMatch.Presentation.Cards;
using CardMatch.Presentation.HUD;
using CardMatch.Presentation.UI;

namespace CardMatch.Core.Gameplay
{
    public class GameManager : MonoBehaviour
    {
        [Header("Config")] [SerializeField] private GameConfig gameConfig;

        [Header("Scene References")] [SerializeField]
        private BoardManager boardManager;

        [SerializeField] private CardSelectionController selectionController;
        [SerializeField] private AudioManager audioManager;
        [SerializeField] private HUDView hudView;
        [SerializeField] private StartMenuView startMenuView;

        private SaveLoadService saveLoadService;
        private MatchResolver matchResolver;
        private ScoreManager scoreManager;
        private Coroutine previewCoroutine;
        private GameSaveData pendingMenuSaveData;

        private BoardSetupData currentBoardSetup;
        private bool isGameCompleted;

        public int CurrentScore
        {
            get
            {
                if (scoreManager == null)
                {
                    return 0;
                }

                return scoreManager.CurrentScore;
            }
        }

        public int CurrentCombo
        {
            get
            {
                if (scoreManager == null)
                {
                    return 0;
                }

                return scoreManager.CurrentCombo;
            }
        }

        private void Awake()
        {
            saveLoadService = new SaveLoadService();
            matchResolver = new MatchResolver();
            scoreManager = new ScoreManager(gameConfig);
            ResolveHudView();
            ResolveStartMenuView();
        }

        private void Start()
        {
            GameSaveData saveData = TryLoadCompatibleSave();

            if (startMenuView != null)
            {
                pendingMenuSaveData = saveData;
                ShowStartMenu();
                return;
            }

            if (saveData != null)
            {
                LoadGame(saveData);
            }
            else
            {
                StartNewGame(gameConfig.defaultRows, gameConfig.defaultColumns);
            }
        }

        public void StartNewGame(int rows, int columns)
        {
            StopPreviewIfRunning();
            HideStartMenu();
            currentBoardSetup = new BoardSetupData(rows, columns);

            if (!currentBoardSetup.IsValid())
            {
                Debug.LogError("Invalid board setup. Row and column counts must produce at least two slots.");
                return;
            }

            isGameCompleted = false;
            scoreManager.Reset();

            boardManager.BuildNewBoard(currentBoardSetup);
            BindCards();

            int totalPairs = currentBoardSetup.TotalCards() / 2;
            selectionController.Initialize(matchResolver, scoreManager, audioManager, gameConfig, totalPairs);
            selectionController.OnStateChanged = HandleStateChanged;
            selectionController.OnGameCompleted = HandleGameCompleted;
            selectionController.SetGameCompleted(false);

            EnableHud();
            RefreshHud();
            SaveIfNeeded();
            previewCoroutine = StartCoroutine(PlayNewGamePreview());
        }

        private void LoadGame(GameSaveData saveData)
        {
            StopPreviewIfRunning();
            HideStartMenu();

            if (saveData == null)
            {
                StartNewGame(gameConfig.defaultRows, gameConfig.defaultColumns);
                return;
            }

            currentBoardSetup = new BoardSetupData(saveData.Rows, saveData.Columns);
            isGameCompleted = saveData.IsGameCompleted;

            scoreManager.Restore(saveData.Score, saveData.Combo);
            boardManager.BuildFromSave(saveData);
            NormalizeLoadedBoardState();
            BindCards();

            int totalPairs = (saveData.Rows * saveData.Columns) / 2;
            int restoredMatchedPairs = CountMatchedPairs();

            if (ShouldStartNewGameFromSave(saveData, restoredMatchedPairs, totalPairs))
            {
                saveLoadService.DeleteSave();
                StartNewGame(gameConfig.defaultRows, gameConfig.defaultColumns);
                return;
            }

            selectionController.Initialize(matchResolver, scoreManager, audioManager, gameConfig, totalPairs);
            selectionController.RestoreMatchedPairs(restoredMatchedPairs, totalPairs);
            selectionController.OnStateChanged = HandleStateChanged;
            selectionController.OnGameCompleted = HandleGameCompleted;
            selectionController.SetGameCompleted(isGameCompleted);
            EnableHud();
            RefreshHud();
        }

        private GameSaveData TryLoadCompatibleSave()
        {
            if (!saveLoadService.HasSave())
            {
                return null;
            }

            GameSaveData saveData = saveLoadService.Load();

            if (saveData == null)
            {
                return null;
            }

            if (gameConfig == null)
            {
                return saveData;
            }

            bool sameRows = saveData.Rows == gameConfig.defaultRows;
            bool sameColumns = saveData.Columns == gameConfig.defaultColumns;

            if (sameRows && sameColumns)
            {
                return saveData;
            }

            saveLoadService.DeleteSave();
            return null;
        }

        public void DeleteSaveAndRestart()
        {
            saveLoadService.DeleteSave();
            StartNewGame(gameConfig.defaultRows, gameConfig.defaultColumns);
        }

        private void BindCards()
        {
            for (int i = 0; i < boardManager.Cards.Count; i++)
            {
                CardPresenter card = boardManager.Cards[i];
                card.OnCardSelected -= HandleCardSelected;
                card.OnCardSelected += HandleCardSelected;
            }
        }

        private void HandleCardSelected(CardPresenter card)
        {
            selectionController.TrySelectCard(card);
        }

        private void HandleStateChanged()
        {
            RefreshHud();
            SaveIfNeeded();
        }

        private void HandleGameCompleted()
        {
            isGameCompleted = true;
            RefreshHud();
            SaveIfNeeded();
        }

        private void SaveIfNeeded()
        {
            if (gameConfig == null)
            {
                return;
            }

            if (!gameConfig.autoSaveOnStateChange)
            {
                return;
            }

            GameSaveData saveData = boardManager.CreateSaveData(
                currentBoardSetup.Rows,
                currentBoardSetup.Columns,
                scoreManager.CurrentScore,
                scoreManager.CurrentCombo,
                isGameCompleted);

            saveLoadService.Save(saveData);
        }

        private int CountMatchedPairs()
        {
            int matchedCards = 0;

            for (int i = 0; i < boardManager.Cards.Count; i++)
            {
                if (boardManager.Cards[i].RuntimeData.IsMatched)
                {
                    matchedCards++;
                }
            }

            return matchedCards / 2;
        }

        private void NormalizeLoadedBoardState()
        {
            for (int i = 0; i < boardManager.Cards.Count; i++)
            {
                CardPresenter card = boardManager.Cards[i];

                if (card == null || card.RuntimeData == null)
                {
                    continue;
                }

                if (card.RuntimeData.IsMatched)
                {
                    continue;
                }

                if (card.RuntimeData.IsRevealed)
                {
                    card.HideImmediate();
                }
            }
        }
        
        private void EnableHud()
        {
            if (hudView == null)
            {
                return;
            }

            hudView.Show();
        }

        private void RefreshHud()
        {
            if (hudView == null)
            {
                return;
            }

            bool showCombo = gameConfig != null && gameConfig.enableCombo;
            hudView.UpdateHud(CurrentScore, CurrentCombo, showCombo, isGameCompleted);
        }

        private void ShowStartMenu()
        {
            if (startMenuView == null)
            {
                return;
            }

            startMenuView.Initialize(HandleStartMenuNewGameRequested, HandleStartMenuLoadRequested);
            startMenuView.SetLoadAvailable(pendingMenuSaveData != null);
            startMenuView.Show();
        }

        private void HideStartMenu()
        {
            if (startMenuView == null)
            {
                return;
            }

            startMenuView.Hide();
        }

        private void ResolveHudView()
        {
            if (hudView != null)
            {
                hudView.BindNewGameAction(HandleNewGameRequested);
            }
        }

        private void ResolveStartMenuView()
        {
            if (startMenuView == null)
            {
                startMenuView = FindObjectOfType<StartMenuView>();
            }
        }

        private void HandleNewGameRequested()
        {
            DeleteSaveAndRestart();
        }

        private void HandleStartMenuNewGameRequested()
        {
            pendingMenuSaveData = null;
            DeleteSaveAndRestart();
        }

        private void HandleStartMenuLoadRequested()
        {
            if (pendingMenuSaveData == null)
            {
                return;
            }

            GameSaveData saveData = pendingMenuSaveData;
            pendingMenuSaveData = null;
            LoadGame(saveData);
        }

        private IEnumerator PlayNewGamePreview()
        {
            SetCardsInteractable(false);
            yield return PlayRevealForAllCards();

            float revealDuration = GetInitialRevealDuration();

            if (revealDuration > 0f)
            {
                yield return new WaitForSeconds(revealDuration);
            }

            yield return PlayHideForAllCards();
            SetCardsInteractable(true);
            previewCoroutine = null;
        }

        private IEnumerator PlayRevealForAllCards()
        {
            for (int i = 0; i < boardManager.Cards.Count; i++)
            {
                CardPresenter card = boardManager.Cards[i];

                if (card == null || card.RuntimeData == null || card.RuntimeData.IsMatched)
                {
                    continue;
                }

                StartCoroutine(card.PlayReveal());
            }

            yield return WaitForCardAnimationsComplete();
        }

        private IEnumerator PlayHideForAllCards()
        {
            for (int i = 0; i < boardManager.Cards.Count; i++)
            {
                CardPresenter card = boardManager.Cards[i];

                if (card == null || card.RuntimeData == null || card.RuntimeData.IsMatched)
                {
                    continue;
                }

                StartCoroutine(card.PlayHide());
            }

            yield return WaitForCardAnimationsComplete();
        }

        private IEnumerator WaitForCardAnimationsComplete()
        {
            bool hasAnimatingCards = true;

            while (hasAnimatingCards)
            {
                hasAnimatingCards = false;

                for (int i = 0; i < boardManager.Cards.Count; i++)
                {
                    CardPresenter card = boardManager.Cards[i];

                    if (card != null && card.IsAnimating)
                    {
                        hasAnimatingCards = true;
                        break;
                    }
                }

                if (hasAnimatingCards)
                {
                    yield return null;
                }
            }
        }

        private void SetCardsInteractable(bool isInteractable)
        {
            for (int i = 0; i < boardManager.Cards.Count; i++)
            {
                CardPresenter card = boardManager.Cards[i];

                if (card == null || card.RuntimeData == null)
                {
                    continue;
                }

                if (card.RuntimeData.IsMatched)
                {
                    card.SetInteractable(false);
                    continue;
                }

                card.SetInteractable(isInteractable);
            }
        }

        private float GetInitialRevealDuration()
        {
            if (gameConfig == null)
            {
                return 1f;
            }

            return gameConfig.initialRevealDuration;
        }

        private void StopPreviewIfRunning()
        {
            if (previewCoroutine == null)
            {
                return;
            }

            StopCoroutine(previewCoroutine);
            previewCoroutine = null;
        }

        private bool ShouldStartNewGameFromSave(GameSaveData saveData, int restoredMatchedPairs, int totalPairs)
        {
            if (saveData != null && saveData.IsGameCompleted)
            {
                return true;
            }

            if (totalPairs <= 0)
            {
                return true;
            }

            return restoredMatchedPairs >= totalPairs;
        }
    }
}

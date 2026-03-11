using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CardMatch.Config;
using CardMatch.Core.Scoring;
using CardMatch.Presentation.Audio;
using CardMatch.Presentation.Cards;

namespace CardMatch.Core.Gameplay
{
    public class CardSelectionController : MonoBehaviour
    {
        private struct CardPairEvaluation
        {
            public CardPresenter First;
            public CardPresenter Second;
        }

        private readonly List<CardPresenter> currentSelection = new List<CardPresenter>();
        private readonly Queue<CardPairEvaluation> pendingPairs = new Queue<CardPairEvaluation>();
        private readonly HashSet<int> reservedCardIds = new HashSet<int>();

        private MatchResolver matchResolver;
        private ScoreManager scoreManager;
        private AudioManager audioManager;
        private GameConfig gameConfig;

        private bool isProcessingQueue;
        private bool isGameCompleted;
        private int matchedPairs;
        private int totalPairs;

        public System.Action OnStateChanged;
        public System.Action OnGameCompleted;

        public void Initialize(MatchResolver resolver, ScoreManager score, AudioManager audio, GameConfig config,
            int totalPairsCount)
        {
            StopAllCoroutines();

            matchResolver = resolver;
            scoreManager = score;
            audioManager = audio;
            gameConfig = config;
            matchedPairs = 0;
            totalPairs = totalPairsCount;
            currentSelection.Clear();
            pendingPairs.Clear();
            reservedCardIds.Clear();
            isProcessingQueue = false;
            isGameCompleted = false;
        }

        public void RestoreMatchedPairs(int restoredMatchedPairs, int totalPairsCount)
        {
            matchedPairs = restoredMatchedPairs;
            totalPairs = totalPairsCount;
        }

        public void SetGameCompleted(bool value)
        {
            isGameCompleted = value;
        }

        public bool TrySelectCard(CardPresenter card)
        {
            if (!CanSelectCard(card))
            {
                return false;
            }

            ReserveCard(card);
            StartCoroutine(HandleReveal(card));
            return true;
        }

        private IEnumerator HandleReveal(CardPresenter card)
        {
            audioManager.PlaySfx(GameSfx.Flip);
            yield return card.PlayReveal();

            currentSelection.Add(card);

            if (currentSelection.Count >= 2)
            {
                CardPairEvaluation pair = new CardPairEvaluation();
                pair.First = currentSelection[0];
                pair.Second = currentSelection[1];

                pendingPairs.Enqueue(pair);
                currentSelection.RemoveRange(0, 2);

                if (!isProcessingQueue)
                {
                    StartCoroutine(ProcessPendingPairs());
                }
            }

            OnStateChanged?.Invoke();
        }

        private IEnumerator ProcessPendingPairs()
        {
            isProcessingQueue = true;

            while (pendingPairs.Count > 0)
            {
                CardPairEvaluation pair = pendingPairs.Dequeue();

                if (pair.First == null || pair.Second == null)
                {
                    ReleaseCard(pair.First);
                    ReleaseCard(pair.Second);
                    continue;
                }

                bool isMatch = matchResolver.IsMatch(pair.First.RuntimeData, pair.Second.RuntimeData);

                if (isMatch)
                {
                    pair.First.MarkAsMatched();
                    pair.Second.MarkAsMatched();
                    ReleaseCard(pair.First);
                    ReleaseCard(pair.Second);

                    scoreManager.RegisterMatch();
                    matchedPairs++;

                    audioManager.PlaySfx(GameSfx.Match);
                    OnStateChanged?.Invoke();

                    if (matchedPairs >= totalPairs)
                    {
                        isGameCompleted = true;
                        audioManager.PlaySfx(GameSfx.GameOver);
                        OnGameCompleted?.Invoke();
                    }
                }
                else
                {
                    scoreManager.RegisterMismatch();
                    audioManager.PlaySfx(GameSfx.Mismatch);
                    pair.First.PlayMismatchFeedback();
                    pair.Second.PlayMismatchFeedback();
                    OnStateChanged?.Invoke();

                    yield return new WaitForSeconds(GetMismatchRevealDuration());

                    yield return pair.First.PlayHide();
                    yield return pair.Second.PlayHide();
                    ReleaseCard(pair.First);
                    ReleaseCard(pair.Second);

                    OnStateChanged?.Invoke();
                }
            }

            isProcessingQueue = false;
        }

        private bool CanSelectCard(CardPresenter card)
        {
            if (isGameCompleted)
            {
                return false;
            }

            if (matchResolver == null || scoreManager == null || audioManager == null)
            {
                return false;
            }

            if (card == null)
            {
                return false;
            }

            if (card.RuntimeData == null)
            {
                return false;
            }

            if (!card.CanBeSelected())
            {
                return false;
            }

            int cardId = card.RuntimeData.UniqueInstanceId;

            if (cardId <= 0)
            {
                return false;
            }

            if (reservedCardIds.Contains(cardId))
            {
                return false;
            }

            return true;
        }

        private void ReserveCard(CardPresenter card)
        {
            if (card == null || card.RuntimeData == null)
            {
                return;
            }

            reservedCardIds.Add(card.RuntimeData.UniqueInstanceId);
            card.SetInteractable(false);
        }

        private void ReleaseCard(CardPresenter card)
        {
            if (card == null || card.RuntimeData == null)
            {
                return;
            }

            reservedCardIds.Remove(card.RuntimeData.UniqueInstanceId);

            if (card.RuntimeData.IsMatched)
            {
                card.SetInteractable(false);
                return;
            }

            if (!card.RuntimeData.IsRevealed)
            {
                card.SetInteractable(true);
            }
        }

        private float GetMismatchRevealDuration()
        {
            if (gameConfig == null)
            {
                return 0f;
            }

            return gameConfig.mismatchRevealDuration;
        }
    }
}

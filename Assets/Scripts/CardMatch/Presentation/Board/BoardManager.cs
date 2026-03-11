using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using CardMatch.Config;
using CardMatch.Core.Data;
using CardMatch.Core.Save;
using CardMatch.Presentation.Cards;

namespace CardMatch.Presentation.Board
{
    public class BoardManager : MonoBehaviour
    {
        [SerializeField] private Transform cardsParent;
        [SerializeField] private CardPresenter cardPrefab;
        [SerializeField] private BoardLayoutFitter boardLayoutFitter;
        [SerializeField] private GameConfig gameConfig;
        [SerializeField] private Sprite cardBackSprite;
        [SerializeField] private List<Sprite> cardFrontSprites = new List<Sprite>();

        private readonly List<CardPresenter> cards = new List<CardPresenter>();
        private readonly Dictionary<int, Sprite> generatedFrontSprites = new Dictionary<int, Sprite>();
        private int nextCardUniqueId = 1;
        private Sprite generatedBackSprite;

        public IReadOnlyList<CardPresenter> Cards
        {
            get
            {
                return cards;
            }
        }

        public void ClearBoard()
        {
            if (cardsParent != null)
            {
                for (int i = cardsParent.childCount - 1; i >= 0; i--)
                {
                    Destroy(cardsParent.GetChild(i).gameObject);
                }
            }

            cards.Clear();
            generatedFrontSprites.Clear();
            generatedBackSprite = null;
            nextCardUniqueId = 1;
        }

        public void BuildNewBoard(BoardSetupData setupData)
        {
            ClearBoard();

            int totalSlots = setupData.TotalCards();
            int emptySlotIndex = GetEmptySlotIndex(totalSlots);
            List<CardRuntimeData> runtimeCards = CreateShuffledCardData(setupData);
            int cardDataIndex = 0;

            for (int slotIndex = 0; slotIndex < totalSlots; slotIndex++)
            {
                if (slotIndex == emptySlotIndex)
                {
                    CreateEmptySlot(slotIndex);
                    continue;
                }

                if (cardDataIndex >= runtimeCards.Count)
                {
                    break;
                }

                CreateCard(runtimeCards[cardDataIndex], slotIndex);
                cardDataIndex++;
            }

            if (boardLayoutFitter != null)
            {
                boardLayoutFitter.ApplyLayout(setupData.Rows, setupData.Columns);
            }
        }

        public void BuildFromSave(GameSaveData saveData)
        {
            ClearBoard();

            if (saveData == null)
            {
                return;
            }

            int totalSlots = saveData.Rows * saveData.Columns;
            Dictionary<int, CardSaveData> saveCardsByIndex = GetSaveCardsByIndex(saveData);

            for (int slotIndex = 0; slotIndex < totalSlots; slotIndex++)
            {
                CardSaveData saveCard;

                if (!saveCardsByIndex.TryGetValue(slotIndex, out saveCard))
                {
                    CreateEmptySlot(slotIndex);
                    continue;
                }

                CardRuntimeData runtimeData = new CardRuntimeData();
                runtimeData.UniqueInstanceId = saveCard.UniqueInstanceId;
                runtimeData.PairId = saveCard.PairId;
                runtimeData.BoardIndex = saveCard.BoardIndex;
                runtimeData.IsRevealed = saveCard.IsRevealed;
                runtimeData.IsMatched = saveCard.IsMatched;

                CreateCard(runtimeData, slotIndex);
            }

            nextCardUniqueId = GetNextUniqueId(saveData.Cards);

            if (boardLayoutFitter != null)
            {
                boardLayoutFitter.ApplyLayout(saveData.Rows, saveData.Columns);
            }
        }

        public GameSaveData CreateSaveData(int rows, int columns, int score, int combo, bool isGameCompleted)
        {
            GameSaveData data = new GameSaveData();
            data.Rows = rows;
            data.Columns = columns;
            data.Score = score;
            data.Combo = combo;
            data.IsGameCompleted = isGameCompleted;

            for (int i = 0; i < cards.Count; i++)
            {
                CardRuntimeData runtime = cards[i].RuntimeData;
                CardSaveData saveCard = new CardSaveData();
                saveCard.UniqueInstanceId = runtime.UniqueInstanceId;
                saveCard.PairId = runtime.PairId;
                saveCard.BoardIndex = runtime.BoardIndex;
                saveCard.IsRevealed = GetRevealedStateForSave(runtime);
                saveCard.IsMatched = runtime.IsMatched;
                data.Cards.Add(saveCard);
            }

            return data;
        }

        private void CreateCard(CardRuntimeData runtimeData, int siblingIndex)
        {
            if (cardPrefab == null || cardsParent == null)
            {
                return;
            }

            CardPresenter presenter = Instantiate(cardPrefab, cardsParent);
            RectTransform presenterTransform = presenter.transform as RectTransform;
            runtimeData.BoardIndex = siblingIndex;
            Sprite frontSprite = GetFrontSprite(runtimeData.PairId);
            Sprite backSprite = GetBackSprite();

            presenter.Initialize(runtimeData, gameConfig, frontSprite, backSprite);

            if (presenterTransform != null)
            {
                presenterTransform.SetSiblingIndex(siblingIndex);
            }

            cards.Add(presenter);
        }

        private List<CardRuntimeData> CreateShuffledCardData(BoardSetupData setupData)
        {
            int totalCards = GetPlayableCardCount(setupData.TotalCards());
            int pairCount = totalCards / 2;

            List<int> pairIds = new List<int>();

            for (int i = 0; i < pairCount; i++)
            {
                pairIds.Add(i);
                pairIds.Add(i);
            }

            Shuffle(pairIds);

            List<CardRuntimeData> result = new List<CardRuntimeData>();

            for (int i = 0; i < pairIds.Count; i++)
            {
                CardRuntimeData data = new CardRuntimeData();
                data.UniqueInstanceId = nextCardUniqueId;
                data.PairId = pairIds[i];
                data.BoardIndex = -1;
                data.IsMatched = false;
                data.IsRevealed = false;

                nextCardUniqueId++;
                result.Add(data);
            }

            return result;
        }

        private void Shuffle(List<int> values)
        {
            for (int i = values.Count - 1; i > 0; i--)
            {
                int randomIndex = Random.Range(0, i + 1);
                int temp = values[i];
                values[i] = values[randomIndex];
                values[randomIndex] = temp;
            }
        }

        private bool GetRevealedStateForSave(CardRuntimeData runtime)
        {
            if (runtime == null)
            {
                return false;
            }

            if (runtime.IsMatched)
            {
                return true;
            }

            return false;
        }

        private int GetNextUniqueId(List<CardSaveData> saveCards)
        {
            if (saveCards == null)
            {
                return 1;
            }

            int maxId = 0;

            for (int i = 0; i < saveCards.Count; i++)
            {
                if (saveCards[i].UniqueInstanceId > maxId)
                {
                    maxId = saveCards[i].UniqueInstanceId;
                }
            }

            return maxId + 1;
        }

        private Dictionary<int, CardSaveData> GetSaveCardsByIndex(GameSaveData saveData)
        {
            Dictionary<int, CardSaveData> result = new Dictionary<int, CardSaveData>();

            if (saveData == null || saveData.Cards == null)
            {
                return result;
            }

            for (int i = 0; i < saveData.Cards.Count; i++)
            {
                CardSaveData saveCard = saveData.Cards[i];

                if (saveCard == null)
                {
                    continue;
                }

                result[saveCard.BoardIndex] = saveCard;
            }

            return result;
        }

        private int GetPlayableCardCount(int totalSlotCount)
        {
            if (totalSlotCount % 2 == 0)
            {
                return totalSlotCount;
            }

            return totalSlotCount - 1;
        }

        private int GetEmptySlotIndex(int totalSlotCount)
        {
            if (totalSlotCount % 2 == 0)
            {
                return -1;
            }

            return totalSlotCount / 2;
        }

        private void CreateEmptySlot(int siblingIndex)
        {
            if (cardsParent == null)
            {
                return;
            }

            GameObject emptySlot = new GameObject("EmptySlot", typeof(RectTransform), typeof(Image));
            RectTransform emptyTransform = emptySlot.GetComponent<RectTransform>();
            Image emptyImage = emptySlot.GetComponent<Image>();

            emptyTransform.SetParent(cardsParent, false);
            emptyTransform.SetSiblingIndex(siblingIndex);
            emptyImage.color = new Color(0f, 0f, 0f, 0f);
            emptyImage.raycastTarget = false;
        }

        private Sprite GetBackSprite()
        {
            if (cardBackSprite != null)
            {
                return cardBackSprite;
            }

            if (generatedBackSprite == null)
            {
                Color baseColor = new Color(0.15f, 0.18f, 0.24f, 1f);
                Color accentColor = new Color(0.35f, 0.42f, 0.55f, 1f);
                generatedBackSprite = CreateProceduralSprite(baseColor, accentColor, 7);
            }

            return generatedBackSprite;
        }

        private Sprite GetFrontSprite(int pairId)
        {
            if (cardFrontSprites != null && cardFrontSprites.Count > 0)
            {
                int configuredIndex = pairId % cardFrontSprites.Count;
                return cardFrontSprites[configuredIndex];
            }

            Sprite generatedSprite;

            if (generatedFrontSprites.TryGetValue(pairId, out generatedSprite))
            {
                return generatedSprite;
            }

            float hue = Mathf.Repeat(pairId * 0.173f, 1f);
            Color baseColor = Color.HSVToRGB(hue, 0.55f, 0.95f);
            Color accentColor = Color.HSVToRGB(Mathf.Repeat(hue + 0.09f, 1f), 0.7f, 0.55f);
            int patternSeed = (pairId % 9) + 1;

            generatedSprite = CreateProceduralSprite(baseColor, accentColor, patternSeed);
            generatedFrontSprites[pairId] = generatedSprite;
            return generatedSprite;
        }

        private Sprite CreateProceduralSprite(Color baseColor, Color accentColor, int patternSeed)
        {
            int size = 96;
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            texture.filterMode = FilterMode.Bilinear;
            texture.wrapMode = TextureWrapMode.Clamp;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    bool isBorder = x < 6 || y < 6 || x >= size - 6 || y >= size - 6;
                    bool isAccentStripe = ((x + y + patternSeed * 5) / 12) % 2 == 0;
                    Color pixelColor = baseColor;

                    if (isBorder || isAccentStripe)
                    {
                        pixelColor = accentColor;
                    }

                    texture.SetPixel(x, y, pixelColor);
                }
            }

            texture.Apply();
            Rect rect = new Rect(0f, 0f, size, size);
            Vector2 pivot = new Vector2(0.5f, 0.5f);
            return Sprite.Create(texture, rect, pivot, 100f);
        }
    }
}

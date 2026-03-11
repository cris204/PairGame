using UnityEngine;
using UnityEngine.UI;
using CardMatch.Config;

namespace CardMatch.Presentation.Board
{
    [RequireComponent(typeof(GridLayoutGroup))]
    public class BoardLayoutFitter : MonoBehaviour
    {
        [SerializeField] private RectTransform boardContainer;
        [SerializeField] private GridLayoutGroup gridLayoutGroup;
        [SerializeField] private GameConfig gameConfig;

        public void ApplyLayout(int rows, int columns)
        {
            if (boardContainer == null || gridLayoutGroup == null || gameConfig == null)
            {
                return;
            }

            float width = boardContainer.rect.width;
            float height = boardContainer.rect.height;

            float spacing = gameConfig.boardSpacing;
            float padding = gameConfig.boardPadding;

            gridLayoutGroup.spacing = new Vector2(spacing, spacing);
            gridLayoutGroup.padding.left = Mathf.RoundToInt(padding);
            gridLayoutGroup.padding.right = Mathf.RoundToInt(padding);
            gridLayoutGroup.padding.top = Mathf.RoundToInt(padding);
            gridLayoutGroup.padding.bottom = Mathf.RoundToInt(padding);

            float usableWidth = width - padding - padding - (columns - 1) * spacing;
            float usableHeight = height - padding - padding - (rows - 1) * spacing;

            float cellWidth = usableWidth / columns;
            float cellHeight = usableHeight / rows;
            float finalSize = Mathf.Min(cellWidth, cellHeight);

            gridLayoutGroup.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            gridLayoutGroup.constraintCount = columns;
            gridLayoutGroup.cellSize = new Vector2(finalSize, finalSize);
        }
    }
}

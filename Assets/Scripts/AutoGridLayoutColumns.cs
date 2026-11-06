using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(GridLayoutGroup))]
public class AutoGridLayoutColumns : MonoBehaviour
{
    private GridLayoutGroup gridLayout;
    private RectTransform rectTransform;

    [Tooltip("Number of columns (must match GridLayoutGroup constraint count)")]
    public int columnCount = 2;

    [Tooltip("Desired aspect ratio (Width / Height). Set to 0 if you want to control height manually.")]
    public float cellAspectRatio = 1.0f; 

    void Start()
    {
        gridLayout = GetComponent<GridLayoutGroup>();
        rectTransform = GetComponent<RectTransform>();
        UpdateCellSize();
    }

    void OnRectTransformDimensionsChange()
    {
        UpdateCellSize();
    }

    public void UpdateCellSize()
    {
        if (gridLayout == null || rectTransform == null) return;

        // 1. Get available width inside padding
        float totalWidth = rectTransform.rect.width;
        float horizontalPadding = gridLayout.padding.left + gridLayout.padding.right;
        float availableWidth = totalWidth - horizontalPadding;

        // 2. Subtract total spacing between columns
        float totalSpacing = gridLayout.spacing.x * (columnCount - 1);
        float netWidth = availableWidth - totalSpacing;

        // 3. Calculate cell width
        float cellWidth = netWidth / columnCount;

        // 4. Determine cell height (using aspect ratio or fallback)
        float cellHeight = cellAspectRatio > 0 ? cellWidth / cellAspectRatio : gridLayout.cellSize.y;

        // 5. Apply new cell size
        gridLayout.cellSize = new Vector2(cellWidth, cellHeight);
    }
}
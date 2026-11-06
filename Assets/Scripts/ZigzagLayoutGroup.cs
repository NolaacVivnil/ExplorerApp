using UnityEngine;
using UnityEngine.UI;

[ExecuteAlways]
[RequireComponent(typeof(RectTransform))]
public class ZigzagLayoutGroup : MonoBehaviour
{
    [Header("Layout Control")]
    [Tooltip("Relative margin/gap between the left and right circles in the center. 0 = circles touch in the middle, higher values push them apart so they never overlap.")]
    [Range(-1f, 0.8f)]
    public float centerGapRatio = 0.1f;

    private RectTransform rectTransform;

    void OnEnable()
    {
        rectTransform = GetComponent<RectTransform>();
    }

    void Update()
    {
        ApplyLayout();
    }

    void ApplyLayout()
    {
        if (rectTransform == null) rectTransform = GetComponent<RectTransform>();

        // Count active children
        int activeCount = 0;
        for (int i = 0; i < transform.childCount; i++)
        {
            if (transform.GetChild(i).gameObject.activeSelf)
                activeCount++;
        }

        if (activeCount == 0) return;

        float containerWidth = rectTransform.rect.width;
        float containerHeight = rectTransform.rect.height;

        // 1. Calculate circle diameter (Width = Height) based on container width and gap ratio
        // Formula guarantees total width is shared between two circles + center gap, making overlap impossible.
        float rawDiameter = (containerWidth * (1f - centerGapRatio)) / 2f;
        float itemSize = Mathf.Round(Mathf.Max(10f, rawDiameter)); // Converge to crisp integer pixels

        // 2. Calculate automatic vertical spacing so items span 100% of container height
        float totalItemsHeight = activeCount * itemSize;
        float totalAvailableSpacing = containerHeight - totalItemsHeight;
        float spacing = (activeCount > 1) ? totalAvailableSpacing / (activeCount - 1) : 0f;

        // 3. Position and size all children
        float currentY = 0f;
        int visibleIndex = 0;

        for (int i = 0; i < transform.childCount; i++)
        {
            RectTransform child = transform.GetChild(i) as RectTransform;
            if (child == null || !child.gameObject.activeSelf) continue;

            // Ensure child's Aspect Ratio Fitter (if present) is set to 1:1 for circles
            AspectRatioFitter fitter = child.GetComponent<AspectRatioFitter>();
            if (fitter != null)
            {
                fitter.aspectRatio = 1f;
            }

            // Apply equal width and height for circular shape
            child.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, itemSize);
            child.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, itemSize);

            bool isEven = (visibleIndex % 2 != 0); // Index 0 = Odd (Left), Index 1 = Even (Right)

            // Anchor setup for top-left positioning
            child.anchorMin = new Vector2(0f, 1f);
            child.anchorMax = new Vector2(0f, 1f);
            child.pivot = new Vector2(0f, 1f);

            if (!isEven)
            {
                // Odd Item: Stick to Left edge (X = 0)
                child.anchoredPosition = new Vector2(0f, -currentY);
            }
            else
            {
                // Even Item: Stick to Right edge (X = ContainerWidth - Diameter)
                float rightX = containerWidth - itemSize;
                child.anchoredPosition = new Vector2(rightX, -currentY);
            }

            currentY += itemSize + spacing;
            visibleIndex++;
        }
    }
}
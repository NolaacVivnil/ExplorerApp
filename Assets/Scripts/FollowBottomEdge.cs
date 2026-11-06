using UnityEngine;

[ExecuteAlways] // Allows you to see it update in real-time in the Scene view
public class FollowBottomEdge : MonoBehaviour
{
    [Tooltip("The reference GameObject whose bottom edge you want to follow.")]
    public RectTransform target;
    [Tooltip("Added padding")]
    public int padding = 0;

    private RectTransform rect;

    void Start()
    {
        rect = GetComponent<RectTransform>();
    }

    void Update()
    {
        if (target == null || rect == null) return;

        // 1. Get the world space corners of GameObject B
        Vector3[] cornersB = new Vector3[4];
        target.GetWorldCorners(cornersB);
        // cornersB[0] is the bottom-left corner in world space
        float bottomYWorld = cornersB[0].y;

        // 2. Get GameObject A's parent RectTransform
        RectTransform parentRect = rect.parent as RectTransform;
        if (parentRect == null) return;

        // 3. Convert B's bottom world Y into A's parent local space
        Vector3 localPos = parentRect.InverseTransformPoint(new Vector3(0, bottomYWorld, 0));
        float targetLocalY = localPos.y;

        // 4. Calculate the distance from the parent's top edge to B's bottom edge
        float parentTopY = parentRect.rect.yMax;
        float topOffset = parentTopY - targetLocalY;

        // 5. Apply the offset to GameObject A's top (offsetMax.y)
        Vector2 offsetMax = rect.offsetMax;
        offsetMax.y = -topOffset - padding;
        rect.offsetMax = offsetMax;
    }
}
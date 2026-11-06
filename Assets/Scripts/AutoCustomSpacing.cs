using UnityEngine;
using UnityEngine.UI;

[ExecuteAlways]
[RequireComponent(typeof(RectTransform))]
public class AutoCustomSpacing : MonoBehaviour
{
    [Tooltip("The extra spacing/gap to apply below this specific item.")]
    public float extraSpacing = 10f;

    private const string SpacerName = "AUTO_SPACER_ELEMENT";
    private GameObject spacerObj;
    private bool shouldUpdate;

    void OnEnable()
    {
        shouldUpdate = true;
    }

    void OnValidate()
    {
        // Safe to flag for updates here without triggering Unity layout errors
        shouldUpdate = true;
    }

    void LateUpdate()
    {
        if (shouldUpdate)
        {
            UpdateSpacer();
            shouldUpdate = false;
        }
    }

    public void UpdateSpacer()
    {
        Transform myTransform = transform;
        int myIndex = myTransform.GetSiblingIndex();
        Transform parent = myTransform.parent;

        if (parent == null) return;

        // Check if the next sibling is already our auto-generated spacer
        if (myIndex + 1 < parent.childCount)
        {
            Transform nextSibling = parent.GetChild(myIndex + 1);
            if (nextSibling.name == SpacerName)
            {
                spacerObj = nextSibling.gameObject;
            }
        }

        // If the spacer doesn't exist yet, create it with a RectTransform
        if (spacerObj == null)
        {
            spacerObj = new GameObject(SpacerName, typeof(RectTransform));
            spacerObj.transform.SetParent(parent, false);
        }

        // Ensure it sits directly below this item in the hierarchy order
        spacerObj.transform.SetSiblingIndex(myIndex + 1);

        // Directly adjust the RectTransform height safely outside of OnValidate
        if (spacerObj.TryGetComponent<RectTransform>(out var rectTransform))
        {
            rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, extraSpacing);
            rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 0f);
        }
    }

    void OnDestroy()
    {
        if (spacerObj != null)
        {
            if (Application.isPlaying)
                Destroy(spacerObj);
            else
                DestroyImmediate(spacerObj);
        }
    }
}
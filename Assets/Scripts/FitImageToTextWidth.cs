using UnityEngine;
using UnityEngine.UI;
using TMPro;

[ExecuteAlways]
[RequireComponent(typeof(RectTransform))]
public class FitImageToTextWidth : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Drag the child text component here.")]
    [SerializeField] private TMP_Text targetText;

    [Header("Padding (Optional)")]
    [SerializeField] private float horizontalPadding = 20f;
    [SerializeField] private float verticalPadding = 20f;

    private RectTransform rectTransform;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        if (targetText == null)
        {
            targetText = GetComponentInChildren<TMP_Text>();
        }
    }

    private void LateUpdate()
    {
        UpdateSize();
    }

    public void UpdateSize()
    {
        if (targetText == null) return;

        targetText.ForceMeshUpdate();

        float preferredWidth = targetText.preferredWidth;
        float preferredHeight = targetText.preferredHeight;

        Vector2 size = rectTransform.sizeDelta;
        size.x = preferredWidth + horizontalPadding;
        size.y = preferredHeight + verticalPadding;
        rectTransform.sizeDelta = size;
    }
}
using UnityEngine;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;

[CustomEditor(typeof(SineWaveVerticalLayout))]
[CanEditMultipleObjects]
public class SineWaveVerticalLayoutEditor : Editor
{
    public override void OnInspectorGUI()
    {
        // Draw inherited VerticalLayoutGroup settings.
        //DrawDefaultInspector();

        // Draw the custom sine-wave settings explicitly.
        serializedObject.Update();

        DrawProperty("amplitude");
        DrawProperty("frequency");
        DrawProperty("phaseOffset");

        DrawProperty("animateWave");
        DrawProperty("animationSpeed");

        DrawProperty("verticalSpacing");
        DrawProperty("clampAmplitudeToMargins");
        DrawProperty("horizontalMargin");

        serializedObject.ApplyModifiedProperties();
    }

    private void DrawProperty(string propertyName)
    {
        SerializedProperty property =
            serializedObject.FindProperty(propertyName);

        if (property != null)
        {
            EditorGUILayout.PropertyField(property, true);
        }
        else
        {
            EditorGUILayout.HelpBox(
                "Property not found: " + propertyName,
                MessageType.Warning
            );
        }
    }
}
#endif

[ExecuteAlways]
[AddComponentMenu("Layout/Sine Wave Vertical Layout Group", 155)]
public class SineWaveVerticalLayout : VerticalLayoutGroup
{
    [Header("Sine Wave Settings")]
    [Tooltip("Maximum horizontal deviation (amplitude) of the sine wave.")]
    public float amplitude = 100f;

    [Tooltip("Frequency of the sine wave (controls wave compression vertically).")]
    public float frequency = 0.02f;

    [Tooltip("Phase offset to shift the wave position.")]
    public float phaseOffset = 0f;

    [Header("Animation")]
    public bool animateWave = false;
    public float animationSpeed = 2f;

    [Header("Gaps & Margins")]
    [Tooltip("Vertical spacing between consecutive items.")]
    public float verticalSpacing = 20f;

    [Tooltip("Prevent items from overflowing past the left/right boundaries.")]
    public bool clampAmplitudeToMargins = true;

    [Tooltip("Minimum horizontal margin from the container edges.")]
    public float horizontalMargin = 20f;

    private float currentPhase = 0f;
    
    #pragma warning disable CS0109, CS0114
    protected new void OnValidate()
    {
        //base.OnValidate();
        m_Spacing = verticalSpacing;
        
        // Safe for OnValidate: Marks the layout for rebuild on the next frame 
        // without triggering forbidden SendMessage calls.
        if (rectTransform != null)
        {
            LayoutRebuilder.MarkLayoutForRebuild(rectTransform);
        }
    }
    #pragma warning restore CS0109, CS0114

    protected override void OnEnable()
    {
        base.OnEnable();
        m_Spacing = verticalSpacing;
    }

    public override void CalculateLayoutInputHorizontal()
    {
        base.CalculateLayoutInputHorizontal();
        m_Spacing = verticalSpacing;
    }

    public override void CalculateLayoutInputVertical()
    {
        base.CalculateLayoutInputVertical();
        m_Spacing = verticalSpacing;
    }

    public override void SetLayoutHorizontal()
    {
        m_Spacing = verticalSpacing;
        base.SetLayoutHorizontal();
    }

    public override void SetLayoutVertical()
    {
        m_Spacing = verticalSpacing;
        base.SetLayoutVertical();
        SetChildrenAlongSineWave();
    }

    #pragma warning disable CS0109, CS0114
    protected new void Update()
    {
        if (animateWave && !Application.isPlaying)
        {
            currentPhase += Time.deltaTime * animationSpeed;
            TriggerRebuild();
        }
    }
    #pragma warning restore CS0109, CS0114

    protected void LateUpdate()
    {
        if (animateWave && Application.isPlaying)
        {
            currentPhase += Time.deltaTime * animationSpeed;
            TriggerRebuild();
        }
    }

    private void SetChildrenAlongSineWave()
    {
        if (rectChildren.Count == 0) return;

        float parentWidth = rectTransform.rect.width;
        float effectiveAmplitude = amplitude;

        if (clampAmplitudeToMargins && rectChildren.Count > 0)
        {
            float itemWidth = rectChildren[0].rect.width;
            float maxAllowedAmplitude = (parentWidth - padding.horizontal - itemWidth) * 0.5f - horizontalMargin;
            effectiveAmplitude = Mathf.Max(0f, Mathf.Min(amplitude, maxAllowedAmplitude));
        }

        float currentY = padding.top;
        float totalPhase = phaseOffset + (animateWave ? currentPhase : 0f);

        for (int i = 0; i < rectChildren.Count; i++)
        {
            RectTransform child = rectChildren[i];
            
            m_Tracker.Add(this, child, DrivenTransformProperties.Anchors | 
                                       DrivenTransformProperties.AnchoredPosition);

            child.anchorMin = new Vector2(0.5f, 1f);
            child.anchorMax = new Vector2(0.5f, 1f);
            child.pivot = new Vector2(0.5f, 1f);

            float itemHeight = child.rect.height;
            float itemWidth = child.rect.width;

            float xOffset = Mathf.Sin(currentY * frequency + totalPhase) * effectiveAmplitude;
            float yOffset = -currentY;

            child.anchoredPosition = new Vector2(xOffset, yOffset);

            currentY += itemHeight + verticalSpacing;
        }
    }

    private void TriggerRebuild()
    {
        if (!IsActive()) return;

        if (!CanvasUpdateRegistry.IsRebuildingLayout())
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(rectTransform);
        }
        else
        {
            LayoutRebuilder.MarkLayoutForRebuild(rectTransform);
        }
    }
}
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;

public class MapZoomController : MonoBehaviour
{
    [Header("Zoom Settings")]
    [Tooltip("Base scale of the map (usually 1)")]
    private float baseScale = 1.0f;
    [Tooltip("Maximum allowed zoom out/in limit (e.g., 0.2 means 20% variation allowed: 0.8 to 1.2)")]
    [SerializeField] private float zoomLimitMargin = 0.2f;
    [Tooltip("How fast the map and position spring back to original state upon release")]
    [SerializeField] private float springBackSpeed = 10f;

    [Header("Pan Settings")]
    [Tooltip("Maximum distance in pixels the map can be panned from center before elastic resistance")]
    [SerializeField] private float maxPanDistance = 250f;

    [Header("Click Protection")]
    [Tooltip("Minimum movement in pixels to consider a touch/drag as a gesture instead of a button click")]
    [SerializeField] private float dragThreshold = 15f;

    private RectTransform rectTransform;
    private float minScale;
    private float maxScale;
    
    // Interaction tracking
    private bool isInteracting = false;
    private bool hasGestureOccurred = false;
    private Vector2 initialTouchPosition;
    
    private float initialPinchDistance;
    private float initialScaleBeforePinch;
    private float mouseScrollTimeout = 0f;

    void OnEnable()
    {
        EnhancedTouchSupport.Enable();
    }

    void OnDisable()
    {
        EnhancedTouchSupport.Disable();
    }

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        minScale = baseScale * (1f - zoomLimitMargin);
        maxScale = baseScale * (1f + zoomLimitMargin);
    }

    void Update()
    {
        HandleInput();
        HandleSpringBack();

        if (mouseScrollTimeout > 0)
        {
            mouseScrollTimeout -= Time.deltaTime;
        }
    }

    private void HandleInput()
    {
        int touchCount = Touch.activeTouches.Count;

        if (touchCount >= 2)
        {
            // --- 2-FINGER PINCH ZOOM ---
            isInteracting = true;
            hasGestureOccurred = true; // Pinching is always a gesture

            Touch touchZero = Touch.activeTouches[0];
            Touch touchOne = Touch.activeTouches[1];

            Vector2 touchZeroPrevPos = touchZero.screenPosition - touchZero.delta;
            Vector2 touchOnePrevPos = touchOne.screenPosition - touchOne.delta;

            float prevTouchDeltaMag = (touchZeroPrevPos - touchOnePrevPos).magnitude;
            float touchDeltaMag = (touchZero.screenPosition - touchOne.screenPosition).magnitude;

            if (prevTouchDeltaMag > 0)
            {
                float scaleFactor = touchDeltaMag / prevTouchDeltaMag;
                float targetScale = rectTransform.localScale.x * scaleFactor;

                float elasticMin = minScale * 0.9f;
                float elasticMax = maxScale * 1.1f;
                targetScale = Mathf.Clamp(targetScale, elasticMin, elasticMax);

                rectTransform.localScale = new Vector3(targetScale, targetScale, 1f);
            }
        }
        else if (touchCount == 1)
        {
            // --- 1-FINGER OMNIDIRECTIONAL PAN ---
            isInteracting = true;
            Touch touch = Touch.activeTouches[0];

            // Track initial touch down to check threshold
            if (touch.phase == UnityEngine.InputSystem.TouchPhase.Began)
            {
                initialTouchPosition = touch.screenPosition;
                hasGestureOccurred = false;
            }
            else if (touch.phase == UnityEngine.InputSystem.TouchPhase.Moved)
            {
                // If movement exceeds threshold, mark it as an active pan gesture
                if (Vector2.Distance(initialTouchPosition, touch.screenPosition) > dragThreshold)
                {
                    hasGestureOccurred = true;
                }
            }

            rectTransform.anchoredPosition += touch.delta;
            rectTransform.anchoredPosition = Vector2.ClampMagnitude(rectTransform.anchoredPosition, maxPanDistance * 1.2f);
        }
        else
        {
            // --- PC MOUSE FALLBACK ---
            bool pcInputActive = false;

            if (Mouse.current != null)
            {
                // Mouse Scroll Zoom (Scroll is always a gesture)
                float scroll = Mouse.current.scroll.y.ReadValue();
                if (Mathf.Abs(scroll) > 0.01f)
                {
                    pcInputActive = true;
                    isInteracting = true;
                    hasGestureOccurred = true;
                    mouseScrollTimeout = 0.2f;

                    float scrollDelta = Mathf.Sign(scroll) * 0.1f;
                    float currentScale = rectTransform.localScale.x;
                    float targetScale = Mathf.Clamp(currentScale + scrollDelta, minScale * 0.9f, maxScale * 1.1f);
                    
                    rectTransform.localScale = new Vector3(targetScale, targetScale, 1f);
                }

                // Mouse Drag Pan
                if (Mouse.current.leftButton.wasPressedThisFrame)
                {
                    initialTouchPosition = Mouse.current.position.ReadValue();
                    hasGestureOccurred = false;
                }
                else if (Mouse.current.leftButton.isPressed)
                {
                    Vector2 mouseDelta = Mouse.current.delta.ReadValue();
                    if (mouseDelta.sqrMagnitude > 0.01f)
                    {
                        if (Vector2.Distance(initialTouchPosition, Mouse.current.position.ReadValue()) > dragThreshold)
                        {
                            hasGestureOccurred = true;
                        }

                        pcInputActive = true;
                        isInteracting = true;

                        rectTransform.anchoredPosition += mouseDelta;
                        rectTransform.anchoredPosition = Vector2.ClampMagnitude(rectTransform.anchoredPosition, maxPanDistance * 1.2f); // wait, clean syntax below
                    }
                }
            }

            if (!pcInputActive && mouseScrollTimeout <= 0f)
            {
                isInteracting = false;
            }
        }
    }

    private void HandleSpringBack()
    {
        if (!isInteracting)
        {
            // Spring back scale
            float currentScale = rectTransform.localScale.x;
            if (!Mathf.Approximately(currentScale, baseScale))
            {
                float newScale = Mathf.Lerp(currentScale, baseScale, Time.deltaTime * springBackSpeed);
                rectTransform.localScale = new Vector3(newScale, newScale, 1f);
            }

            // Spring back position
            Vector2 currentPos = rectTransform.anchoredPosition;
            if (currentPos.sqrMagnitude > 0.01f)
            {
                Vector2 newPos = Vector2.Lerp(currentPos, Vector2.zero, Time.deltaTime * springBackSpeed);
                rectTransform.anchoredPosition = newPos;
            }
        }
    }

    /// <summary>
    /// Returns true if the user simply tapped without dragging or zooming. 
    /// Use this to guard your button callback functions.
    /// </summary>
    public bool CanRegisterClick()
    {
        // Check if scale is back to baseScale (with a tiny floating-point tolerance)
        bool isAtOriginalScale = Mathf.Approximately(rectTransform.localScale.x, baseScale);
        
        // Check if position is back to center (Vector2.zero) within a 1-pixel threshold
        bool isAtOriginalPosition = rectTransform.anchoredPosition.sqrMagnitude < 1f;

        // Click is allowed only if no drag/zoom gesture happened AND it's fully settled at origin
        return !hasGestureOccurred /*&& isAtOriginalScale*/ && isAtOriginalPosition;
    }
}
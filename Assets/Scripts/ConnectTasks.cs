using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;

[RequireComponent(typeof(LineRenderer))]
public class ConnectTasks : MonoBehaviour
{
    private LineRenderer lineRenderer;

    [Header("Curve Settings")]
    [Tooltip("How many small segments to draw per curve section. Higher = smoother.")]
    [Range(2, 40)]
    public int resolution = 20;

    private const string SpacerName = "AUTO_SPACER_ELEMENT";

    void Start()
    {
        lineRenderer = GetComponent<LineRenderer>();
        lineRenderer.useWorldSpace = true;
    }

    void LateUpdate()
    {
        RectTransform rectTransform = GetComponent<RectTransform>();
        if (rectTransform != null)
        {
            // Force layout rebuild so scene switches calculate positions properly
            LayoutRebuilder.ForceRebuildLayoutImmediate(rectTransform);
        }

        int childCount = transform.childCount;
        if (childCount < 2)
        {
            lineRenderer.positionCount = 0;
            return;
        }

        // 1. Gather all valid children, skipping AUTO_SPACER_ELEMENT
        List<Transform> validItems = new List<Transform>();
        for (int i = 0; i < childCount; i++)
        {
            Transform child = transform.GetChild(i);
            if (child.name == SpacerName) continue;
            validItems.Add(child);
        }

        if (validItems.Count < 2)
        {
            lineRenderer.positionCount = 0;
            return;
        }

        List<Vector3> allPoints = new List<Vector3>();

        // Baseline flat Z depth
        float flatZ = transform.position.z + 5f; 

        // Start at the center of the first element
        Vector3 currentPos = GetElementCenter(validItems[0], flatZ);
        allPoints.Add(currentPos);

        // 2. Iterate through valid items and build connection paths
        for (int i = 0; i < validItems.Count - 1; i++)
        {
            Transform currentItem = validItems[i];
            Transform nextItem = validItems[i + 1];

            Vector3 startPoint = GetElementCenter(currentItem, flatZ);

            // Explicitly check if the next item is a MapsLinkPrefab by name
            bool isMapsLink = nextItem.name.Contains("MapsLink");

            if (isMapsLink)
            {
                // Find the Button child inside MapsLinkPrefab
                Transform buttonChild = nextItem.Find("Button");
                if (buttonChild == null)
                {
                    Button btn = nextItem.GetComponentInChildren<Button>();
                    if (btn != null) buttonChild = btn.transform;
                }

                if (buttonChild != null)
                {
                    // Get center of Button (correctly factors in (0, 1) pivot)
                    Vector3 buttonCenter = GetElementCenter(buttonChild, flatZ);

                    // Forward curve: Down-then-right
                    List<Vector3> forwardCurve = GenerateDownThenRightCurve(startPoint, buttonCenter);
                    for (int p = 1; p < forwardCurve.Count; p++)
                    {
                        allPoints.Add(forwardCurve[p]);
                    }

                    // Backward curve: Perfectly matches by reversing the exact forward curve points
                    for (int p = forwardCurve.Count - 2; p >= 0; p--)
                    {
                        allPoints.Add(forwardCurve[p]);
                    }
                }
                else
                {
                    // Fallback to direct segment if button is missing
                    AddDirectSegment(startPoint, GetElementCenter(nextItem, flatZ), allPoints);
                }
            }
            else
            {
                // Connect directly center-to-center to the next TaskPrefab
                Vector3 endPoint = GetElementCenter(nextItem, flatZ);
                AddDirectSegment(startPoint, endPoint, allPoints);
            }
        }

        // 3. Feed points into Line Renderer
        lineRenderer.positionCount = allPoints.Count;
        lineRenderer.SetPositions(allPoints.ToArray());
    }

    void AddDirectSegment(Vector3 start, Vector3 end, List<Vector3> allPoints)
    {
        List<Vector3> directSegment = GenerateStraightSegment(start, end, resolution);
        for (int p = 1; p < directSegment.Count; p++)
        {
            allPoints.Add(directSegment[p]);
        }
    }

    Vector3 GetElementCenter(Transform t, float targetZ)
    {
        if (t == null) return Vector3.zero;
        
        RectTransform rect = t.GetComponent<RectTransform>();
        Vector3 worldCenter;
        if (rect != null)
        {
            Vector2 pivot = rect.pivot;
            Vector2 size = rect.rect.size;
            Vector3 localCenter = new Vector3((0.5f - pivot.x) * size.x, (0.5f - pivot.y) * size.y, 0);
            worldCenter = rect.TransformPoint(localCenter);
        }
        else
        {
            worldCenter = t.position;
        }

        worldCenter.z = targetZ;
        return worldCenter;
    }

    List<Vector3> GenerateDownThenRightCurve(Vector3 start, Vector3 end)
    {
        List<Vector3> segmentPoints = new List<Vector3>();

        // Orthogonal corner control point: shares X of start and Y of end (down-then-right behavior)
        Vector3 controlPoint = new Vector3(start.x, end.y, start.z);

        for (int j = 0; j <= resolution; j++)
        {
            float t = j / (float)resolution;
            segmentPoints.Add(CalculateQuadraticBezierPoint(t, start, controlPoint, end));
        }
        return segmentPoints;
    }

    List<Vector3> GenerateStraightSegment(Vector3 start, Vector3 end, int res)
    {
        List<Vector3> segmentPoints = new List<Vector3>();
        for (int j = 0; j <= res; j++)
        {
            float t = j / (float)res;
            segmentPoints.Add(Vector3.Lerp(start, end, t));
        }
        return segmentPoints;
    }

    Vector3 CalculateQuadraticBezierPoint(float t, Vector3 p0, Vector3 p1, Vector3 p2)
    {
        float u = 1f - t;
        float tt = t * t;
        float uu = u * u;

        Vector3 p = uu * p0;
        p += 2f * u * t * p1;
        p += tt * p2;
        return p;
    }
}
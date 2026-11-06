using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(LineRenderer))]
public class ConnectCurve : MonoBehaviour
{
    private LineRenderer lineRenderer;

    [Header("Curve Settings")]
    [Tooltip("How much the lines bulge or bend outward between children.")]
    public float curveIntensity = 1.5f;

    [Tooltip("How many small segments to draw per connection. Higher = smoother.")]
    [Range(2, 40)]
    public int resolution = 20;

    [Tooltip("If checked, alternates the curve direction (left/right or up/down) like a wave or rope.")]
    public bool alternatingWaves = true;

    void Start()
    {
        lineRenderer = GetComponent<LineRenderer>();
        lineRenderer.useWorldSpace = true;
    }

    void LateUpdate()
    {
        int childCount = transform.childCount;
        if (childCount < 2)
        {
            lineRenderer.positionCount = 0;
            return;
        }

        // 1. Gather all true child centers
        List<Vector3> points = new List<Vector3>();
        for (int i = 0; i < childCount; i++)
        {
            Transform child = transform.GetChild(i);
            Vector3 trueCenter = GetTrueCenter(child);
            trueCenter.z += 0.1f; // Depth adjustment
            points.Add(trueCenter);
        }

        // 2. Generate smooth Bézier curve points between each pair of children
        List<Vector3> curvePoints = GenerateBezierCurvePoints(points, resolution, curveIntensity, alternatingWaves);

        // 3. Feed points into Line Renderer
        lineRenderer.positionCount = curvePoints.Count;
        lineRenderer.SetPositions(curvePoints.ToArray());
    }

    Vector3 GetTrueCenter(Transform child)
    {
        // UI Elements (RectTransform) with pivot (0.5, 1)
        RectTransform rect = child.GetComponent<RectTransform>();
        if (rect != null)
        {
            float halfHeight = rect.rect.height * 0.5f;
            return rect.TransformPoint(new Vector3(0, -halfHeight, 0));
        }

        // 2D Sprites (SpriteRenderer)
        SpriteRenderer spriteRenderer = child.GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            return spriteRenderer.bounds.center;
        }

        // Fallback
        return child.position;
    }

    List<Vector3> GenerateBezierCurvePoints(List<Vector3> nodes, int numSegments, float intensity, bool alternate)
    {
        List<Vector3> result = new List<Vector3>();

        for (int i = 0; i < nodes.Count - 1; i++)
        {
            Vector3 startPoint = nodes[i];
            Vector3 endPoint = nodes[i + 1];

            // Calculate midpoint
            Vector3 midPoint = (startPoint + endPoint) * 0.5f;

            // Find perpendicular direction to push the curve outward
            Vector3 direction = (endPoint - startPoint).normalized;
            Vector3 perpendicular = new Vector3(-direction.y, direction.x, 0); // 2D perpendicular normal

            // If alternating waves is on, flip the curve direction for each step
            if (alternate && i % 2 == 1)
            {
                perpendicular = -perpendicular;
            }

            // Control point for Quadratic Bézier curve
            Vector3 controlPoint = midPoint + (perpendicular * intensity);

            // Generate points along this quadratic Bézier curve segment
            for (int j = 0; j < numSegments; j++)
            {
                float t = j / (float)numSegments;
                Vector3 evaluatedPoint = CalculateQuadraticBezierPoint(t, startPoint, controlPoint, endPoint);
                result.Add(evaluatedPoint);
            }
        }

        // Add the very last child point
        result.Add(nodes[nodes.Count - 1]);
        return result;
    }

    Vector3 CalculateQuadraticBezierPoint(float t, Vector3 p0, Vector3 p1, Vector3 p2)
    {
        float u = 1f - t;
        float tt = t * t;
        float uu = u * u;

        Vector3 p = uu * p0; // (1-t)^2 * P0
        p += 2f * u * t * p1; // 2(1-t)t * P1
        p += tt * p2; // t^2 * P2

        return p;
    }
}
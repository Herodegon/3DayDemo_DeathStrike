using System;
using UnityEngine;

public class LineRendererData
{
    public LineRenderer lineRenderer;
    public Func<Vector3> getStartWorldPosition;
    public Func<Vector3> getEndWorldPosition;

    public void Update()
    {
        if (lineRenderer == null || getStartWorldPosition == null || getEndWorldPosition == null)
            return;

        lineRenderer.SetPosition(0, getStartWorldPosition());
        lineRenderer.SetPosition(1, getEndWorldPosition());
    }
}

public static class GlobalHelper
{
    public static LineRendererData CreateLineRenderer(GameObject parent, Func<Vector3> getStartWorldPosition, Func<Vector3> getEndWorldPosition, Color color, float width = 0.1f)
    {
        GameObject lineObject = new GameObject("Line");
        lineObject.transform.SetParent(parent.transform);
        lineObject.transform.localPosition = Vector3.zero;
        lineObject.transform.localRotation = Quaternion.identity;
        lineObject.transform.localScale = Vector3.one;
        LineRenderer lineRenderer = lineObject.AddComponent<LineRenderer>();
        lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
        lineRenderer.startColor = color;
        lineRenderer.endColor = color;
        lineRenderer.startWidth = width;
        lineRenderer.endWidth = width;
        lineRenderer.positionCount = 2;
        return new LineRendererData
        {
            lineRenderer = lineRenderer,
            getStartWorldPosition = getStartWorldPosition,
            getEndWorldPosition = getEndWorldPosition
        };
    }
}
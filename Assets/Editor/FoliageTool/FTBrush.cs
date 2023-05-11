using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering;

public class FTBrush
{
    public bool Display = false;
    public Vector3 Position;
    public Vector3 Normal;

    // Size
    private float _size = 2f;
    public float MinSize { get; private set; } = 0.5f;
    public float MaxSize { get; private set; } = 10f;

    public readonly FTBrushPreset PaintPreset;
    public readonly FTBrushPreset ErasePreset;

    // Constructor
    public FTBrush()
    {
        PaintPreset = new FTBrushPreset(color: new Color(0f, 0.75f, 1f, 1f));
        ErasePreset = new FTBrushPreset(color: new Color(1f, 0f, 0f, 1f));
    }

    // Getter | Setter : Size
    public float Size
    {
        get { return _size; }
        set { _size = Mathf.Clamp(value, MinSize, MaxSize); }
    }

    // Getter | Setter : Radius
    public float Radius
    {
        get { return Size / 2; }
    }

    // Getter | Setter : InvertNormal (Raydirection)
    public Vector3 InvertNormal
    {
        get { return Normal * -1; }
    }

    // Getter | Setter : BrushArea
    public float BrushArea
    {
        get { return Mathf.PI * Mathf.Pow(((Size) / 2), 2); }
    }

    // Draw a circle at brush position based on color preset and radius
    public void DrawCircles(FTBrushPreset colorPreset)
    {
        Handles.zTest = CompareFunction.Always;
        Handles.color = colorPreset.Color * new Color(1f, 1f, 1f, 0.25f);
        Handles.DrawSolidDisc(Position, Normal, Radius);
        Handles.color = colorPreset.Color;
        Handles.DrawWireDisc(Position, Normal, Radius, 3f);
    }

    public void DrawSquare(Vector3 position, Vector3 size, FTBrushPreset colorPreset)
    {
        Handles.zTest = CompareFunction.Always;
        Handles.color = colorPreset.Color;

        Vector3 normal = Vector3.up;
        Vector3 right = Vector3.right * (size.x / 2);
        Vector3 forward = Vector3.forward * (size.z / 2);
        Vector3 upperLeft = position - right - forward;
        Vector3 upperRight = position + right - forward;
        Vector3 lowerLeft = position - right + forward;
        Vector3 lowerRight = position + right + forward;

        Handles.DrawAAPolyLine(5, new Vector3[] { upperLeft, upperRight });
        Handles.DrawAAPolyLine(5, new Vector3[] { upperRight, lowerRight });
        Handles.DrawAAPolyLine(5, new Vector3[] { lowerRight, lowerLeft });
        Handles.DrawAAPolyLine(5, new Vector3[] { lowerLeft, upperLeft });

        Handles.DrawSolidRectangleWithOutline(new Vector3[] { upperLeft, upperRight, lowerRight, lowerLeft }, colorPreset.Color * new Color(1f, 1f, 1f, 0.25f), new Color(1f, 1f, 1f, 0f));


    }

    // A sunflower algorythm to draw lines for foliage types spawning
    private Vector3[] SunflowerAlgorythm(float density, float disorder)
    {
        Quaternion rotation = Quaternion.FromToRotation(Vector3.up, Normal);

        int pointNumbers = (int)(BrushArea * density);
        Vector3[] points = new Vector3[pointNumbers];

        float alpha = 2f;
        int b = Mathf.RoundToInt(alpha * Mathf.Sqrt(pointNumbers));
        float phi = (Mathf.Sqrt(5f) + 1f) / 2f;

        for (int i = 0; i < pointNumbers; i++)
        {
            float randomX = Random.Range(-disorder, disorder);
            float randomZ = Random.Range(-disorder, disorder);

            float r = SunFlowerRadius(i, pointNumbers, b);
            float theta = 2f * Mathf.PI * i / Mathf.Pow(phi, 2f);
            Vector3 pointOffset = new Vector3(r * Mathf.Cos(theta) + randomX, 0f, r * Mathf.Sin(theta) + randomZ) * Radius;

            Vector3 point = Position + rotation * pointOffset;
            points[i] = point;
        }

        if (points.Length <= 1)
        {
            points = new Vector3[1];
            points[0] = Position;
        }

        return points;
    }

    private float SunFlowerRadius(int pointIndex, int pointNumbers, int b)
    {
        if (pointIndex > pointNumbers - b)
        {
            return 1f;
        }
        else
        {
            return Mathf.Sqrt(pointIndex - 0.5f) / Mathf.Sqrt(pointNumbers - (b + 0.5f));
        }
    }

    public Vector3[] GetPoints(float density, float disorder)
    {
        return SunflowerAlgorythm(density: density, disorder: disorder);
    }

    public class FTBrushPreset
    {
        public readonly Color Color;

        public FTBrushPreset(Color color)
        {
            Color = color;
        }
    }
}
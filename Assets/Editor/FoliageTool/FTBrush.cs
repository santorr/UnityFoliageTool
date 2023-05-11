using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering;
using UnityEngine.UIElements;

public class FTBrush
{
    public bool Display = false;
    public Vector3 Position;
    public Vector3 Normal;
    public Texture2D Mask;
    
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

    public Bounds Bounds
    {
        get { return new Bounds(Position, new Vector3(Size, 1, Size)); }
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

    public class FTBrushPreset
    {
        public readonly Color Color;

        public FTBrushPreset(Color color)
        {
            Color = color;
        }
    }
}
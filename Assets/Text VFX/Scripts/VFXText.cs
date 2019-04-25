using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Experimental.VFX;


public class VFXText : MonoBehaviour
{
    public CHRFont Font;
    public string[] WordList;
    public float initialDelay = 0;
    public float delay = 1f;
    public bool automatic = true;
    public bool morphable = true;

    private int _currentWordIndex;
    private VisualEffect _vfx;
    
    public struct Shape
    {
        public List<Vector2> StartPoints;
        public List<Vector2> EndPoints;
        public float Width;
    }
    
    // Start is called before the first frame update
    void Start()
    {
        _vfx = gameObject.GetComponent<VisualEffect>();
    }

    void Update()
    {
        if (automatic && !IsInvoking(nameof(DrawNextWord)))
        {
            InvokeRepeating(nameof(DrawNextWord), initialDelay, delay);                            
        }
        else if (!automatic && IsInvoking(nameof(DrawNextWord)))
        {
            CancelInvoke();
        }
    }

    public void DrawNextWord()
    {
        if (_currentWordIndex >= WordList.Length) _currentWordIndex = 0;
        DrawWord(WordList[_currentWordIndex++]);
    }

    private Shape BuildLines(int character)
    {
        // Flatten the character data into pairs of start end points
        // Instead of joined strokes
        var shape = new Shape();
        shape.StartPoints = new List<Vector2>();
        shape.EndPoints = new List<Vector2>();
        if (character < Font.Outlines.Count)
        {
            var letter = Font.Outlines[character];
            shape.Width = Font.Widths[character];
            foreach (var stroke in letter)
            {
                for (var j = 0; j < stroke.Count - 1; j++)
                {
                    shape.StartPoints.Add(stroke[j]);
                    shape.EndPoints.Add(stroke[j + 1]);
                }
            }            
        }
        return shape;
    }

    private Shape ShiftRight(Shape shape, float amount)
    {
        for (var i = 0; i < shape.StartPoints.Count; i++)
        {
            var p = shape.StartPoints[i];
            p.x += amount;
            shape.StartPoints[i] = p;
        }
        for (var i = 0; i < shape.EndPoints.Count; i++)
        {
            var p = shape.EndPoints[i];
            p.x += amount;
            shape.EndPoints[i] = p;
        }
        return shape;
    }

    private Shape ConcatShape(Shape shape1, Shape shape2)
    {
        var shiftedShape2 = ShiftRight(shape2, shape1.Width);
        return new Shape
        {
            StartPoints = shape1.StartPoints.Concat(shiftedShape2.StartPoints).ToList(),
            EndPoints = shape1.EndPoints.Concat(shiftedShape2.EndPoints).ToList(),
            Width = shape1.Width + shape2.Width
        };
    }

    public Shape AlignShapeCenter(Shape shape)
    {
        var MaxX = shape.StartPoints.Concat(shape.EndPoints).Select(p => p.x).Max();
        var amount = MaxX / 2;
        for (var i = 0; i < shape.StartPoints.Count; i++)
        {
            var p = shape.StartPoints[i];
            p.x -= amount;
            shape.StartPoints[i] = p;
        }
        for (var i = 0; i < shape.EndPoints.Count; i++)
        {
            var p = shape.EndPoints[i];
            p.x -= amount;
            shape.EndPoints[i] = p;
        }
        return shape;
        
    }

    private Shape BuildWord(string word)
    {
        var shape = new Shape()
        {
            StartPoints = new List<Vector2>(),
            EndPoints = new List<Vector2>(),
            Width = 0
        };
        foreach (var character in word)
        {
            var newShape = BuildLines(character);
            shape = ConcatShape(shape, newShape);
        }

        return shape;
    }

    private Color[] BuildColorArray(Shape shape)
    {
        // Create a 2px high texture from start and end pairs
        // Row 0 is start points, row 1 is end points
        var pixelData = new Color[shape.StartPoints.Count * 2];        
        for (var i = 0; i < shape.StartPoints.Count; i++)
        {
            pixelData[i] = new Color(shape.StartPoints[i].x, shape.StartPoints[i].y, 0, 1);
            pixelData[i + shape.StartPoints.Count] = new Color(shape.EndPoints[i].x, shape.EndPoints[i].y, 0, 1);
        }
        return pixelData;
    }

    public void DrawWord(string word)
    {
        var shape = BuildWord(word);
        shape = AlignShapeCenter(shape);
        var colorArray = BuildColorArray(shape);
        var texture = new Texture2D(colorArray.Length / 2, 2, TextureFormat.RGBAFloat, false);
        texture.wrapMode = TextureWrapMode.Mirror;
        texture.filterMode = morphable ? FilterMode.Trilinear : FilterMode.Point;
        texture.SetPixels(colorArray);
        texture.Apply();
        _vfx.SetTexture("Positions", texture);
        _vfx.SetInt("Count", texture.width);
    }

}
using System.Drawing;
using System.Numerics;

namespace SGLib;

public struct AtlasMap
{
    public Rectangle Rect => _rect;

    public Point Origin { get; }
    public Point OriginalSize => this.m_origSize;
    public Vector2 ScaleRatio { get; }
    public Vector2 InvScaleRatio { get; }

    public int ID { get; }
    public int SiblingID { get; }

    public bool IsMultiTexture => SiblingID > 0;
    public SiblingTextureType Type { get; }
    public float[] HullPoints { get; }

    public int Width => m_origSize.X;
    public int Height => m_origSize.Y;

    public bool IsMip => (_flags & MipMask) != 0;
    public bool IsBink => (_flags & BinkMask) != 0;
    
    public AtlasMap(Rectangle rect, Point topLeft, Point originalSize, Vector2 scaleRatio)
    {
        _rect = rect;
        Origin = topLeft;
        ScaleRatio = scaleRatio;
        m_origSize = originalSize;
    }

    public AtlasMap(Rectangle rect, Point topLeft, Point originalSize, Vector2 scaleRatio, int id, bool isMultiTexture,
        SiblingTextureType siblingType)
    {
        _rect = rect;
        Origin = topLeft;
        ScaleRatio = scaleRatio;
        InvScaleRatio = Vector2.Divide(Vector2.One, scaleRatio);
        m_origSize = originalSize;
        ID = id;
        SiblingID = (isMultiTexture ? (-1) : 0);
        Type = siblingType;
        _flags = 0;
    }

    public AtlasMap(Rectangle rect, Point topLeft, Point originalSize, Vector2 scaleRatio, int id, bool isMultiTexture,
        bool isMip, bool isBink, SiblingTextureType siblingType)
    {
        _rect = rect;
        Origin = topLeft;
        ScaleRatio = scaleRatio;
        InvScaleRatio = Vector2.Divide(Vector2.One, scaleRatio);
        m_origSize = originalSize;
        ID = id;
        SiblingID = (isMultiTexture ? (-1) : 0);
        Type = siblingType;
        _flags = 0;
        
        if (isMip)
        {
            _flags |= MipMask;
        }

        if (isBink)
        {
            _flags |= BinkMask;
        }
    }
    
    public AtlasMap(Rectangle rect, Point topLeft, Point originalSize, Vector2 scaleRatio, bool isMultiTexture, SiblingTextureType siblingType, List<Point> hull, bool isMip = false, bool isBink = false, bool isAlpha8 = false)
    {
        this._rect = rect;
        this.Origin = topLeft;
        this.ScaleRatio = scaleRatio;
        this.InvScaleRatio = Vector2.Divide(Vector2.One, scaleRatio);
        m_origSize = originalSize;
        this.Type = siblingType;
        this.HullPoints = NormalizeHull(hull, ref this._rect);
        _flags = 0;
        SiblingID = (isMultiTexture ? (-1) : 0);
        
        if (isMip)
        {
            _flags |= MipMask;
        }

        if (isBink)
        {
            _flags |= BinkMask;
        }

        if (isAlpha8)
        {
            _flags |= Alpha8Mask;
        }
    }
    
    private static float[] NormalizeHull(List<Point> hullPoints, ref Rectangle rect)
    {
        if (hullPoints == null)
        {
            return null;
        }
        float[] array = new float[hullPoints.Count * 2];
        int num = -rect.Width / 2;
        int num2 = rect.Width / 2;
        float num3 = (float)(num2 - num);
        int num4 = -rect.Height / 2;
        int num5 = rect.Height / 2;
        float num6 = (float)(num5 - num4);
        int num7 = 0;
        foreach (Point point in hullPoints)
        {
            array[num7++] = (float)(point.X - num) / num3;
            array[num7 - 1] = Math.Clamp(array[num7 - 1], 0f, 1f);
            array[num7++] = (float)(point.Y - num4) / num6;
            array[num7 - 1] = Math.Clamp(array[num7 - 1], 0f, 1f);
        }
        float num8 = CalculatePolygonArea(array);
        if (num8 >= -0.05f || num8 <= -1f)
        {
            return null;
        }
        return array;
    }
    
    private static float CalculatePolygonArea(float[] polygon)
    {
        int num = polygon.Length / 2;
        float num2 = 0f;
        int num3 = num - 1;
        for (int i = 0; i < num; i++)
        {
            num2 += (polygon[num3 * 2] + polygon[i * 2]) * (polygon[num3 * 2 + 1] - polygon[i * 2 + 1]);
            num3 = i;
        }
        return num2 * 0.5f;
    }

    public Rectangle ExpandedSourceRect =>
        new Rectangle(Rect.X - Origin.X, Rect.Y - Origin.Y, OriginalSize.X, OriginalSize.Y);

    public const int INVALID_ID = 0;
    public const int UNRESOLVED_ID = -1;

    public const int MipMask = 1;
    public const int BinkMask = 2;
    public const int Alpha8Mask = 4;

    public Point m_origSize;
    private int _flags;
    private Rectangle _rect;
}
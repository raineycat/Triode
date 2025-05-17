using System.Drawing;
using System.Numerics;

namespace SGLib;

public struct AtlasMap
{
    public Rectangle Rect { get; }
    public Point Origin { get; }
    public Point OriginalSize => this.m_origSize;
    public Vector2 ScaleRatio { get; }
    public Vector2 InvScaleRatio { get; }

    public int ID { get; }
    public int SiblingID { get; }

    public bool IsMultiTexture => SiblingID > 0;
    public SiblingTextureType Type { get; }

    public int Width => m_origSize.X;
    public int Height => m_origSize.Y;

    public bool IsMip => (_flags & MipMask) != 0;
    public bool IsBink => (_flags & BinkMask) != 0;

    public AtlasMap(Rectangle rect, Point topLeft, Point originalSize, Vector2 scaleRatio, int id, bool isMultiTexture,
        SiblingTextureType siblingType)
    {
        Rect = rect;
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
        Rect = rect;
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
            _flags = 1;
        }

        if (isBink)
        {
            _flags |= 2;
        }
    }

    public Rectangle ExpandedSourceRect =>
        new Rectangle(Rect.X - Origin.X, Rect.Y - Origin.Y, OriginalSize.X, OriginalSize.Y);

    public const int INVALID_ID = 0;
    public const int UNRESOLVED_ID = -1;

    public const int MipMask = 1;
    public const int BinkMask = 2;

    public Point m_origSize;
    private int _flags;
}
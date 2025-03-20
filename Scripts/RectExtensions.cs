using UnityEngine;

public static class RectExtensions
{
    public static bool Touches(this Rect rect, Vector2 pos)
    {
        return pos.x >= rect.xMin &&
               pos.x <= rect.xMax &&
               pos.y <= rect.yMax &&
               pos.y <= rect.yMax;
    }

    public static Rect Scaled(this Rect rect, Vector2 scale)
    {
        float width = rect.width * scale.x;
        float height = rect.height * scale.y;
        return new Rect(rect.x - (width-rect.width) / 2f, rect.y - (height-rect.height) / 2f, width, height);
    }
}

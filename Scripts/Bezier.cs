using UnityEngine;
using System.Collections;

public class Bezier {

    public static Vector3 GetPoint(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
    {
        float omt = 1f - t;
        float omt2 = omt * omt;
        float t2 = t * t;

        return p0 * (omt2 * omt) +
               p1 * (3f * omt2 * t) +
               p2 * (3f * omt * t2) +
               p3 * (t2 * t);
    }

    public static Vector3 GetTangent(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
    {
        float omt = 1f - t;
        float omt2 = omt * omt;
        float t2 = t * t;
        Vector3 tangent = p0 * (-omt2) +
                          p1 * (3f * omt2 - 2f * omt) +
                          p2 * (-3f * t2 + 2f * t) +
                          p3 * (t2);
        return tangent.normalized;
    }

    public static Vector3 GetNormal2D(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
    {
        Vector3 tangent = GetTangent(p0,p1,p2,p3,t);
        return new Vector3(-tangent.y, tangent.x, 0);
    }

    public static Vector3 GetNormal(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t, Vector3 up)
    {
        Vector3 tangent = GetTangent(p0, p1, p2, p3, t);
        Vector3 binormal = Vector3.Cross(up, tangent).normalized;
        return Vector3.Cross(tangent, binormal);
    }

    public static Quaternion GetOrientation2D(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
    {
        Vector3 tangent = GetTangent(p0, p1, p2, p3, t);
        Vector3 normal = GetNormal(p0, p1, p2, p3, t,Vector3.up);
        return Quaternion.LookRotation(tangent, normal);
    }

    public static Quaternion GetOrientation(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t, Vector3 up)
    {
        Vector3 tangent = GetTangent(p0, p1, p2, p3, t);
        Vector3 normal = GetNormal(p0, p1, p2, p3, t, up);
        return Quaternion.LookRotation(tangent, normal);
    }

    public static OrientedPoint GetOrientedPoint(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t, Vector3 up)
    {
        return new OrientedPoint(GetPoint(p0, p1, p2, p3, t), GetOrientation(p0, p1, p2, p3, t, up));
    }
}

using UnityEngine;
using System.Collections;

[System.Serializable]
public struct SplinePosition {

    public enum Modes
    {
        Normalized,
        Distance,
        Simple
    }

    public Modes Mode;
    public float Position;

    public SplinePosition(float pos, Modes mode = Modes.Normalized)
    {
        this.Position = pos;
        this.Mode = mode;
    }

    public float GetSplinePosition(Spline spline)
    {
        float t = this.Position;

        switch (this.Mode)
        {
            case Modes.Normalized:
                t = spline.GetPosition(t);
                break;
            case Modes.Distance:
                if(t < 0) t = -(1f-spline.GetPositionFromLength(t));
                else t = spline.GetPositionFromLength(t);
                break;
            case Modes.Simple:
                t %= 1f;
                if (t < 0) t = 1f - t;
                break;
        }

        return t;
    }

    public float GetLength(Spline spline)
    {
        return spline.GetLength(this.GetSplinePosition(spline));
    }

    public static implicit operator SplinePosition(float value)
    {
        return new SplinePosition(value);
    }
}

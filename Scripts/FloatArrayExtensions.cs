using UnityEngine;

public static class FloatArrayExtensions
{
    public static float Sample(this float[] fArr, float t)
    {
        int count = fArr.Length;
        if (count == 0)
        {
            Debug.LogError("Unable to sample array - it has no elements.");
            return 0;
        }

        if (count == 1) return fArr[0];

        float f = t * (count - 1);
        int idLower = Mathf.FloorToInt(f);
        int idUpper = Mathf.FloorToInt(f + 1);

        if (idUpper >= count) return fArr[count - 1];
        if (idLower < 0) return fArr[0];

        return Mathf.Lerp(fArr[idLower], fArr[idUpper], f - idLower);
    }

    public static float ReverseSample(this float[] fArr, float value)
    {
        int count = fArr.Length;
        if (count == 0)
        {
            Debug.LogError("Unable to sample array - it has no elements.");
            return 0;
        }

        int idUpper = System.Array.BinarySearch(fArr, value);
        if (idUpper < 0) idUpper = ~idUpper;
        int idLower = idUpper - 1;
        int clampedIdLower = Mathf.Max(0, idLower);
        float lower = fArr[clampedIdLower];
        float span = (fArr[idUpper] - lower);
        if (span == 0) return clampedIdLower;
        float share = (value - lower);
        float lerp = share / span;
        float step = 1f / (count-1f);
        return step * clampedIdLower + step * lerp;

    }
}
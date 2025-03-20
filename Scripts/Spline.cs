using UnityEngine;
using System.Collections;
using System;
using System.Collections.Generic;
using System.Linq;

[AddComponentMenu("Spline/Spline")]
public class Spline : MonoBehaviour {

    public enum ControlPointMode
    {
        Free,
        Aligned,
        Mirrored
    }

    public delegate void SplineUpdateDelegate();

    public bool ShowNormals = false;
    public bool ShowDirections = false;
    public bool ShowLengthSamples = false;
    public bool ShowDefaultHandles = true;
    public Color Color = Color.white;

    public SplineUpdateDelegate OnSplineUpdate = delegate { };

    [SerializeField]
    private OrientedPoint[] controlPoints;
    [SerializeField]
    private Vector3[] tangentPoints;
    [SerializeField]
    private ControlPointMode[] modes;
    [SerializeField]
    private bool loop;
    [SerializeField]
    private float[] sampledLengths;
    [SerializeField]
    private int lengthSamples = 100;

    public bool Loop
    {
        get
        {
            return this.loop;
        }
        set
        {
            if (value == this.loop) return;
            loop = value;
            if (value == true) EnforceLoop();
        }
    }

    public int LengthSamples
    {
        get { return this.lengthSamples; }
        set
        {
            this.lengthSamples = value;
            GenerateLengthSamples();
        }
    }

    public float Length
    {
        get { return this.sampledLengths.Length <= 0 ? 0 : this.sampledLengths.Last(); }
    }

    public int ControlPointCount
    {
        get
        {
            return this.controlPoints.Length <= 0 ? 0 : this.controlPoints.Length;
        }
    }

    public int TangentPointCount
    {
        get
        {
            return this.tangentPoints.Length;
        }
    }

    public int SegmentCount
    {
        get
        {
            return this.controlPoints.Length <= 0 ? 0 : this.controlPoints.Length - 1;
        }
    }

    public void Reset()
    {
        this.controlPoints = new OrientedPoint[]
        {
            new OrientedPoint(new Vector3(0f,0f,0f), Quaternion.identity),
            new OrientedPoint(new Vector3(0f,0f,3f), Quaternion.identity)
        };

        modes = new ControlPointMode[] {
            ControlPointMode.Free,
            ControlPointMode.Free
        };

        this.tangentPoints = new Vector3[] {
            new Vector3(0f, 0f, 1f),
            new Vector3(0f, 0f, 2f)
        };

        GenerateLengthSamples();
        this.OnSplineUpdate.Invoke();
    }

    public enum InsertPositions
    {
        Before = 0,
        After = 1
    }

    public int AddControlPointAt(int index, InsertPositions insertPosition = InsertPositions.After)
    {
        if (index >= this.ControlPointCount || index < 0) return -1;

        int insertIndex = index + (int)insertPosition;

        if (this.loop)
        {
            if (insertIndex == this.ControlPointCount) insertIndex = 1;
            if (insertIndex == 0) insertIndex = this.ControlPointCount -1;
        }

        OrientedPoint referencePoint;
        Vector3 tangentPoint1;
        Vector3 tangentPoint2;
        int insertTangentIndex;

        if (insertIndex >= this.ControlPointCount)
        {
            int lastIndex = this.ControlPointCount - 1;
            float distance = 3f;
            if(lastIndex-1 >= 0) distance = Vector3.Distance(controlPoints[lastIndex].Position, controlPoints[lastIndex-1].Position);
            Vector3 pos = controlPoints[lastIndex].Position + controlPoints[lastIndex].Forward * distance;
            referencePoint = new OrientedPoint(pos, Quaternion.LookRotation(pos - controlPoints[lastIndex].Position, controlPoints[lastIndex].Up));
            tangentPoint1 = referencePoint.Position + referencePoint.Forward * -(distance/3f*2f);
            tangentPoint2 = referencePoint.Position + referencePoint.Forward * -(distance/3f);
            insertIndex = lastIndex+1;
            insertTangentIndex = insertIndex * 2 - 2;
        }
        else if(insertIndex <= 0)
        {
            float distance = 3f;
            if(controlPoints.Length > 1) distance = Vector3.Distance(controlPoints[0].Position, controlPoints[1].Position);
            Vector3 pos = controlPoints[0].Position - controlPoints[0].Forward * distance;
            referencePoint = new OrientedPoint(pos, Quaternion.LookRotation(controlPoints[0].Position - pos, controlPoints[0].Up));
            tangentPoint1 = referencePoint.Position + referencePoint.Forward * (distance/3f*2f);
            tangentPoint2 = referencePoint.Position + referencePoint.Forward * (distance/3f);
            insertIndex = 0;
            insertTangentIndex = 0;
        }
        else
        {
            int prevIndex = insertIndex - 1;

            float prevLength = GetLength(GetControlPointPosition(prevIndex));
            float nextLength = GetLength(GetControlPointPosition(insertIndex));
            float length = Mathf.Lerp(nextLength, prevLength, 0.5f);
            float pos = GetPositionFromLength(length);
            referencePoint = GetLocalOrientedPoint(pos);
            tangentPoint1 = referencePoint.Position + referencePoint.Forward * -1f;
            tangentPoint2 = referencePoint.Position + referencePoint.Forward * 1f;

            insertTangentIndex = insertIndex * 2 - 1;
        }

        List<ControlPointMode> newModes = new List<ControlPointMode>(this.modes);
        List<OrientedPoint> newControlPoints = new List<OrientedPoint>(this.controlPoints);
        List<Vector3> newTangentPoints = new List<Vector3>(this.tangentPoints);

        newModes.Insert(insertIndex, this.modes[index]);
        newControlPoints.Insert(insertIndex, referencePoint);
        newTangentPoints.InsertRange(insertTangentIndex, new Vector3[] { tangentPoint1, tangentPoint2 });

        this.controlPoints = newControlPoints.ToArray();
        this.modes = newModes.ToArray();
        this.tangentPoints = newTangentPoints.ToArray();

        EnforceModeTangent(insertIndex);

        GenerateLengthSamples();
        this.OnSplineUpdate.Invoke();

        return insertIndex;
    }

    public void Remove(int index)
    {
        if (index >= this.ControlPointCount || index < 0) return;

        int removeTangentCount = 2;
        int removeTangentIndex = index * 2 - 1;
        if(removeTangentIndex < 0)
        {
            removeTangentIndex = 0;
            removeTangentCount = 2;
        }
        else if(removeTangentIndex >= this.TangentPointCount -1)
        {
            removeTangentIndex = this.TangentPointCount - 2;
            if (this.loop) removeTangentIndex -= 1;
            removeTangentCount = 2;
        }

        List<ControlPointMode> newModes = new List<ControlPointMode>(this.modes);
        List<OrientedPoint> newControlPoints = new List<OrientedPoint>(this.controlPoints);
        List<Vector3> newTangentPoints = new List<Vector3>(this.tangentPoints);

        newControlPoints.RemoveAt(index);
        newModes.RemoveAt(index);
        newTangentPoints.RemoveRange(removeTangentIndex, removeTangentCount);

        this.controlPoints = newControlPoints.ToArray();
        this.modes = newModes.ToArray();
        this.tangentPoints = newTangentPoints.ToArray();

        EnforceLoop();

        GenerateLengthSamples();
        this.OnSplineUpdate.Invoke();
    }

    public OrientedPoint GetControlPoint(int index)
    {
        return this.controlPoints[index];
    }

    public void SetControlPoint(int index, OrientedPoint point)
    {
        Vector3 delta = point.Position - this.controlPoints[index].Position;
        Quaternion rotationDelta = Quaternion.Inverse(this.controlPoints[index].Rotation) * point.Rotation;
        //Move Tangent Points
        int prevTangent = GetPreviousTangent(index);
        int nextTangent = GetNextTangent(index);

        if (prevTangent > 0)
        {
            Vector3 local = this.controlPoints[index].WorldToLocal(this.tangentPoints[prevTangent]);
            this.tangentPoints[prevTangent] = point.LocalToWorld(local);
        }
        if (nextTangent < this.tangentPoints.Length - 1)
        {
            Vector3 local = this.controlPoints[index].WorldToLocal(this.tangentPoints[nextTangent]);
            this.tangentPoints[nextTangent] = point.LocalToWorld(local);
        }

        this.controlPoints[index] = point;

        if(this.loop)
        {
            if (index == 0) this.controlPoints[this.ControlPointCount - 1] = this.controlPoints[index];
            else if (index == this.ControlPointCount - 1) this.controlPoints[0] = this.controlPoints[index];
        }

        EnforceMode(index);
        GenerateLengthSamples();

        this.OnSplineUpdate.Invoke();
    }

    public void SnapControlPointToGround(int index)
    {
        var point = GetControlPoint(index);
        RaycastHit hit;
        if(Physics.Raycast(this.transform.TransformPoint(point.Position+Vector3.up), -Vector3.up, out hit, Mathf.Infinity, Physics.AllLayers))
        {
            SetControlPoint(index, new OrientedPoint(this.transform.InverseTransformPoint(hit.point), point.Rotation));
        }
    }

    public void AlignControlPointNormalWithUp(int index)
    {
        var point = GetControlPoint(index);
        SetControlPoint(index, new OrientedPoint(point.Position, Quaternion.LookRotation(point.Forward,Vector3.up)));
    }

    public Vector3 GetTangentPoint(int index)
    {
        return tangentPoints[index];
    }


    public void SetTangentPoint(int index, Vector3 point)
    {
        tangentPoints[index] = point;
        EnforceModeTangent(index);
        GenerateLengthSamples();

       this.OnSplineUpdate.Invoke();
    }

    public ControlPointMode GetControlPointMode(int index)
    {
        return modes[index];
    }

    public void SetControlPointMode(int index, ControlPointMode mode)
    {
        modes[index] = mode;
        if (loop)
        {
            if (index == 0) modes[modes.Length - 1] = mode;
            else if (index == modes.Length - 1) modes[0] = mode;
        }
        EnforceMode(index);
        GenerateLengthSamples();

        this.OnSplineUpdate.Invoke();
    }

    public void SetTangentPointMode(int index, ControlPointMode mode)
    {
        int cpIndex = GetControlPointIndex(index);
        modes[cpIndex] = mode;
        if (loop)
        {
            if (cpIndex == 0) modes[modes.Length - 1] = mode;
            else if (cpIndex == modes.Length - 1) modes[0] = mode;
        }
        EnforceModeTangent(index);
        GenerateLengthSamples();

        this.OnSplineUpdate.Invoke();
    }

    public void SplineUpdate()
    {
        if (this.loop) EnforceLoop();
        this.OnSplineUpdate.Invoke();
    }

    public float GetLength(float t)
    {
        if(t < 0)
        {
            if (!this.loop) return 0f;
            var l = this.sampledLengths.Sample(1f + t);
            return l - this.Length;
        }
        return this.sampledLengths.Sample(t);
    }

    public float GetPositionFromLength(float length)
    {
        if(this.sampledLengths.Length <= 0) return 0f;
        if (this.loop) length = length >= 0 ? length % this.Length : this.Length + length;
        else length = Mathf.Clamp(length, 0f, this.Length);
        return this.sampledLengths.ReverseSample(length);
    }

    public float GetPosition(float normalizedPosition)
    {
        return GetPositionFromLength(normalizedPosition * this.Length);
    }

    public float GetNormalizedPosition(float t)
    {
        return GetLength(t) / this.Length;
    }

    public float GetNormalizedPositionFromlength(float length)
    {
        return length / this.Length;
    }

    public Vector3 GetPoint(float t)
    {
        int i = 0;
        t = GetSegmentPosition(t, out i);
        return transform.TransformPoint(Bezier.GetPoint(
            this.controlPoints[i].Position, this.tangentPoints[i*2], this.tangentPoints[i*2+1], this.controlPoints[i + 1].Position, t));
    }

    public Vector3 GetTangent(float t)
    {
        int i = 0;
        t = GetSegmentPosition(t, out i);
        return transform.TransformPoint(Bezier.GetTangent(
            this.controlPoints[i].Position, this.tangentPoints[i * 2], this.tangentPoints[i * 2 + 1], this.controlPoints[i + 1].Position, t)) - transform.position;
    }

    public Vector3 GetNormal(float t)
    {
        int i = 0;
        t = GetSegmentPosition(t, out i);
        Quaternion rotation = Quaternion.Slerp(this.controlPoints[i].Rotation, this.controlPoints[i + 1].Rotation, t);
        return transform.TransformPoint(Bezier.GetNormal(
            this.controlPoints[i].Position, this.tangentPoints[i * 2], this.tangentPoints[i * 2 + 1], this.controlPoints[i + 1].Position, t, rotation * Vector3.up)) - transform.position;
    }

    public Quaternion GetOrientation(float t)
    {
        int i = 0;
        t = GetSegmentPosition(t, out i);
        Quaternion rotation = Quaternion.Slerp(this.controlPoints[i].Rotation, this.controlPoints[i + 1].Rotation, t);
        return Bezier.GetOrientation(this.controlPoints[i].Position, this.tangentPoints[i * 2], this.tangentPoints[i * 2 + 1], this.controlPoints[i + 1].Position, t, rotation * Vector3.up);
    }

    public OrientedPoint GetOrientedPoint(float t)
    {
        OrientedPoint point = GetLocalOrientedPoint(t);
        point.Position = this.transform.TransformPoint(point.Position);
        point.Rotation = this.transform.rotation * point.Rotation;
        return point;
    }

    public OrientedPoint GetLocalOrientedPoint(float t)
    {
        int i = 0;
        t = GetSegmentPosition(t, out i);
        Quaternion rotation = Quaternion.Slerp(this.controlPoints[i].Rotation, this.controlPoints[i + 1].Rotation, t);
        return Bezier.GetOrientedPoint(this.controlPoints[i].Position, this.tangentPoints[i * 2], this.tangentPoints[i * 2 + 1], this.controlPoints[i + 1].Position, t, rotation * Vector3.up);
    }

    public float GetNearestPoint(Vector3 point, int iterations = 10)
    {
        float min = float.MaxValue;
        float nearest = 0f;
        int steps = iterations * this.SegmentCount;
        for (int i = 1; i <= steps; i++)
        {
            float t = (float)i/(float)steps;
            float pos = GetPosition(t);
            Vector3 p1 = GetPoint(pos);
            float distance = Vector3.Distance(p1, point);
            if(distance < min)
            {
                min = distance;
                nearest = t;
            }
        }

        //Device and Conquer

        float distance0 = 0f;
        float distance1 = 0f;

        float t0 = nearest - 1f / (float)steps;
        float t1 = nearest + 1f / (float)steps;

        for (int i = 0; i < iterations; ++i)
        {
            float m = (t1 - t0) / 2f;

            var p0 = GetPoint(GetPosition(t0));
            var p1 = GetPoint(GetPosition(t1));

            distance0 = Vector3.Distance(p0, point);
            distance1 = Vector3.Distance(p1, point);
            //Debug.DrawLine(p0, p0+Vector3.up*0.5f, Color.green);

            if (distance0 < distance1) t1 -= m;
            else t0 += m;
        }

        nearest = (distance0 < distance1) ? t0 : t1;

        return GetPosition(nearest);
    }

    public float GetNearestPointToScreenPosition(Vector2 screenPosition, int iterations = 10)
    {
        float min = float.MaxValue;
        float nearest = 0f;
        int steps = iterations * this.SegmentCount;
        for (int i = 1; i <= steps; i++)
        {
            float t = (float)i / (float)steps;
            float pos = GetPosition(t);
            Vector3 p1 = GetPoint(pos);
            //Gizmos.color = Color.red;
            //Gizmos.DrawLine(p1, p1 + Vector3.up * 0.5f);
            Vector3 screenP1 = Camera.current.WorldToScreenPoint(p1);
            float distance = Vector2.Distance(screenP1, screenPosition);
            if (distance < min)
            {
                min = distance;
                nearest = t;
            }
        }

        //Device and Conquer

        float distance0 = 0f;
        float distance1 = 0f;

        float t0 = nearest - 1f / (float)steps;
        float t1 = nearest + 1f / (float)steps;

        for (int i = 0; i < iterations; ++i)
        {
            float m = (t1 - t0) / 2f;

            var p0 = GetPoint(GetPosition(t0));
            var p1 = (GetPoint(GetPosition(t1)));

            var p0Screen = Camera.current.WorldToScreenPoint(p0);
            var p1Screen = Camera.current.WorldToScreenPoint(p1);

            distance0 = Vector2.Distance(p0Screen, screenPosition);
            distance1 = Vector2.Distance(p1Screen, screenPosition);
            //Debug.DrawLine(p0, p0+Vector3.up*0.5f, Color.green);
            //Gizmos.color = Color.green;
            //Gizmos.DrawLine(p0, p0 + Vector3.up * 0.5f);

            if (distance0 < distance1) t1 -= m;
            else t0 += m;
        }

        nearest = (distance0 < distance1) ? t0 : t1;

        return GetPosition(nearest);
    }

    public int GetControlPointIndex(int tangentIndex)
    {
        return (tangentIndex + 1) / 2;
    }

    public int GetPreviousTangent(int controlPointIndex)
    {
        int index = controlPointIndex * 2 - 1;
        if (index < 0)
        {
            if (this.loop) index = this.TangentPointCount - 1;
            else index = -1;
        }
        return index;
    }

    public int GetNextTangent(int controlPointIndex)
    {
        int index = controlPointIndex * 2;
        if (this.loop && index >= this.TangentPointCount) index = 0;
        return index;
    }

    public void GenerateLengthSamples()
    {
        Vector3 prevPoint = this.GetPoint(0f);
        Vector3 pt;
        float total = 0;

        int sampleCount = this.LengthSamples * this.SegmentCount;
        List<float> samples = new List<float>(sampleCount) { 0 };
        float step = 1.0f / ((float)sampleCount);
        for (float t = step; t < 1.0f; t += step)
        {
            pt = GetPoint(t);
            total += (pt - prevPoint).magnitude;
            samples.Add(total);
            prevPoint = pt;
        }

        //pt = GetPoint(1f);
        //samples.Add(total + (pt - prevPoint).magnitude);

        this.sampledLengths = samples.ToArray();
    }

    private float GetSegmentPosition(float t, out int segment)
    {
        if (t >= 1f)
        {
            if (this.loop) t %= 1f;
            else
            {
                segment = this.controlPoints.Length - 2;
                return 1f;
            }
        }

        t = Mathf.Clamp01(t) * this.SegmentCount;
        segment = (int)t;
        t -= segment;
        return t;
    }

    public float GetControlPointPosition(int index)
    {
        return (float)index / (float)this.SegmentCount;
    }

    private void EnforceMode(int index)
    {
        EnforceModeTangent(GetPreviousTangent(index));
    }

    private void EnforceModeTangent(int index)
    {
        if (index < 0 || index > this.TangentPointCount-1) return;
        int cpIndex = GetControlPointIndex(index);
        var cp = this.GetControlPoint(cpIndex);
        int previousTangent = GetPreviousTangent(cpIndex);
        int nextTangent = GetNextTangent(cpIndex);

        //Rotate controlpoint
        if(nextTangent < this.tangentPoints.Length) cp.Rotation = Quaternion.LookRotation(this.tangentPoints[nextTangent] - cp.Position, cp.Up);
        else cp.Rotation = Quaternion.LookRotation(cp.Position - this.tangentPoints[previousTangent], cp.Up);
        this.controlPoints[cpIndex] = cp;

        ControlPointMode mode = modes[cpIndex];
        if (mode == ControlPointMode.Free || !loop && (index == 0 || index == this.tangentPoints.Length - 1)) return;


        int fixedIndex, enforcedIndex;
        if (index == previousTangent)
        {
            fixedIndex = previousTangent;
            if (fixedIndex < 0) fixedIndex = this.tangentPoints.Length - 1;
            enforcedIndex = nextTangent;
            if (enforcedIndex >= this.tangentPoints.Length) enforcedIndex = 0;
        }
        else
        {
            fixedIndex = nextTangent;
            if (fixedIndex >= this.tangentPoints.Length) fixedIndex = 0;
            enforcedIndex = previousTangent;
            if (enforcedIndex < 0) enforcedIndex = this.tangentPoints.Length - 1;
        }

        Vector3 middle = this.controlPoints[GetControlPointIndex(index)].Position;
        Vector3 enforcedTangent = middle - tangentPoints[fixedIndex];
        if (mode == ControlPointMode.Aligned) enforcedTangent = enforcedTangent.normalized * Vector3.Distance(middle, tangentPoints[enforcedIndex]);
        tangentPoints[enforcedIndex] = middle + enforcedTangent;
    }

    private void EnforceLoop()
    {
        modes[modes.Length - 1] = modes[0];
        SetControlPoint(0, controlPoints[0]);
    }
}

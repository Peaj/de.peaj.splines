using UnityEngine;
using System.Collections;

[AddComponentMenu("Spline/Position On Spline")]
[ExecuteInEditMode]
public class PositionOnSpline : MonoBehaviour {

    public enum Modes
    {
        Normalized,
        Distance,
        Simple
    }

    public enum UpdateMethods
    {
        Update,
        LateUpdate
    }

    public Spline Spline;
    public SplinePosition Position;
    public Vector2 Offset = Vector2.zero;
    public bool FaceDirection = true;

    public UpdateMethods UpdateMethod;

    public float SplinePosition
    {
        get { return this.Position.GetSplinePosition(this.Spline); }
    }

    public void Update()
    {
        if(this.UpdateMethod == UpdateMethods.Update) UpdatePosition();
    }

    public void LateUpdate()
    {
        if(this.UpdateMethod == UpdateMethods.LateUpdate) UpdatePosition();
    }

    private void UpdatePosition()
    {
        if (!this.Spline) return;

        float t = this.Position.GetSplinePosition(this.Spline);

        var point = this.Spline.GetLocalOrientedPoint(t);
        this.transform.position = this.Spline.transform.TransformPoint(point.LocalToWorld(this.Offset));
        if(this.FaceDirection) this.transform.rotation = this.Spline.transform.rotation * point.Rotation;
    }
}

using UnityEngine;
using System.Collections;

[AddComponentMenu("Spline/Animate On Spline")]
[RequireComponent(typeof(PositionOnSpline))]
public class AnimateOnSpline : MonoBehaviour {

    public float Speed = 1f;
    public AnimationCurve Curve;

    private float start = 0f;
    private PositionOnSpline positionOnSpline;

    void OnEnable()
    {
        this.positionOnSpline = this.GetComponent<PositionOnSpline>();
        this.start = this.positionOnSpline.SplinePosition;
    }

    void Update()
    {
        this.positionOnSpline.Position = this.start + this.Curve.Evaluate(Time.time * this.Speed);
    }
}

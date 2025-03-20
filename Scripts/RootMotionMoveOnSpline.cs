using UnityEngine;
using System.Collections;

[ExecuteInEditMode]
[AddComponentMenu("Spline/Animate On Spline")]
[RequireComponent(typeof(PositionOnSpline))]
[RequireComponent(typeof(Animator))]
public class RootMotionMoveOnSpline : MonoBehaviour {

    private PositionOnSpline positionOnSpline;
    private Animator animator;

    void Initialize()
    {
        if(this.positionOnSpline == null) this.positionOnSpline = this.GetComponent<PositionOnSpline>();
        if(this.animator == null) this.animator = this.GetComponent<Animator>();
    }

    void OnAnimatorMove()
    {
        Initialize();
        Debug.Log("Delta: "+this.animator.deltaPosition.magnitude);
        this.positionOnSpline.Position.Position += this.animator.deltaPosition.magnitude;
    }
}

using UnityEngine;
using System.Collections;

[ExecuteInEditMode]
public class NearestPointOnSpline : MonoBehaviour {

    public Spline Spline;

	void Update () {

        var pos = this.Spline.GetNearestPoint(this.transform.position);
        Debug.DrawLine(this.Spline.GetPoint(pos), this.transform.position, Color.blue);
    }
}

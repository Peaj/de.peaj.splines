using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Rigidbody))]
public class SplineJoint : MonoBehaviour {

    public Spline Target;

    public float SplinePosition
    {
        get { return this.splinePosition; }
    }

    private new Rigidbody rigidbody;
    private Vector3 lastVelocity;
    private float splinePosition;

    void Start()
    {
        rigidbody = this.GetComponent<Rigidbody>();
    }

    void FixedUpdate()
    {
        this.splinePosition = this.Target.GetNearestPoint(this.transform.position);
        var point = this.Target.GetOrientedPoint(this.splinePosition);
        Vector3 velocity = point.WorldToLocalDirection(this.rigidbody.velocity);
        Vector3 acc = (velocity - this.lastVelocity) / Time.deltaTime;
        Vector3 force = this.rigidbody.mass * acc;

        Vector3 forwardForce = force;
        forwardForce.y = 0f;
        forwardForce.x = 0f;
        Vector3 forwardAcc = forwardForce / this.rigidbody.mass;

        Vector3 forwardVelocity = this.lastVelocity + forwardAcc * Time.deltaTime;

        this.rigidbody.velocity = point.LocalToWorldDirection(forwardVelocity);

        this.lastVelocity = forwardVelocity;

        this.transform.rotation = point.Rotation;

        if (acc.magnitude > 0f) this.rigidbody.MovePosition(point.Position);
    }

}


using UnityEngine;
using System.Collections;

public class Accelerator : MonoBehaviour {

    public float TargetSpeed = 1f;
    public float MaxForce = 1000f;

    void OnTriggerStay(Collider other)
    {
        var rigidbody = other.GetComponent<Collider>().attachedRigidbody;
        if (!rigidbody) return;
        float speed = rigidbody.velocity.magnitude;

        if (speed < this.TargetSpeed)
        {
            rigidbody.AddRelativeForce(Vector3.forward * this.MaxForce, ForceMode.Force);
        }
        else if (speed > this.TargetSpeed)
        {
            rigidbody.AddRelativeForce(-Vector3.forward * this.MaxForce, ForceMode.Force);
        }
    }
}

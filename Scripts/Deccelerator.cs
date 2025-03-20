using UnityEngine;
using System.Collections;

public class Deccelerator : MonoBehaviour {

    public float TargetSpeed = 1f;
    public float Factor = 1f;

    void OnTriggerStay(Collider other)
    {
        var rigidbody = other.GetComponent<Collider>().attachedRigidbody;
        if (!rigidbody) return;
        float speed = rigidbody.velocity.magnitude;

        Vector3 force = -(rigidbody.mass * rigidbody.transform.forward * (rigidbody.velocity.magnitude - this.TargetSpeed)) * this.Factor;
        rigidbody.AddForce(force, ForceMode.Force);
    }
}

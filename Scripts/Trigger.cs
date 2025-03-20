using UnityEngine;
using System.Collections;
using UnityEngine.Events;

[RequireComponent(typeof(Collider))]
public class Trigger : MonoBehaviour {

    public UnityEvent TriggerEnter; 
    public UnityEvent TriggerStay; 
    public UnityEvent TriggerLeave;

    void OnTriggerEnter(Collider other)
    {
        this.TriggerEnter.Invoke();
    }

    void OnTriggerStay(Collider other)
    {
        this.TriggerStay.Invoke();
    }

    void OnTriggerExit(Collider other)
    {
        this.TriggerLeave.Invoke();
    }
}

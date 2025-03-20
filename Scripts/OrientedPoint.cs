using UnityEngine;
using System.Collections;

[System.Serializable]
public struct OrientedPoint {

    public Vector3 Position;
    public Quaternion Rotation;

    public Vector3 Forward { get { return this.Rotation * Vector3.forward; } }
    public Vector3 Up { get { return this.Rotation * Vector3.up; } }

    public OrientedPoint(Vector3 position, Quaternion rotation)
    {
        this.Position = position;
        this.Rotation = rotation;
    }

    public Vector3 LocalToWorld(Vector3 point)
    {
        return this.Position + this.Rotation * point;
    }

    public Vector3 WorldToLocal(Vector3 point)
    {
        return Quaternion.Inverse(this.Rotation) * (point - this.Position);
    }

    public Vector3 LocalToWorldDirection(Vector3 point)
    {
        return this.Rotation * point;
    }

    public Vector3 WorldToLocalDirection(Vector3 direction)
    {
        return Quaternion.Inverse(this.Rotation) * direction;
    }
}

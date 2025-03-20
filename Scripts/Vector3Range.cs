using UnityEngine;
using System.Collections;

[System.Serializable]
public class Vector3Range {

	public enum RangeTypes
    {
        Constant,
        Curve,
        RandomBetweenConstants
    }

    public RangeTypes RangeType;

    [SerializeField]
    private Vector3 constant1;
    [SerializeField]
    private Vector3 constant2;
    [SerializeField]
    private AnimationCurve curveX;
    [SerializeField]
    private AnimationCurve curveY;
    [SerializeField]
    private AnimationCurve curveZ;

    public Vector3 Evaluate(float time)
    {
        switch(this.RangeType)
        {
            case RangeTypes.Curve:
                return new Vector3(
                    this.curveX.Evaluate(time),
                    this.curveY.Evaluate(time),
                    this.curveZ.Evaluate(time)
                    );
            case RangeTypes.RandomBetweenConstants:
                return new Vector3(
                    Random.Range(this.constant1.x,this.constant2.x),
                    Random.Range(this.constant1.y,this.constant2.y),
                    Random.Range(this.constant1.z,this.constant2.z)
                    );
            default:
                return constant1;
        }
    }

    public static implicit operator Vector3Range(Vector3 vector)
    {
        var range = new Vector3Range();
        range.constant1 = vector;
        return range;
    }
}

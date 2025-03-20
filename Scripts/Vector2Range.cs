using UnityEngine;
using System.Collections;

[System.Serializable]
public class Vector2Range {

	public enum RangeTypes
    {
        Vector,
        VectorCurve,
        RandomBetweenVectors,
        Scalar,
        Curve,
        RandomBetweenScalars
    }

    public RangeTypes RangeType;
    
    [SerializeField]
    private Vector2 constant1;
    [SerializeField]
    private Vector2 constant2;
    [SerializeField]
    private AnimationCurve curveX;
    [SerializeField]
    private AnimationCurve curveY;

    public Vector2 Evaluate(float time)
    {
        switch(this.RangeType)
        {
            case RangeTypes.VectorCurve:
                return new Vector2(
                    this.curveX.Evaluate(time),
                    this.curveY.Evaluate(time)
                    );
            case RangeTypes.RandomBetweenVectors:
                return new Vector2(
                    Random.Range(this.constant1.x,this.constant2.x),
                    Random.Range(this.constant1.y,this.constant2.y)
                    );
            case RangeTypes.Scalar:
                return new Vector2(this.constant1.x, this.constant1.x);
            case RangeTypes.Curve:
                float curveValue = this.curveX.Evaluate(time);
                return new Vector2(curveValue, curveValue);
            case RangeTypes.RandomBetweenScalars:
                float value = Random.Range(this.constant1.x, this.constant2.x);
                return new Vector2(
                    value,
                    value
                    );
            default:
                return constant1;
        }
    }

    public static implicit operator Vector2Range(Vector2 vector)
    {
        var range = new Vector2Range();
        range.constant1 = vector;
        return range;
    }
}

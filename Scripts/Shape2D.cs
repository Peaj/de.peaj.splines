using UnityEngine;
using System.Collections;

[CreateAssetMenu]
public class Shape2D : ScriptableObject {

    public Vertex2D[] Vertices;
    public int[] Lines;

    public Rect Bounds
    {
        get
        {
            UnityEngine.Profiling.Profiler.BeginSample("GetBounds");
            float xMin = float.MaxValue;
            float yMin = float.MaxValue;
            float xMax = float.MinValue;
            float yMax = float.MinValue;

            for(int i=0; i< this.Vertices.Length; ++i)
            {
                if (this.Vertices[i].Position.x < xMin) xMin = this.Vertices[i].Position.x;
                if (this.Vertices[i].Position.x > xMax) xMax = this.Vertices[i].Position.x;

                if (this.Vertices[i].Position.y < yMin) yMin = this.Vertices[i].Position.y;
                if (this.Vertices[i].Position.y > yMax) yMax = this.Vertices[i].Position.y;
            }
            var rect = new Rect(xMin, yMin, xMax - xMin, yMax - yMin);
            UnityEngine.Profiling.Profiler.EndSample();
            return rect;
        }
    }
    
    public float GetUSpan()
    {
        float length = 0;
        for(int i=0; i<this.Lines.Length; i+=2)
        {
            length += Vector2.Distance(this.Vertices[this.Lines[i]].Position, this.Vertices[this.Lines[i+1]].Position);
        }
        return length;
    }
}

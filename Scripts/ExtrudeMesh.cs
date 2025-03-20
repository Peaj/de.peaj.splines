using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System;

[AddComponentMenu("Spline/Extrude Mesh")]
[RequireComponent(typeof(MeshRenderer))]
[RequireComponent(typeof(MeshFilter))]
public class ExtrudeMesh : MonoBehaviour, ISerializationCallbackReceiver
{
    const float VectexMergeTolerance = 0.1f;

    public enum IntersectionAvoidanceMethods
    {
        None,
        SegmentBased,
        VertexBased,
        Both
    }

    public enum EndingStyles
    {
        Flat,
        Pointy,
        Rounded
    }

    public float Spacing = 0.1f;
    public bool OptimizeMesh = true;
    public bool ShowWireframe = true;
    public float OptimizationOffset = 0.01f;
    public Vector2Range Offset = Vector2.zero;
    public Vector2Range Scale = Vector2.one;
    public Shape2D Shape;
    public IntersectionAvoidanceMethods IntersectionAvoidance;
    public EndingStyles EndingStyle = EndingStyles.Flat;
    public bool ShowOverlap = false;
    public bool ShowBounds = false;

    private Mesh mesh;
    [SerializeField]
    private Spline spline;
    
    private new MeshRenderer renderer;

    [SerializeField]
    private int segments = 0;
    [SerializeField]
    private int vertexCount = 0;
    [SerializeField]
    private int triangleCount = 0;
    [SerializeField]
    private int segmentsSaved = 0;
    [SerializeField]
    private int vertexCountSaved = 0;
    [SerializeField]
    private int triangleCountSaved = 0;
    [SerializeField]
    private float rebuildDuration = 0;

    //Lists cached for optimization
    private List<OrientedPoint> pointList = new List<OrientedPoint>();
    private List<float> normalizedPositionList = new List<float>();
    private List<Color> colorList = new List<Color>();
    private List<int> remove = new List<int>();

    public Spline Spline
    {
        get { return this.spline; }
        set
        {
            this.spline = value;
            RegisterSplineEvents();
            Rebuild();
        }
    }

    public int VertexCount
    {
        get
        {
            if(!this.mesh) mesh = this.GetComponent<MeshFilter>().mesh;
            if (!this.mesh) return 0;
            return mesh.vertexCount;
        }
    }

    void OnEnable()
    {
        this.renderer = this.GetComponent<MeshRenderer>();
        RegisterSplineEvents();
    }

    void OnDisable()
    {
        if (!this.Spline) return;
        this.spline.OnSplineUpdate -= this.Rebuild;
    }

    public void RegisterSplineEvents()
    {
        if (!this.spline) return;
        this.spline.OnSplineUpdate -= this.Rebuild;
        this.spline.OnSplineUpdate += this.Rebuild;
    }

    void Reset()
    {
        Debug.Log("Reset");
        var spline = this.gameObject.GetComponent<Spline>();
        if (!spline) return;

        this.spline = spline;
        this.Shape = Resources.Load("Default") as Shape2D;
        this.renderer = this.GetComponent<MeshRenderer>();
        this.renderer.sharedMaterial = new Material(Shader.Find("Standard"));
        RegisterSplineEvents();
        Rebuild();
    }

    [ContextMenu("Rebuild")]
    public void Rebuild()
    {
        if (!this.spline || !this.Shape) return;
        var watch = System.Diagnostics.Stopwatch.StartNew();
        UnityEngine.Profiling.Profiler.BeginSample("Initialization");

        this.renderer = this.GetComponent<MeshRenderer>();

        int segments = (int)(this.spline.Length / this.Spacing);

        this.mesh = new Mesh();
        this.mesh.name = "Extrude Mesh";
        var bounds = this.Shape.Bounds;
        int vertsInShape = this.Shape.Vertices.Length;

        float aspect = 1f;
        if (this.renderer.sharedMaterial && this.renderer.sharedMaterial.mainTexture)
            aspect = (float)this.renderer.sharedMaterial.mainTexture.width / (float)this.renderer.sharedMaterial.mainTexture.height;

        UnityEngine.Profiling.Profiler.EndSample();

        #region Segment Generation
        UnityEngine.Profiling.Profiler.BeginSample("Segment Generation");

        this.pointList.Clear();
        this.normalizedPositionList.Clear();
        this.colorList.Clear();

        OrientedPoint point;
        OrientedPoint lastPoint = this.spline.GetLocalOrientedPoint(0f);

        for (int i = 0; i <= segments; ++i)
        {
            float tn = (float)i / (float)segments;
            point = this.spline.GetLocalOrientedPoint(this.Spline.GetPosition(tn));
            Vector2 offset = this.Offset.Evaluate(tn);
            point.Position += point.Rotation * offset;
            this.pointList.Add(point);
            this.normalizedPositionList.Add(tn);
            this.colorList.Add(Color.black);
            lastPoint = point;
        }
        UnityEngine.Profiling.Profiler.EndSample();
        #endregion

        #region Optimize Mesh
        this.segmentsSaved = 0;
        if (this.OptimizeMesh)
        {
            UnityEngine.Profiling.Profiler.BeginSample("Optimize Mesh");

            remove.Clear();
            for (int i = 0, prev = 0; i < segments; ++i)
            {
                point = pointList[i];
                lastPoint = pointList[prev];

                var transformedPoint = lastPoint.WorldToLocal(point.Position);

                if ((Mathf.Abs(transformedPoint.x) < this.OptimizationOffset) &&
                    (Mathf.Abs(transformedPoint.y) < this.OptimizationOffset) &&
                    Vector2.Distance(this.Scale.Evaluate(normalizedPositionList[prev]), this.Scale.Evaluate(normalizedPositionList[i])) < this.OptimizationOffset &&
                    (Mathf.Abs(lastPoint.Rotation.eulerAngles.z - point.Rotation.eulerAngles.z) * 0.005f < this.OptimizationOffset))
                {
                    remove.Add(i);
                }
                else prev = i;
            }

            this.segmentsSaved = remove.Count - 1;

            for (int i = this.segmentsSaved; i > 0; --i)
            {
                this.pointList.RemoveAt(remove[i]);
                this.normalizedPositionList.RemoveAt(remove[i]);
                this.colorList.RemoveAt(remove[i]);
            }
            UnityEngine.Profiling.Profiler.EndSample();
        }

        this.vertexCountSaved = vertsInShape * this.segmentsSaved;
        this.triangleCountSaved = this.Shape.Lines.Length * this.segmentsSaved;

        #endregion

        UnityEngine.Profiling.Profiler.BeginSample("Lists to Arrays");
        OrientedPoint[] points = pointList.ToArray();
        float[] normalizedPositions = normalizedPositionList.ToArray();
        int[] overlap = new int[points.Length];
        UnityEngine.Profiling.Profiler.EndSample();

        #region Correct Segment Intersection
        if (this.ShowOverlap || this.IntersectionAvoidance != IntersectionAvoidanceMethods.None)
        {
            UnityEngine.Profiling.Profiler.BeginSample("Correct Segment Intersection");
            for (int i = 0; i < points.Length; ++i)
            {
                Vector2 overlapPos = bounds.min;
                int overlapCount = CountOverlaps(points, normalizedPositions, bounds.min, bounds, i, segments / 4);
                if(overlapCount <= 0)
                {
                    overlapCount = CountOverlaps(points, normalizedPositions, bounds.max, bounds, i, segments / 4);
                    overlapPos = bounds.max;
                }
                overlap[i] = overlapCount;

                if (overlapCount > 0)
                {
                    if (this.ShowOverlap)
                    {
                        for (int j = i; j < i + overlapCount + 1; ++j) colorList[j] = Color.red;
                    }

                    if (this.IntersectionAvoidance == IntersectionAvoidanceMethods.SegmentBased ||
                        this.IntersectionAvoidance == IntersectionAvoidanceMethods.Both)
                    {
                        Vector3 local = overlapPos;
                        local.y = 0f;

                        Vector3 lp1 = local;
                        lp1.Scale(this.Scale.Evaluate(normalizedPositions[i]));

                        Vector3 lp2 = local;
                        lp2.Scale(this.Scale.Evaluate(normalizedPositions[i+overlapCount]));

                        Vector3 p1 = points[i].LocalToWorld(lp1);
                        Vector3 p2 = points[i + overlapCount].LocalToWorld(lp2);

                        Vector3 mid = Vector3.Lerp(p1, p2, 0.5f);

                        for (int j = i; j < i+overlapCount+1; ++j)
                        {
                            Vector3 side = local.x <= 0f? points[j].Position - mid : mid - points[j].Position;
                            Vector3 forward = Vector3.Cross(side, points[j].Up);
                            points[j].Rotation = Quaternion.LookRotation(forward, points[j].Up);
                        }
                    }
                    i += overlapCount;
                }
            }
            UnityEngine.Profiling.Profiler.EndSample();
        }
        #endregion

        #region Generate Vertices
        UnityEngine.Profiling.Profiler.BeginSample("Generate Vertices");

        int endingSegments = 0;
        if (this.EndingStyle != EndingStyles.Flat)
        {
            endingSegments = 10;
        }

        int splineSegments = points.Length - 1;
        this.segments = splineSegments + endingSegments * 2;
        int edgeLoops = this.segments + 1;
        this.vertexCount = vertsInShape * edgeLoops;
        this.triangleCount = this.Shape.Lines.Length * this.segments;
        int triangleIndexCount = this.triangleCount * 3;

        float uspan = this.Shape.GetUSpan();

        UnityEngine.Profiling.Profiler.BeginSample("Create Arrays");
        int[] triangles = new int[triangleIndexCount];
        Vector3[] vertices = new Vector3[this.vertexCount];
        Color[] colors = new Color[this.vertexCount];
        Vector3[] normals = new Vector3[this.vertexCount];
        Vector2[] uvs = new Vector2[this.vertexCount];
        UnityEngine.Profiling.Profiler.EndSample();

        Vector2 scale0 = Vector2.zero;
        Vector2 offset0 = Vector2.zero;

        for (int i= 0; i<= splineSegments; ++i)
        {
            int indexOffset = (endingSegments+i) * vertsInShape;
            //Debug.Log("Lenght: " + length);
            Vector2 scale = this.Scale.Evaluate(normalizedPositions[i]);

            if (i == 0)
            {
                scale0 = scale;
            }
            if (this.spline.Loop && i == splineSegments)
            {
                scale = scale0;
            }

            //float v = this.spline.GetLength(normalizedPositions[i] / (uspan * scale.x));
            float v = (normalizedPositions[i] * this.spline.Length / uspan* aspect);

            for (int j=0; j<vertsInShape; ++j)
            {
                int id = indexOffset + j;
                
                vertices[id] = points[i].LocalToWorld(Vector3.Scale(this.Shape.Vertices[j].Position, scale));
                normals[id] = points[i].LocalToWorldDirection(this.Shape.Vertices[j].Normal);
                uvs[id] = new Vector2(this.Shape.Vertices[j].U, v);
                colors[id] = colorList[i];
            }
        }
        UnityEngine.Profiling.Profiler.EndSample();
        #endregion

        #region Create Endings
        UnityEngine.Profiling.Profiler.BeginSample("Create Endings");
        if (this.EndingStyle != EndingStyles.Flat)
        {
            var startPoint = points[0];
            Vector3 scale = this.Scale.Evaluate(0f);
            scale.z = 1f;
            Vector2 scaleOffset = Vector2.one;

            for (int i = 0; i < endingSegments; ++i)
            {
                int indexOffset = i * vertsInShape;
                float normalizedPos = (float)i / (float)endingSegments;
                Vector2 offset = Vector2.zero;

                if (this.EndingStyle == EndingStyles.Pointy)
                {
                    offset = -scale + scale * normalizedPos;
                    scaleOffset = Vector2.Lerp(Vector2.zero, scale, normalizedPos);
                }
                else if (this.EndingStyle == EndingStyles.Rounded)
                {
                    offset = -scale + scale * normalizedPos * normalizedPos;
                    scaleOffset.x = Mathf.Sqrt((scale.x * scale.x) - (offset.x * offset.x));
                    scaleOffset.y = Mathf.Sqrt((scale.y * scale.y) - (offset.y * offset.y));

                    if(float.IsNaN(scaleOffset.x)) scaleOffset.x = 0f;
                    if(float.IsNaN(scaleOffset.y)) scaleOffset.y = 0f;
                }

                Vector3 offsetVector = new Vector3(0f, 0f, Mathf.Lerp(offset.x, offset.y, 0.5f));

                float v = (offset.x / uspan * aspect);

                for (int j = 0; j < vertsInShape; ++j)
                {
                    int id = indexOffset + j;
                    float centerOffset = ((Vector2)this.Shape.Vertices[j].Position).magnitude;

                    Vector3 local = (Vector3)this.Shape.Vertices[j].Position + offsetVector;// * centerOffset;
                    local.x *= scaleOffset.x;
                    local.y *= scaleOffset.y;
                    vertices[id] = startPoint.LocalToWorld(local);
                    normals[id] = startPoint.LocalToWorldDirection(this.Shape.Vertices[j].Normal);
                    uvs[id] = new Vector2(this.Shape.Vertices[j].U, v);
                    colors[id] = Color.green;
                }
            }

            var endPoint = points[points.Length-1];
            float vEnd = (normalizedPositions[points.Length - 1] * this.spline.Length / uspan * aspect);
            scale = this.Scale.Evaluate(1f);
            scale.z = 1f;
            float scaleOffsetX = 1f;
            scale.x = bounds.width * scale.x * 0.5f;

            for (int i = 1; i <= endingSegments; ++i)
            {
                int indexOffset = (endingSegments + splineSegments + i) * vertsInShape;
                float normalizedPos = 1f-(((float)i) / ((float)endingSegments));
                float offset = 0;

                if (this.EndingStyle == EndingStyles.Pointy)
                {
                    offset = scale.x - scale.x * normalizedPos;
                    scaleOffsetX = Mathf.Lerp(0f, scale.x, normalizedPos);
                }
                else if (this.EndingStyle == EndingStyles.Rounded)
                {
                    offset = scale.x - scale.x * (normalizedPos * normalizedPos);
                    scaleOffsetX = Mathf.Sqrt((scale.x * scale.x) - (offset * offset));
                }

                Vector3 offsetVector = new Vector3(0f, 0f, offset);
                float v = vEnd + (offset / uspan * aspect);

                for (int j = 0; j < vertsInShape; ++j)
                {
                    int id = indexOffset + j;
                    float centerOffset = ((Vector2)this.Shape.Vertices[j].Position).magnitude;

                    Vector3 local = (Vector3)this.Shape.Vertices[j].Position + offsetVector * centerOffset;
                    local.x *= scaleOffsetX;
                    local.y *= scaleOffsetX;
                    //local = Vector3.Scale(local, scale);

                    vertices[id] = endPoint.LocalToWorld(local);
                    normals[id] = endPoint.LocalToWorldDirection(this.Shape.Vertices[j].Normal);
                    uvs[id] = new Vector2(this.Shape.Vertices[j].U, v);
                    colors[id] = Color.green;
                }
            }
        }
        UnityEngine.Profiling.Profiler.EndSample();
        #endregion

        #region Correct Vertex Intersection
        if (this.IntersectionAvoidance == IntersectionAvoidanceMethods.VertexBased ||
            this.IntersectionAvoidance == IntersectionAvoidanceMethods.Both)
        {
            UnityEngine.Profiling.Profiler.BeginSample("Correct Vertex Intersection");
            float tolerance = 0.1f;

            bounds.xMin -= tolerance;
            bounds.yMin -= tolerance;
            bounds.xMax += tolerance;
            bounds.yMax += tolerance;

            for (int i = 0; i < overlap.Length; ++i)
            {
                if (overlap[i] <= 0) continue;

                for (int j = 0; j < vertsInShape; ++j)
                {
                    int overlapCount = CountOverlaps(points, normalizedPositions, this.Shape.Vertices[j].Position, bounds, i, overlap[i], VectexMergeTolerance);
                    if (overlapCount <= 0) continue;

                    var scale1 = this.Scale.Evaluate(normalizedPositions[i]);
                    var scale2 = this.Scale.Evaluate(normalizedPositions[i + overlap[i]]);

                    Vector3 p1 = points[i].LocalToWorld(Vector2.Scale(this.Shape.Vertices[j].Position,scale1));
                    Vector3 p2 = points[i + overlap[i]].LocalToWorld(Vector2.Scale(this.Shape.Vertices[j].Position,scale2));

                    Vector3 mid = Vector3.Lerp(p1, p2, 0.5f);

                    for (int k = i-1; k < i + overlapCount+1; ++k)
                    {
                        vertices[(endingSegments + k) * vertsInShape + j] = mid;
                    }
                }
            }
            UnityEngine.Profiling.Profiler.EndSample();
        }
        #endregion

        #region Triangulation
        UnityEngine.Profiling.Profiler.BeginSample("Triangulation");
        int ti = 0;
        for(int i=0; i<this.segments; ++i)
        {
            int offset = i * vertsInShape;
            for ( int j=0; j<this.Shape.Lines.Length; j += 2)
            {
                int a = offset + this.Shape.Lines[j] + vertsInShape;
                int b = offset + this.Shape.Lines[j];
                int c = offset + this.Shape.Lines[j+1];
                int d = offset + this.Shape.Lines[j+1] + vertsInShape;

                triangles[ti++] = a;
                triangles[ti++] = b;
                triangles[ti++] = c;
                triangles[ti++] = c;
                triangles[ti++] = d;
                triangles[ti++] = a;
            }
        }
        UnityEngine.Profiling.Profiler.EndSample();
        #endregion

        UnityEngine.Profiling.Profiler.BeginSample("Create Mesh");
        mesh.Clear();
        mesh.vertices = vertices;
        mesh.colors = colors;
        mesh.triangles = triangles;
        mesh.normals = normals;
        mesh.uv = uvs;

        this.GetComponent<MeshFilter>().mesh = this.mesh;
        UnityEngine.Profiling.Profiler.EndSample();


        var collider = this.GetComponent<MeshCollider>();
        if (collider)
        {
            UnityEngine.Profiling.Profiler.BeginSample("Update Collider");
            collider.sharedMesh = null;
            collider.sharedMesh = this.mesh;
            UnityEngine.Profiling.Profiler.EndSample();
        }

        watch.Stop();
        this.rebuildDuration = (float)watch.Elapsed.TotalMilliseconds;
    }

    private int CountOverlaps(OrientedPoint[] points, float[] normalizedPositions, Vector2 localPosition, Rect bounds , int index, int ignores = 0, float tolerance = 0f)
    {
        bounds.xMin -= tolerance;
        bounds.yMin -= tolerance;
        bounds.xMax += tolerance;
        bounds.yMax += tolerance;

        int count = 0;
        int jumps = 0;
        for (int i = index+1; i < points.Length; ++i)
        {
            var checkPoint = points[i].LocalToWorld(Vector2.Scale(localPosition,this.Scale.Evaluate(normalizedPositions[i])));
            checkPoint = points[index].WorldToLocal(checkPoint);

            if (checkPoint.z < tolerance && bounds.Scaled(this.Scale.Evaluate(normalizedPositions[index])).Touches(checkPoint))
            {
                ++count;
                count += jumps;
                jumps = 0;
            }
            else
            {
                if (ignores <= 0) return count;
                else
                {
                    ++jumps;
                    --ignores;
                }
            }
        }
        return count;
    }

    public void OnBeforeSerialize()
    {
        
    }

    public void OnAfterDeserialize()
    {
        RegisterSplineEvents();
    }
}

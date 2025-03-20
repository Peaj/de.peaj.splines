using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[AddComponentMenu("Spline/Spline Sprite")]
[RequireComponent(typeof(MeshRenderer))]
[RequireComponent(typeof(MeshFilter))]
public class SplineSprite : MonoBehaviour, ISerializationCallbackReceiver {

    public Sprite Sprite;

    public int Segments = 100;
    public bool OptimizeMesh = true;
    public bool ShowWireframe = true;
    public float OptimizationOffset = 0.01f;
    public Vector2Range Offset = Vector2.zero;
    public Vector2Range Scale = Vector2.one;
    public Shape2D Shape;

    private Mesh mesh;
    [SerializeField]
    private Spline spline;

    private new MeshRenderer renderer;

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
        this.renderer.sharedMaterial = new Material(Shader.Find("Sprites/Default"));
        RegisterSplineEvents();
        Rebuild();
    }

    [ContextMenu("Rebuild")]
    public void Rebuild()
    {
        this.renderer = this.GetComponent<MeshRenderer>();
        Rect textureCoords = new Rect();
        if (this.Sprite)
        {
            renderer.sharedMaterial.mainTexture = this.Sprite.texture;
            float width = this.Sprite.texture.width;
            float height = this.Sprite.texture.height;
            textureCoords = new Rect(Sprite.textureRect.x / width, Sprite.textureRect.y / height, Sprite.textureRect.width / width, Sprite.textureRect.height / height);
        }

        if (!this.spline || !this.Shape) return;
        this.renderer = this.GetComponent<MeshRenderer>();

        this.mesh = new Mesh();
        this.mesh.name = "Spline Sprite";

        float uspan = this.Shape.GetUSpan();
        Vector2 scale;

        float aspect = 1f;
        if (this.renderer.sharedMaterial && this.renderer.sharedMaterial.mainTexture)
            aspect = (float)this.renderer.sharedMaterial.mainTexture.width / (float)this.renderer.sharedMaterial.mainTexture.height;
        float vLength = uspan * aspect;

        List<OrientedPoint> pointList = new List<OrientedPoint>();
        List<float> normalizedPositions = new List<float>();
        List<float> vs = new List<float>();
        OrientedPoint point;
        OrientedPoint lastPoint = this.spline.GetLocalOrientedPoint(0f);

        int uvSeamCount = 1;
        float lastTn = 0f;
        for (int i = 0; i <= this.Segments; ++i)
        {
            float tn = (float)i / (float)this.Segments;
            scale = this.Scale.Evaluate(tn);
            float length = tn * this.spline.Length;
            float seamPos = (float)uvSeamCount * vLength;
            float v = (length / vLength) % 1; 

            if (length > seamPos)
            {
                ++uvSeamCount;
                float tn2 = this.spline.GetNormalizedPositionFromlength(seamPos);
                lastPoint = this.spline.GetLocalOrientedPoint(this.Spline.GetPosition(tn2));

                pointList.Add(lastPoint);
                normalizedPositions.Add(tn2);
                vs.Add(1f);

                pointList.Add(lastPoint);
                normalizedPositions.Add(tn2);
                vs.Add(0f);
            }

            float t = this.Spline.GetPosition(tn);
            point = this.spline.GetLocalOrientedPoint(t);

            if (this.OptimizeMesh &&
                i != 0 && // Do not cull first
                i != this.Segments && // Do not cull last segment
                (Mathf.Abs(lastPoint.WorldToLocal(point.Position).x) < this.OptimizationOffset) &&
                (Mathf.Abs(lastPoint.WorldToLocal(point.Position).y) < this.OptimizationOffset) &&
                Vector2.Distance(this.Scale.Evaluate(lastTn), this.Scale.Evaluate(tn)) < this.OptimizationOffset &&
                (Mathf.Abs(lastPoint.Rotation.eulerAngles.z - point.Rotation.eulerAngles.z) * 0.005f < this.OptimizationOffset))
                continue;

            pointList.Add(point);
            normalizedPositions.Add(tn);
            vs.Add(v);
            lastPoint = point;
            lastTn = tn;
        }

        OrientedPoint[] points = pointList.ToArray();

        int vertsInShape = this.Shape.Vertices.Length;
        int segments = points.Length - 1;
        int edgeLoops = points.Length;
        int vertexCount = vertsInShape * edgeLoops;
        int triangleCount = this.Shape.Lines.Length * segments;
        int triangleIndexCount = triangleCount * 3;


        int[] triangles = new int[triangleIndexCount];
        Vector3[] vertices = new Vector3[vertexCount];
        Vector3[] normals = new Vector3[vertexCount];
        Vector2[] uvs = new Vector2[vertexCount];

        Vector2 scale0 = Vector2.zero;
        Vector2 offset0 = Vector2.zero;

        for (int i = 0; i <= segments; ++i)
        {
            int indexOffset = i * vertsInShape;
            scale = this.Scale.Evaluate(normalizedPositions[i]);
            Vector2 offset = this.Offset.Evaluate(normalizedPositions[i]);

            if (i == 0)
            {
                scale0 = scale;
                offset0 = offset;
            }
            if (this.spline.Loop && i == segments)
            {
                scale = scale0;
                offset = offset0;
            }

            float v = vs[i];
            v = textureCoords.y + v * textureCoords.height;

            for (int j = 0; j < vertsInShape; ++j)
            {
                float u = this.Shape.Vertices[j].U;
                u = textureCoords.x + u * textureCoords.width;

                int id = indexOffset + j;

                vertices[id] = points[i].LocalToWorld(Vector3.Scale(this.Shape.Vertices[j].Position, scale) + (Vector3)offset);
                normals[id] = points[i].LocalToWorldDirection(this.Shape.Vertices[j].Normal);
                uvs[id] = new Vector2(u, v);
            }
        }

        //Indices
        int ti = 0;
        for (int i = 0; i < segments; ++i)
        {
            int offset = i * vertsInShape;
            for (int j = 0; j < this.Shape.Lines.Length; j += 2)
            {
                int a = offset + this.Shape.Lines[j] + vertsInShape;
                int b = offset + this.Shape.Lines[j];
                int c = offset + this.Shape.Lines[j + 1];
                int d = offset + this.Shape.Lines[j + 1] + vertsInShape;

                triangles[ti++] = a;
                triangles[ti++] = b;
                triangles[ti++] = c;
                triangles[ti++] = c;
                triangles[ti++] = d;
                triangles[ti++] = a;
            }
        }

        mesh.Clear();
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.normals = normals;
        mesh.uv = uvs;

        this.GetComponent<MeshFilter>().mesh = this.mesh;
    }

    public void OnBeforeSerialize()
    {

    }

    public void OnAfterDeserialize()
    {
        RegisterSplineEvents();
    }
}

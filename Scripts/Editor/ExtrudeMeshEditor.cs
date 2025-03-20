using UnityEngine;
using System.Collections;
using UnityEditor;

[CustomEditor(typeof(ExtrudeMesh))]
public class ExtrudeMeshEditor : Editor
{
    private ExtrudeMesh script;

    private bool visualizationFoldout = false;

    private SerializedProperty spline;
    private SerializedProperty shape;
    private SerializedProperty spacing;
    private SerializedProperty optimizeMesh;
    private SerializedProperty showWireframe;
    private SerializedProperty optimizationOffset;
    private SerializedProperty offset;
    private SerializedProperty scale;
    private SerializedProperty scaleRange;
    private SerializedProperty scaleCurve;
    private SerializedProperty intersectionAvoidance;
    private SerializedProperty showOverlap;
    private SerializedProperty showBounds;

    private SerializedProperty segments;
    private SerializedProperty vertexCount;
    private SerializedProperty triangleCount;

    private SerializedProperty segmentsSaved;
    private SerializedProperty vertexCountSaved;
    private SerializedProperty triangleCountSaved;

    private SerializedProperty rebuildDuration;

    private SerializedProperty endingStyle;

    protected void OnEnable()
    {
        this.spline = serializedObject.FindProperty("spline");
        this.shape = serializedObject.FindProperty("Shape");
        this.spacing = serializedObject.FindProperty("Spacing");
        this.optimizeMesh = serializedObject.FindProperty("OptimizeMesh");
        this.showWireframe = serializedObject.FindProperty("ShowWireframe");
        this.optimizationOffset = serializedObject.FindProperty("OptimizationOffset");
        this.offset = serializedObject.FindProperty("Offset");
        this.scale = serializedObject.FindProperty("Scale");
        this.scaleRange = serializedObject.FindProperty("ScaleRange");
        this.scaleCurve = serializedObject.FindProperty("ScaleCurve");
        this.showOverlap = serializedObject.FindProperty("ShowOverlap");
        this.intersectionAvoidance = serializedObject.FindProperty("IntersectionAvoidance");
        this.showBounds = serializedObject.FindProperty("ShowBounds");

        this.segments = serializedObject.FindProperty("segments");
        this.vertexCount = serializedObject.FindProperty("vertexCount");
        this.triangleCount = serializedObject.FindProperty("triangleCount");

        this.segmentsSaved = serializedObject.FindProperty("segmentsSaved");
        this.vertexCountSaved = serializedObject.FindProperty("vertexCountSaved");
        this.triangleCountSaved = serializedObject.FindProperty("triangleCountSaved");

        this.rebuildDuration = serializedObject.FindProperty("rebuildDuration");

        this.endingStyle = serializedObject.FindProperty("EndingStyle");
    }

    public override void OnInspectorGUI()
    {
        script = target as ExtrudeMesh;

        this.serializedObject.Update();


        EditorGUI.BeginChangeCheck();
        EditorGUILayout.PropertyField(spline);
        EditorGUILayout.PropertyField(shape);
        EditorGUILayout.PropertyField(spacing);
        if (spacing.floatValue < 0.01f) spacing.floatValue = 0.01f;

        GUILayout.Label("Shape", EditorStyles.boldLabel);

        EditorGUILayout.PropertyField(offset);
        EditorGUILayout.PropertyField(scale);
        EditorGUILayout.PropertyField(endingStyle);

        GUILayout.Label("Optimization", EditorStyles.boldLabel);

        EditorGUILayout.PropertyField(optimizeMesh);
        EditorGUILayout.PropertyField(optimizationOffset);
        if (optimizationOffset.floatValue < 0f) optimizationOffset.floatValue = 0f;
        EditorGUILayout.PropertyField(intersectionAvoidance);

        GUILayout.Label("Mesh Info", EditorStyles.boldLabel);

        Color defaultColor = GUI.contentColor;

        float ratio = 0f;

        GUIStyle align = new GUIStyle(EditorStyles.label);
        align.alignment = TextAnchor.MiddleRight;
        //align.fixedWidth = 70f;
        align.normal.textColor = new Color(0.3f, 1f, 0.3f, 0.8f);

        EditorGUIUtility.fieldWidth = 30f;
        

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Segments", segments.intValue.ToString());
        if (optimizeMesh.boolValue)
        {
            ratio = ((float)segmentsSaved.intValue) / (float)(segments.intValue + segmentsSaved.intValue);
            EditorGUIUtility.labelWidth = 40f;
            EditorGUILayout.LabelField(string.Format("{0} ({1:P0})", -segmentsSaved.intValue, ratio), align);
            EditorGUIUtility.labelWidth = 0f;
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Vertex Count", vertexCount.intValue.ToString());
        if (optimizeMesh.boolValue)
        {
            ratio = ((float)vertexCountSaved.intValue) / (float)(vertexCount.intValue + vertexCountSaved.intValue);
            EditorGUIUtility.labelWidth = 40f;
            EditorGUILayout.LabelField(string.Format("{0} ({1:P0})", -vertexCountSaved.intValue, ratio), align);
            EditorGUIUtility.labelWidth = 0f;
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Triangle Count", triangleCount.intValue.ToString());
        if (optimizeMesh.boolValue)
        {
            ratio = ((float)triangleCountSaved.intValue) / (float)(triangleCount.intValue + triangleCountSaved.intValue);
            EditorGUIUtility.labelWidth = 40f;
            EditorGUILayout.LabelField(string.Format("{0} ({1:P0})", -triangleCountSaved.intValue, ratio), align);
            EditorGUIUtility.labelWidth = 0f;
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Build time", rebuildDuration.floatValue.ToString() + " ms");

        EditorGUIUtility.fieldWidth = 0f;
        EditorGUIUtility.labelWidth = 0f;

        GUILayout.Label("Visualization", EditorStyles.boldLabel);

        EditorGUIUtility.labelWidth = 100f;
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.PropertyField(showOverlap, new GUIContent("Overlap"));
        this.serializedObject.ApplyModifiedProperties();
        if (EditorGUI.EndChangeCheck())
        {
            this.script.RegisterSplineEvents();
            this.script.Rebuild();
        }
        EditorGUILayout.PropertyField(showBounds, new GUIContent("Bounds"));
        EditorGUILayout.EndHorizontal();
        EditorGUI.BeginChangeCheck();
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.PropertyField(showWireframe, new GUIContent("Wireframe"));
        EditorGUILayout.EndHorizontal();
        EditorGUIUtility.labelWidth = 0f;
        this.serializedObject.ApplyModifiedProperties();
        if (EditorGUI.EndChangeCheck())
        {
            EditorUtility.SetSelectedWireframeHidden(script.GetComponent<MeshRenderer>(), !this.showWireframe.boolValue);
            SceneView.RepaintAll();
        }
    }

    private void OnSceneGUI()
    {
        script = target as ExtrudeMesh;

        if (showBounds.boolValue)
        {
            var bounds = this.script.Shape.Bounds;

            Vector3 lp0 = Vector2.zero;
            Vector3 lp1 = Vector2.zero;
            Vector3 lp2 = Vector2.zero;
            Vector3 lp3 = Vector2.zero;

            int segments = (int)(this.script.Spline.Length / 0.5f);
            for (int i=0; i<=segments; ++i)
            {
                float tn = script.Spline.GetPositionFromLength((float)i* (this.script.Spline.Length/(float)segments));
                OrientedPoint point = script.Spline.GetLocalOrientedPoint(script.Spline.GetPosition(tn));
                OrientedPoint temp;
                temp.Position = script.Spline.transform.TransformPoint(point.Position + point.Rotation * (Vector3)this.script.Offset.Evaluate(tn));
                temp.Rotation = script.Spline.transform.rotation * point.Rotation;
                var scale = script.Scale.Evaluate(tn);

                //Handles.RectangleCap(0, position, rotation, scale);

                Vector3 p0 = temp.LocalToWorld(Vector3.Scale(bounds.min,scale));
                Vector3 p1 = temp.LocalToWorld(Vector3.Scale(new Vector3(bounds.xMin, bounds.yMax), scale));
                Vector3 p2 = temp.LocalToWorld(Vector3.Scale(bounds.max, scale));
                Vector3 p3 = temp.LocalToWorld(Vector3.Scale(new Vector3(bounds.xMax, bounds.yMin), scale));

                if(i > 0)
                {
                    Handles.DrawLine(lp0, p0);
                    Handles.DrawLine(lp1, p1);
                    Handles.DrawLine(lp2, p2);
                    Handles.DrawLine(lp3, p3);
                }

                Handles.DrawLine(p0, p1);
                Handles.DrawLine(p1, p2);
                Handles.DrawLine(p2, p3);
                Handles.DrawLine(p3, p0);

                lp0 = p0;
                lp1 = p1;
                lp2 = p2;
                lp3 = p3;
            }
        }
    }
}
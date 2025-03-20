using UnityEngine;
using System.Collections;
using UnityEditor;

[CustomEditor(typeof(SplineSprite),true)]
public class SplineSpriteEditor : Editor
{
    private SplineSprite script;

    private SerializedProperty spline;
    private SerializedProperty shape;
    private SerializedProperty segments;
    private SerializedProperty optimizeMesh;
    private SerializedProperty showWireframe;
    private SerializedProperty optimizationOffset;
    private SerializedProperty offset;
    private SerializedProperty scale;
    private SerializedProperty scaleRange;
    private SerializedProperty scaleCurve;
    private SerializedProperty textureCoords;

    private SerializedProperty sprite;

    protected void OnEnable()
    {
        this.sprite = serializedObject.FindProperty("Sprite");
        this.spline = serializedObject.FindProperty("spline");
        this.shape = serializedObject.FindProperty("Shape");
        this.segments = serializedObject.FindProperty("Segments");
        this.optimizeMesh = serializedObject.FindProperty("OptimizeMesh");
        this.showWireframe = serializedObject.FindProperty("ShowWireframe");
        this.optimizationOffset = serializedObject.FindProperty("OptimizationOffset");
        this.offset = serializedObject.FindProperty("Offset");
        this.scale = serializedObject.FindProperty("Scale");
        this.scaleRange = serializedObject.FindProperty("ScaleRange");
        this.scaleCurve = serializedObject.FindProperty("ScaleCurve");
        this.textureCoords = serializedObject.FindProperty("TextureCoords");
    }

    public override void OnInspectorGUI()
    {
        this.script = target as SplineSprite;

        this.serializedObject.Update();


        EditorGUI.BeginChangeCheck();

        if (this.target is SplineSprite)
        {
            EditorGUILayout.PropertyField(sprite);
            EditorGUILayout.Space();
        }

        EditorGUILayout.PropertyField(spline, true);
        //EditorGUILayout.PropertyField(shape, true);
        EditorGUILayout.PropertyField(segments, true);
        EditorGUILayout.PropertyField(optimizeMesh, true);
        EditorGUILayout.PropertyField(optimizationOffset, true);
        EditorGUILayout.PropertyField(offset, true);
        EditorGUILayout.PropertyField(scale, true);

        this.serializedObject.ApplyModifiedProperties();
        if (EditorGUI.EndChangeCheck())
        {
            this.script.RegisterSplineEvents();
            this.script.Rebuild();
        }

        EditorGUI.BeginChangeCheck();
        EditorGUILayout.PropertyField(showWireframe, true);
        this.serializedObject.ApplyModifiedProperties();
        if (EditorGUI.EndChangeCheck())
        {
            EditorUtility.SetSelectedWireframeHidden(script.GetComponent<MeshRenderer>(), !this.showWireframe.boolValue);
            SceneView.RepaintAll();
        }
    }
}
using UnityEngine;
using System.Collections;
using UnityEditor;

[CustomEditor(typeof(SpawnOnSpline))]
[CanEditMultipleObjects]
public class SpawnOnSplineEditor : Editor {

    private SpawnOnSpline script;

    private SerializedProperty spline;
    private SerializedProperty start;
    private SerializedProperty end;
    private SerializedProperty prefab;
    private SerializedProperty faceDirection;
    private SerializedProperty offset;
    private SerializedProperty distance;
    private SerializedProperty loopDistance;
    private SerializedProperty instances;

    protected void OnEnable()
    {
        this.spline = serializedObject.FindProperty("spline");
        this.start = serializedObject.FindProperty("Start");
        this.end = serializedObject.FindProperty("End");
        this.prefab = serializedObject.FindProperty("Prefab");
        this.faceDirection = serializedObject.FindProperty("FaceDirection");
        this.offset = serializedObject.FindProperty("Offset");
        this.distance = serializedObject.FindProperty("Distance");
        this.loopDistance = serializedObject.FindProperty("LoopDistance");
        this.instances = serializedObject.FindProperty("instances");
    }

    public override void OnInspectorGUI()
    {
        this.serializedObject.Update();

        EditorGUI.BeginChangeCheck();
        EditorGUILayout.PropertyField(spline,true);
        if (EditorGUI.EndChangeCheck())
        {
            this.serializedObject.ApplyModifiedProperties();
            Rebuild();
        }

        EditorGUI.BeginChangeCheck();
        EditorGUILayout.PropertyField(prefab,true);
        if (EditorGUI.EndChangeCheck())
        {
            this.serializedObject.ApplyModifiedProperties();
            Rebuild();
        }

        EditorGUI.BeginChangeCheck();
        EditorGUILayout.PropertyField(start,true);
        EditorGUILayout.PropertyField(end,true);
        EditorGUILayout.PropertyField(faceDirection,true);
        EditorGUILayout.PropertyField(offset,true);
        EditorGUILayout.PropertyField(distance,true);
        EditorGUILayout.PropertyField(loopDistance,true);

        this.serializedObject.ApplyModifiedProperties();
        if (EditorGUI.EndChangeCheck()) QuickRebuild();


        EditorGUILayout.LabelField("Count", ((SpawnOnSpline)this.target).InstanceCount.ToString());

    }

    private void OnSceneGUI()
    {
        if (Event.current.commandName == "UndoRedoPerformed")
        {
            ((SpawnOnSpline)this.target).Rebuild();
            return;
        }
    }

    private void QuickRebuild()
    {
        foreach (var target in this.serializedObject.targetObjects)
        {
            ((SpawnOnSpline)target).QuickRebuild();
        }
        this.serializedObject.Update();
        this.serializedObject.ApplyModifiedPropertiesWithoutUndo();
    }

    private void Rebuild()
    {
        this.serializedObject.Update();
        foreach (var target in this.serializedObject.targetObjects)
        {
            ((SpawnOnSpline)target).Rebuild();
        }
        this.serializedObject.ApplyModifiedPropertiesWithoutUndo();
    }
}

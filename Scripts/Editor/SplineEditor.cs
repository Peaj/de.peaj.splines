using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(Spline))]
public class SplineEditor : Editor
{
    private static Color[] modeColors = {
        Color.white,
        Color.yellow,
        Color.cyan
    };


    [MenuItem("GameObject/3D Object/Spline")]
    private static void CreateSpline()
    {
        GameObject obj = new GameObject("Spline");
        obj.AddComponent<Spline>();
        if(Selection.activeGameObject != null) obj.transform.parent = Selection.activeGameObject.transform;
        Selection.activeGameObject = obj;
        Undo.RegisterCreatedObjectUndo(obj, "Create Spline");
    }

    [MenuItem("GameObject/2D Object/Spline")]
    private static void CreateSpline2D()
    {
        GameObject obj = new GameObject("Spline");
        Spline spline = obj.AddComponent<Spline>();
        //spline.transform.rotation = Quaternion.Euler(new Vector3(-90f, 0f, 0f));
        spline.SetControlPoint(0, new OrientedPoint(Vector3.zero, Quaternion.Euler(new Vector3(-90f, 0f, 0f))));
        spline.SetControlPoint(1, new OrientedPoint(new Vector3(0f,3f), Quaternion.Euler(new Vector3(-90f, 0f, 0f))));
        Selection.activeGameObject = obj;
        Undo.RegisterCreatedObjectUndo(obj, "Create Spline");
    }

    

    [DrawGizmo(GizmoType.NotInSelectionHierarchy | GizmoType.Pickable)]
    static void RenderCustomGizmo(Spline spline, GizmoType gizmoType)
    {
        Draw(spline);
    }

    public static void Draw(Spline spline)
    {
        if(spline.ControlPointCount <= 0) return;
        
        var handleTransform = spline.transform;
        var handleRotation = Tools.pivotRotation == PivotRotation.Local ?
            handleTransform.rotation : Quaternion.identity;

        Color color = spline.Color;
        color.a *= 0.5f;

        for (int i = 0; i < spline.ControlPointCount - 1; ++i)
        {
            Handles.color = Color.white;
            Vector3 cp0 = handleTransform.TransformPoint(spline.GetControlPoint(i).Position);
            Vector3 cp1 = handleTransform.TransformPoint(spline.GetControlPoint(i + 1).Position);
            Vector3 tp1 = handleTransform.TransformPoint(spline.GetTangentPoint(spline.GetNextTangent(i)));
            Vector3 tp2 = handleTransform.TransformPoint(spline.GetTangentPoint(spline.GetPreviousTangent(i + 1)));

            Handles.DrawBezier(cp0, cp1, tp1, tp2, color, null, 2f);
            cp0 = cp1;
        }

        //Debug.Log(Event.current.mousePosition);

        Vector2 mousePos = Event.current.mousePosition;
        Camera cam;
        if(SceneView.lastActiveSceneView) cam = SceneView.lastActiveSceneView.camera;
        else cam = Camera.current;
        mousePos.y = cam.pixelHeight - mousePos.y;
        //Debug.Log("Mouse: " + mousePos);
        float t = spline.GetNearestPointToScreenPosition(mousePos, 30);

        Gizmos.color = new Color(1f, 0f, 0f, 0f);
        Vector3 point = spline.GetPoint(t);

        //point = SceneView.lastActiveSceneView.camera.ScreenToWorldPoint(new Vector3(mousePos.x, mousePos.y, 1f));
        //Debug.Log("Point: " + point);
        float size = HandleUtility.GetHandleSize(point) * 0.3f;
        Gizmos.DrawCube(point, Vector3.one * size);
    }

    private const int lineSteps = 10;
    private const float directionScale = 0.5f;

    private Spline script;
    private Transform handleTransform;
    private const float handleSize = 0.04f;
    private const float pickSize = 0.06f;

    private int selectedIndex = -1;
    private int selectedTangentIndex = -1;

    private Quaternion globalRotationHandle = Quaternion.identity;
    private Quaternion tangentHandleRotation = Quaternion.identity;
    private PivotRotation tangentPivotRotation = PivotRotation.Global;

    private SerializedProperty loop;
    private SerializedProperty showDefaultHandles;
    private SerializedProperty showNormals;
    private SerializedProperty showDirections;
    private SerializedProperty showLengthSamples;
    private SerializedProperty color;
    private SerializedProperty lengthSamples;

    private bool debugFoldout = false;

    private Color zTestFailedColor = new Color(1f,1f,1f,0.5f);

    protected void OnEnable()
    {
        this.loop = serializedObject.FindProperty("loop");
        this.showDefaultHandles = serializedObject.FindProperty("ShowDefaultHandles");
        this.showNormals = serializedObject.FindProperty("ShowNormals");
        this.showDirections = serializedObject.FindProperty("ShowDirections");
        this.showLengthSamples = serializedObject.FindProperty("ShowLengthSamples");
        this.color = serializedObject.FindProperty("Color");
        this.lengthSamples = serializedObject.FindProperty("lengthSamples");
    }

    public override void OnInspectorGUI()
    {
        script = target as Spline;

        this.serializedObject.Update();

        EditorGUI.BeginChangeCheck();
        EditorGUILayout.PropertyField(loop, true);

        if(debugFoldout = EditorGUILayout.Foldout(debugFoldout, "Visualization"))
        {
            EditorGUIUtility.labelWidth = 100f;
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PropertyField(showDefaultHandles, new GUIContent("Default Handles"), true);
            EditorGUILayout.PropertyField(showNormals, new GUIContent("Normals"), true);
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PropertyField(showDirections, new GUIContent("Directions"), true);
            EditorGUILayout.PropertyField(showLengthSamples, new GUIContent("Length Samples"), true);
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PropertyField(color, new GUIContent("Color"), true);
            EditorGUILayout.EndHorizontal();
            EditorGUIUtility.labelWidth = 0f;
        }

        this.serializedObject.ApplyModifiedProperties();
        if (EditorGUI.EndChangeCheck())
        {
            this.script.SplineUpdate();
        }

        EditorGUI.BeginChangeCheck();
        EditorGUILayout.PropertyField(lengthSamples, true);
        this.serializedObject.ApplyModifiedProperties();
        if (EditorGUI.EndChangeCheck())
        {
            this.script.GenerateLengthSamples();
            this.script.SplineUpdate();
        }

        EditorGUILayout.LabelField("Length", this.script.Length.ToString());

        Tools.hidden = !this.script.ShowDefaultHandles;

        if (selectedIndex >= 0 && selectedIndex < script.ControlPointCount)
        {
            DrawSelectedControlPointInspector();
        }
        else if (this.selectedTangentIndex >= 0 && this.selectedTangentIndex < this.script.TangentPointCount)
        {
            DrawSelectedTangentPointInspector();
        }
    }

    public void OnDisable()
    {
        Tools.hidden = false;
    }

    private void DrawSelectedControlPointInspector()
    {
        EditorGUILayout.Space();

        var rect = GUILayoutUtility.GetLastRect();
        GUI.Box(new Rect(0, rect.y+10f, EditorGUIUtility.currentViewWidth, 135f), GUIContent.none);

        GUILayout.Label("Selected Point", EditorStyles.boldLabel);
        var wideMode = EditorGUIUtility.wideMode;
        EditorGUIUtility.wideMode = true;
        if(EditorGUIUtility.currentViewWidth < 332f) EditorGUIUtility.labelWidth =  EditorGUIUtility.currentViewWidth - 217f;
        EditorGUI.BeginChangeCheck();
        Vector3 position = EditorGUILayout.Vector3Field("Position", script.GetControlPoint(selectedIndex).Position);
        Quaternion rotation = script.GetControlPoint(selectedIndex).Rotation;
        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(script, "Move Point");
            EditorUtility.SetDirty(script);
            script.SetControlPoint(selectedIndex, new OrientedPoint(position, rotation));
        }
        EditorGUI.BeginChangeCheck();
        Vector3 eulerAngles = EditorGUILayout.Vector3Field("Rotation", rotation.eulerAngles);
        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(script, "Rotated Point");
            EditorUtility.SetDirty(script);
            script.SetControlPoint(selectedIndex, new OrientedPoint(position, Quaternion.Euler(eulerAngles)));
        }

        EditorGUI.BeginChangeCheck();
        Spline.ControlPointMode mode = (Spline.ControlPointMode)
            EditorGUILayout.EnumPopup("Mode", script.GetControlPointMode(selectedIndex));
        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(script, "Change Point Mode");
            script.SetControlPointMode(selectedIndex, mode);
            EditorUtility.SetDirty(script);
        }

        EditorGUIUtility.labelWidth = 0f;
        EditorGUIUtility.wideMode = wideMode;

        EditorGUILayout.Space();

        DrawControlPointTools(this.selectedIndex);

        EditorGUILayout.Space();
    }

    private void DrawControlPointTools(int index)
    {
        GUILayout.BeginHorizontal();

        if (GUILayout.Button("Add After"))
        {
            Undo.RecordObject(script, "Spline Point Add");
            this.selectedIndex = script.AddControlPointAt(index, Spline.InsertPositions.After);
            this.selectedTangentIndex = -1;
            EditorUtility.SetDirty(script);
        }

        if (GUILayout.Button("Delete"))
        {
            Undo.RecordObject(script, "Spline Point Removed");
            script.Remove(index);
            if (this.selectedIndex >= this.script.ControlPointCount) this.selectedIndex = this.script.ControlPointCount - 1;
            this.selectedTangentIndex = -1;
            EditorUtility.SetDirty(script);
        }

        if (GUILayout.Button("Add Before"))
        {
            Undo.RecordObject(script, "Spline Point Add");
            this.selectedIndex = script.AddControlPointAt(index, Spline.InsertPositions.Before);
            this.selectedTangentIndex = -1;
            EditorUtility.SetDirty(script);
        }

        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();

        if (GUILayout.Button("Snap to ground"))
        {
            Undo.RecordObject(script, "Spline Point Snap");
            script.SnapControlPointToGround(index);
            EditorUtility.SetDirty(script);
        }

        if (GUILayout.Button("Align upwards"))
        {
            Undo.RecordObject(script, "Spline Point Align Upwards");
            script.AlignControlPointNormalWithUp(index);
            EditorUtility.SetDirty(script);
        }

        GUILayout.EndHorizontal();
    }

    private void DrawSelectedTangentPointInspector()
    {
        EditorGUILayout.Space();

        var rect = GUILayoutUtility.GetLastRect();
        GUI.Box(new Rect(0, rect.y + 10f, EditorGUIUtility.currentViewWidth, 97f), GUIContent.none);

        int controlPointIndex = this.script.GetControlPointIndex(this.selectedTangentIndex);
        GUILayout.Label("Selected Tangent", EditorStyles.boldLabel);

        var wideMode = EditorGUIUtility.wideMode;
        EditorGUIUtility.wideMode = true;
        if (EditorGUIUtility.currentViewWidth < 332f) EditorGUIUtility.labelWidth = EditorGUIUtility.currentViewWidth - 217f;

        EditorGUI.BeginChangeCheck();
        Vector3 position = EditorGUILayout.Vector3Field("Position", script.GetTangentPoint(this.selectedTangentIndex));
        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(script, "Move Tangent");
            EditorUtility.SetDirty(script);
            script.SetTangentPoint(selectedTangentIndex, position);
        }
        EditorGUI.BeginChangeCheck();
        Spline.ControlPointMode mode = (Spline.ControlPointMode)
        EditorGUILayout.EnumPopup("Mode", script.GetControlPointMode(controlPointIndex));
        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(script, "Change Point Mode");
            script.SetTangentPointMode(this.selectedTangentIndex, mode);
            EditorUtility.SetDirty(script);
        }

        EditorGUIUtility.labelWidth = 0f;
        EditorGUIUtility.wideMode = wideMode;

        EditorGUILayout.Space();

        DrawControlPointTools(controlPointIndex);

        EditorGUILayout.Space();
    }

    private void OnSceneGUI()
    {
        UnityEngine.Profiling.Profiler.BeginSample("OnSceneGUI");
        script = target as Spline;
        if (Event.current.commandName == "UndoRedoPerformed")
        {
            script.SplineUpdate();
            return;
        }

        this.handleTransform = script.transform;

        Handles.color = Color.white;
        var zTest = Handles.zTest;
        for (int i = 0; i < script.ControlPointCount -1; ++i)
        {
            Handles.color = Color.white;
            Handles.zTest = zTest;
            Vector3 cp0 = DrawControlPoint(i);
            Vector3 cp1 = DrawControlPoint(i+1);
            Vector3 tp1 = DrawTangentHandle(i * 2);
            Vector3 tp2 = DrawTangentHandle(i * 2 + 1);

            Handles.zTest = UnityEngine.Rendering.CompareFunction.Greater;
            Handles.DrawBezier(cp0, cp1, tp1, tp2, zTestFailedColor, null, 2f);
            Handles.zTest = UnityEngine.Rendering.CompareFunction.LessEqual;
            Handles.DrawBezier(cp0, cp1, tp1, tp2, Color.white, null, 2f);
            cp0 = cp1;
        }
        Handles.zTest = zTest;

        if (script.ShowNormals) ShowNormals();
        if (script.ShowDirections) ShowDirections();
        if (script.ShowLengthSamples) ShowLengthSamples();
        //DrawTestPoint();
        UnityEngine.Profiling.Profiler.EndSample();
    }

    private Vector3 DrawControlPoint(int index)
    {
        OrientedPoint point = script.GetControlPoint(index);
        Vector3 position = handleTransform.TransformPoint(point.Position);
        Quaternion rotation = this.handleTransform.rotation * point.Rotation;
        float size = HandleUtility.GetHandleSize(position);
        if (index == 0) size *= 2f;
        Handles.color = modeColors[(int)script.GetControlPointMode(index)];
        if (Handles.Button(position, this.handleTransform.rotation, handleSize * size, pickSize * size, Handles.DotHandleCap))
        {
            this.selectedIndex = index;
            this.selectedTangentIndex = -1;
            this.globalRotationHandle = Quaternion.identity; //Reset global rotation handle
            Repaint();
        }
        if (selectedIndex == index)
        {
            EditorGUI.BeginChangeCheck();
            if(Tools.current == Tool.Rotate)
            {
                if (Tools.pivotRotation == PivotRotation.Local)
                {
                    rotation = Handles.DoRotationHandle(rotation, position);
                }
                else
                {
                    Quaternion after = Handles.DoRotationHandle(this.globalRotationHandle, position);
                    Quaternion diff = Quaternion.Inverse(this.globalRotationHandle) * after;
                    this.globalRotationHandle = after;
                    rotation = diff * rotation;
                }
            }
            else
            {
                this.globalRotationHandle = Quaternion.identity;
                var handleRotation = Tools.pivotRotation == PivotRotation.Local ? rotation : Quaternion.identity;
                position = Handles.DoPositionHandle(position, handleRotation);
            }

            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(script, "Move Point");
                EditorUtility.SetDirty(script);
                script.SetControlPoint(index, new OrientedPoint(handleTransform.InverseTransformPoint(position), Quaternion.Inverse(handleTransform.rotation) * rotation));
            }
        }

        return position;
    }

    private Vector3 DrawTangentHandle(int index)
    {
        Vector3 position = handleTransform.TransformPoint(this.script.GetTangentPoint(index));
        float size = HandleUtility.GetHandleSize(position);
        //Handles.color = (selectedIndex == this.script.GetControlPointIndex(index)) ? Color.white : Color.gray;
        Handles.color = new Color(1f, 1f, 1f, 0.5f);

        OrientedPoint cp = this.script.GetControlPoint(this.script.GetControlPointIndex(index));
        Vector3 cpPos = handleTransform.TransformPoint(cp.Position);
        Vector3 cpUp = handleTransform.TransformDirection(cp.Up);

        Handles.DrawLine(cpPos, position);

        if (Handles.Button(position, SceneView.lastActiveSceneView.rotation, handleSize * size, pickSize * size, Handles.RectangleHandleCap))
        {
            this.selectedTangentIndex = index;
            this.selectedIndex = -1;
            this.tangentPivotRotation = Tools.pivotRotation == PivotRotation.Local? PivotRotation.Global : PivotRotation.Local; //make sure tangent rotation gets reset
            Repaint();
        }
        if (this.selectedTangentIndex == index)
        {
            EditorGUI.BeginChangeCheck();

            if(this.tangentPivotRotation != Tools.pivotRotation)
            {
                this.tangentHandleRotation = Tools.pivotRotation == PivotRotation.Local ? Quaternion.LookRotation(position - cpPos, cpUp) : Quaternion.identity;
                this.tangentPivotRotation = Tools.pivotRotation;
            }

            //Quaternion handleRotation = Tools.pivotRotation == PivotRotation.Local ? this.tangentHandleRotation : Quaternion.identity;

            position = Handles.DoPositionHandle(position, tangentHandleRotation);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(script, "Move Point");
                EditorUtility.SetDirty(script);
                script.SetTangentPoint(index, handleTransform.InverseTransformPoint(position));
            }
        }
        return position;
    }

    private void ShowNormals()
    {
        Handles.color = Color.yellow;
        Vector3 point = script.GetPoint(0f);
        Handles.DrawLine(point, point + script.GetNormal(0f) * directionScale);
        int steps = lineSteps * script.SegmentCount;
        for (int i = 1; i <= steps; i++)
        {
            point = script.GetPoint(i / (float)steps);
            Handles.DrawLine(point, point + script.GetNormal(i / (float)steps) * directionScale);
        }
    }

    private void ShowDirections()
    {
        Handles.color = Color.blue;
        Vector3 point = script.GetPoint(0f);
        Handles.DrawLine(point, point + script.GetTangent(0f) * directionScale);
        int steps = lineSteps * script.SegmentCount;
        for (int i = 1; i <= steps; i++)
        {
            point = script.GetPoint(i / (float)steps);
            Handles.DrawLine(point, point + script.GetTangent(i / (float)steps) * directionScale);
        }
    }

    private void ShowLengthSamples()
    {
        Handles.color = Color.white;
        OrientedPoint point = script.GetLocalOrientedPoint(0f);

        int count = (this.script.LengthSamples * this.script.SegmentCount);
        for (int i = 0; i <= count; i++)
        {
            Vector3 position = handleTransform.TransformPoint(point.Position);
            Quaternion rotation = this.handleTransform.rotation * point.Rotation;
            point = script.GetLocalOrientedPoint((float)i / (float)count);
            Handles.RectangleHandleCap(0, position, rotation, directionScale, EventType.Repaint);
        }
    }
}
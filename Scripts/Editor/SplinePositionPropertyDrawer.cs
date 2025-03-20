using UnityEditor;
using UnityEngine;

// IngredientDrawer
[CustomPropertyDrawer(typeof(SplinePosition))]
public class SplinePositionPropertyDrawer : PropertyDrawer
{

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);

        // Draw label
        //position = EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), label);
        //EditorGUI.LabelField(position, label);
        //position.x += EditorGUIUtility.labelWidth;

        float halfWidth = (position.width- EditorGUIUtility.labelWidth) * 0.5f;
        float toggleWidth = Mathf.Max(halfWidth, 75f);
        float valueWidth = position.width - toggleWidth;

        // Calculate rects
        var positionRect = new Rect(position.x, position.y, valueWidth, position.height);
        var modeRect = new Rect(position.x + valueWidth, position.y, toggleWidth, position.height);

        // Draw fields - passs GUIContent.none to each so they are drawn without labels
        EditorGUI.PropertyField(positionRect, property.FindPropertyRelative("Position"), label);
        EditorGUI.PropertyField(modeRect, property.FindPropertyRelative("Mode"), GUIContent.none);

        EditorGUI.EndProperty();
    }
}
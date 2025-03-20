using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(Vector2Range))]
public class Vector2RangeDrawer : PropertyDrawer
{

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);

        position.height = EditorGUIUtility.singleLineHeight;

        var rangeTypeProperty = property.FindPropertyRelative("RangeType");
        //EditorGUI.PropertyField(position, rangeTypeProperty, label);
        EditorGUI.LabelField(position, label);

        var dropdownRect = position;
        dropdownRect.width = 160f;
        dropdownRect.x = dropdownRect.x + position.width - dropdownRect.width;
        dropdownRect.y += 1f;
        var style = new GUIStyle("ShurikenDropdown");
        style.alignment = TextAnchor.LowerRight;

        var rangeType = (Vector2Range.RangeTypes)rangeTypeProperty.enumValueIndex;
        var newRangeType = (Vector2Range.RangeTypes)EditorGUI.EnumPopup(dropdownRect, "", rangeType, style);
        if(newRangeType != rangeType) rangeTypeProperty.enumValueIndex = (int)newRangeType;

        var field = position;
        field.y += EditorGUIUtility.singleLineHeight;
        field.height = EditorGUIUtility.singleLineHeight;
        Rect rect1, rect2;

        switch (rangeType)
        {
            case Vector2Range.RangeTypes.VectorCurve:
                rect1 = field;
                rect1.width = field.width / 2f -1f;
                rect2 = rect1;
                rect2.x += rect1.width + 2f;

                EditorGUIUtility.labelWidth = 15f;

                EditorGUI.PropertyField(rect1, property.FindPropertyRelative("curveX"), new GUIContent("X"));
                EditorGUI.PropertyField(rect2, property.FindPropertyRelative("curveY"), new GUIContent("Y"));
                break;
            case Vector2Range.RangeTypes.RandomBetweenVectors:
                rect1 = field;
                rect1.width = field.width / 2f  - 10f;
                rect2 = rect1;
                rect2.x += rect1.width  + 20f;

                EditorGUI.PropertyField(rect1, property.FindPropertyRelative("constant1"), GUIContent.none);
                EditorGUI.PropertyField(rect2, property.FindPropertyRelative("constant2"), GUIContent.none);
                break;
            case Vector2Range.RangeTypes.Scalar:
                EditorGUIUtility.labelWidth = 25f;
                EditorGUI.PropertyField(field, property.FindPropertyRelative("constant1").FindPropertyRelative("x"), new GUIContent("XY"));
                break;
            case Vector2Range.RangeTypes.Curve:
                EditorGUIUtility.labelWidth = 25f;
                EditorGUI.PropertyField(field, property.FindPropertyRelative("curveX"), new GUIContent("XY"));
                break;
            case Vector2Range.RangeTypes.RandomBetweenScalars:
                rect1 = field;
                rect1.width = field.width / 2f - 2.5f;
                rect2 = rect1;
                rect2.x += rect1.width + 5f;

                EditorGUIUtility.labelWidth = 20f;

                EditorGUI.PropertyField(rect1, property.FindPropertyRelative("constant1").FindPropertyRelative("x"), new GUIContent("XY"));
                EditorGUI.PropertyField(rect2, property.FindPropertyRelative("constant2").FindPropertyRelative("x"), new GUIContent("XY"));
                break;
            default:
                EditorGUI.PropertyField(field, property.FindPropertyRelative("constant1"), GUIContent.none);
                break;
        }

        EditorGUI.EndProperty();
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        return EditorGUIUtility.singleLineHeight * 2f + 3f;
    }
}
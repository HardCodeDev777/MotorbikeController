#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace HardCodeDev.Attributes
{
    [CustomPropertyDrawer(typeof(NotInteractableAttribute))]
    public class NotInteractableDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            GUI.enabled = false;
            EditorGUI.PropertyField(position, property, label);
        }
    }
#endif
}
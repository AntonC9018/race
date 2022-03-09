using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace EngineCommon
{ 
    #if UNITY_EDITOR
    [CustomPropertyDrawer(typeof(ButtonAttribute))]
    public class ButtonDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            string methodName = (attribute as ButtonAttribute).MethodName;
            Object target = property.serializedObject.targetObject;
            var type = target.GetType();
            var method = type.GetMethod(methodName);

            bool ok = true;

            if (method == null)
            {
                EditorGUILayout.HelpBox($"Method {methodName} could not be found. Is it public?", MessageType.Warning);
                ok = false;
            }

            if (method.GetParameters().Length > 0)
            {
                EditorGUILayout.HelpBox($"Method {methodName} cannot have parameters.", MessageType.Warning);
                ok = false;
            }

            if (ok && GUI.Button(position, method.Name))
            {
                if (method.IsStatic)
                    method.Invoke(null, null);
                else
                    method.Invoke(target, null);
            }
        }
    }
    #endif
    
    public class ButtonAttribute : PropertyAttribute
    {
        public string MethodName { get; }
        public ButtonAttribute(string methodName)
        {
            MethodName = methodName;
        }
    }
}
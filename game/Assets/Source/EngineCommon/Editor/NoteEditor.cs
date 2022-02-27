using UnityEditor;
using UnityEngine;

namespace EngineCommon.Editor
{
    /// <summary>
    /// <see href="https://docs.unity3d.com/2021.2/Documentation/ScriptReference/Editor.html"/>
    /// <see cref="EngineCommon.NoteComponent"/>
    /// </summary>
    [CustomEditor(typeof(NoteComponent))]
    public class NoteEditor : UnityEditor.Editor
    {
        private SerializedProperty _text;

        private void OnEnable()
        {
            _text = serializedObject.FindProperty(nameof(NoteComponent.text));
        }

        public override void OnInspectorGUI()
        {
            var oldText = _text.stringValue;
            var newText = EditorGUILayout.TextArea(oldText);
            if (newText != oldText)
            {
                serializedObject.Update();
                _text.stringValue = newText;
                serializedObject.ApplyModifiedProperties();
            }
        }
    }
}
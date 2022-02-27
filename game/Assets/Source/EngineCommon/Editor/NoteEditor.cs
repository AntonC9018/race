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

        // NOTE:
        // I'm using IMGUI here and not the new ui toolkit, because editing a text field
        // with it is SOOO SLOW. I don't know what they did, and I don't want to go down
        // that rabbit hole right now, but changing the string content take like 0.2 seconds,
        // which is just insane.
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
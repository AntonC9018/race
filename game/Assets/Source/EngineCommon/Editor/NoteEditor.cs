using UnityEditor;
using UnityEngine.UIElements;

namespace EngineCommon.Editor
{
    /// <summary>
    /// <see href="https://docs.unity3d.com/2021.2/Documentation/ScriptReference/Editor.html"/>
    /// <see cref="EngineCommon.NoteComponent"/>
    /// </summary>
    [CustomEditor(typeof(NoteComponent))]
    public class NoteEditor : UnityEditor.Editor
    {
        public override VisualElement CreateInspectorGUI()
        {
            var visualElement = new VisualElement();
            var textArea = new TextField
            {
                multiline = true,
                bindingPath = nameof(NoteComponent.text),
            };
            visualElement.Add(textArea);
            return visualElement;
        }
    }
}
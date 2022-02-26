using UnityEngine;

namespace EngineCommon
{
    /// <summary>
    /// A component only needed to display notes in the inspector.
    /// It uses a custom editor (which has to be in a separate file,
    /// because of the well-known Unity quirk that classes inheriting
    /// from Unity's object have to be in a file with the same name).
    /// <see cref="EngineCommon.Editor.NoteEditor"/>
    /// </summary>
    public class NoteComponent : MonoBehaviour
    {
        #if UNITY_EDITOR
            public string text;
        #endif
    }
}

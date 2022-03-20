using UnityEngine;

namespace Race.Gameplay
{
    [System.Serializable]
    public struct CommonInitializationStuff
    {
        public KeyboardInputViewFactory inputViewFactory;

        /// <summary>
        /// Currently, only UI needs DI (dependency injection).
        /// </summary>
        public Transform diRootTransform;
    }
}
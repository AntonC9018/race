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

        /// <summary>
        /// </summary>
        // For now it's a concrete type just to be able to set it in the inspector.
        // The race manager calls it through interface already.
        public SameDurationDelay playerRespawnDelay;
    }
}
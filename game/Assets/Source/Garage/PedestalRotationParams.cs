using UnityEngine;

namespace Race
{
    // TODO: might be worth it to offer something like this:
    // https://gist.github.com/tomkail/ba4136e6aa990f4dc94e0d39ec6a058c
    [CreateAssetMenu(
        fileName = "New PedestalRotationParams",
        menuName = "PedestalRotationParams",
        // Second grouping.
        order = 51)]
    public class PedestalRotationParams : ScriptableObject
    {
        // TODO: figure out a reliable way to store components here??
        // public Transform thingToRotate;
        public float rotationScale = 0.5f;
    }
}

using UnityEngine;

namespace Race
{
    public class PedestalRotation : MonoBehaviour
    {
        [SerializeField] private Transform _thingToRotate;
        [SerializeField] private float _rotationScale;

        /// <summary>
        /// The direction doesn't necessarily have to be -1 or 1, 
        /// it can also indicate the amount by having a larger value.
        /// It will be scaled by the internal factor.
        /// </summary>
        public void Rotate(float direction)
        {
            var rotationAxis = Vector3.up;
            _thingToRotate.Rotate(rotationAxis, _rotationScale * direction);
        }
    }
}

using UnityEngine;
using UnityEngine.UI;

namespace Race.Gameplay
{
    /// <summary>
    /// I'm putting this in a separate object, because I think this won't be ever changing at runtime.
    /// These settings are for the 0 and visual max positions of the pips and of the needle.
    /// </summary>
    [CreateAssetMenu(
        fileName = "New " + nameof(RadialDisplayVisualConfiguration),
        menuName = nameof(RadialDisplayVisualConfiguration),
        // Second grouping.
        order = 51)]
    public class RadialDisplayVisualConfiguration : ScriptableObject
    {
        [InspectorName("Min Angle (degrees)")]
        [Range(-360, 360)]
        [SerializeField] private float _minAngle;
        
        [InspectorName("Max Angle (degrees)")]
        [Range(-360, 360)]
        [SerializeField] private float _maxAngle;

        // TODO: always store internally in radians.
        public float MaxAngle => Mathf.Deg2Rad * _maxAngle;
        public float MinAngle => Mathf.Deg2Rad * _minAngle;

        public float SignedAngleRangeLength => MaxAngle - MinAngle;
        public float AngleRangeLength => Mathf.Abs(SignedAngleRangeLength);
    }
}
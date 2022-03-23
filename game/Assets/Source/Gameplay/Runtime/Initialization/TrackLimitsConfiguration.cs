using UnityEngine;
using UnityEngine.Serialization;

namespace Race.Gameplay
{
    [CreateAssetMenu(
        fileName = "New " + nameof(TrackLimitsConfiguration),
        menuName = nameof(TrackLimitsConfiguration),
        // Second grouping.
        order = 51)]       
    public class TrackLimitsConfiguration : ScriptableObject
    {
        [FormerlySerializedAs("howMuchVisualWidthInActualWidth")]
        public float actualToVisualWidthRatio; 
    }
}
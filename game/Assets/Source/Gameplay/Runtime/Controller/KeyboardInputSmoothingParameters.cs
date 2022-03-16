using UnityEngine;

namespace Race.Gameplay
{
    [CreateAssetMenu(
        fileName = "New " + nameof(KeyboardInputSmoothingParameters),
        menuName = nameof(KeyboardInputSmoothingParameters),
        // Second grouping.
        order = 51)]
    public class KeyboardInputSmoothingParameters : ScriptableObject
    {
        // 1.5, 2
        public float maxSteeringAngleInputFactorChangePerSecond;

        // This one here is used to simulate gradual input specifically.
        // We might need another such engine-specific factor.
        public float maxMotorTorqueInputFactorChangePerSecond;
    }
}
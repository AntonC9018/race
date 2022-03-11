using UnityEngine;
using static EngineCommon.Assertions;
using System.Linq;

namespace Race.Gameplay
{
    [RequireComponent(typeof(CarController))]
    public class GearRatioCalculator : MonoBehaviour
    {
        [SerializeField] public float[] desiredOptimalSpeeds;
        
        [ContextMenuItem("Compute gear ratios", nameof(Compute))]
        [ContextMenuItem("Set the gear ratios on the controller", nameof(Set))]
        public bool RightClickMe;
        [SerializeField] public float[] computedGearRatios;

        public void Compute()
        {
            var colliderInfo = GetComponent<CarColliderInfoComponent>();
            float circumference = colliderInfo.CarColliderInfo.wheels[0].GetCircumference();

            computedGearRatios = new float[desiredOptimalSpeeds.Length];
            
            float optimalRPM = GetComponent<CarController>()._carEngineSpec.optimalRPM;

            for (int index = 0; index < computedGearRatios.Length; index++)
            {
                float s = desiredOptimalSpeeds[index];
                float s_m_per_min = s * 1000.0f / 60.0f;
                float rpm_at_s = s_m_per_min / circumference;
                float g = optimalRPM / rpm_at_s;
                computedGearRatios[index] = g;
            }
        }

        public void Set()
        {
            GetComponent<CarController>()._carEngineSpec.gears = computedGearRatios
                .Select(g => new GearInfo { gearRatio = g })
                .ToArray();
        }
    }


}
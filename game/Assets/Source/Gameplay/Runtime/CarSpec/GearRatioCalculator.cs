using UnityEngine;
using static EngineCommon.Assertions;
using System.Linq;

namespace Race.Gameplay
{
    #if UNITY_EDITOR
    
    /// <summary>
    /// Helper for computing gear rations from speeds.
    /// </summary>
    public class GearRatioCalculator : MonoBehaviour
    {
        [ContextMenuItem("Compute gear ratios", nameof(Compute))]
        [ContextMenuItem("Set the gear ratios on the controller", nameof(Set))]
        public bool RightClickMe;

        [SerializeField] public CarInfoComponent carInfo;
        [SerializeField] public float[] desiredOptimalSpeeds;
        
        [SerializeField] public float[] computedGearRatios;

        public void Compute()
        {
            float circumference = carInfo.colliderParts.wheels[0].collider.GetCircumference();

            computedGearRatios = new float[desiredOptimalSpeeds.Length];
            
            float optimalRPM = carInfo.template.baseSpec.engine.optimalRPM;

            for (int index = 0; index < computedGearRatios.Length; index++)
            {
                float s = desiredOptimalSpeeds[index];
                float s_m_per_min = s * 1000.0f / 60.0f;
                float rpm_at_s = s_m_per_min / circumference;
                float g = optimalRPM / rpm_at_s;
                computedGearRatios[index] = g;
            }

            Debug.Log("Computed.");
        }

        public void Set()
        {
            carInfo.template.baseSpec.transmission.gearRatios = computedGearRatios;
            Debug.Log("Ratios have been set.");
        }
    }
    #endif
}
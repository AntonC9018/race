using UnityEngine;
using static EngineCommon.Assertions;
using System.Linq;
using System;

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
        [ContextMenuItem("Compute speeds from the first speed (default ratio ratios)", nameof(ComputeSpeedsFromFirstGear))]
        public bool RightClickMe;

        [SerializeField] private CarInfoComponent _carInfo;
        [SerializeField] private float[] _desiredOptimalSpeeds;
        [SerializeField] private float[] _computedGearRatios;

        
        private static float GetSpeedFromGearRatio(float gearRatio, float circumference, float optimalRPM)
        {
            float wheelRPM = optimalRPM / gearRatio;
            float metersPerMinute = circumference * wheelRPM;
            float kmPerHour = metersPerMinute * 60.0f / 1000.0f;
            return kmPerHour;
        }

        public static float GetGearRatioFromSpeed(float speedKmPerHour, float circumference, float optimalRPM)
        {
            float metersPerMinute = speedKmPerHour / 60.0f * 1000.0f;
            float wheelRPM = metersPerMinute / circumference;
            float gearRatio = optimalRPM / wheelRPM;
            return gearRatio;
        }

        private static readonly float[] _ReferenceGearRatios = new float[]
        {
            -3.136f,
            3.136f,
            1.888f,
            1.330f,
            1,
            0.814f,
        };

        public void ComputeSpeedsFromFirstGear()
        {
            assert(_desiredOptimalSpeeds is not null);
            assert(_desiredOptimalSpeeds.Length > 0);

            float circumference = _carInfo.colliderParts.wheels[0].GetCircumference();
            float optimalRPM = _carInfo.template.baseSpec.engine.optimalRPM;

            int fixedGearIndex;
            float fixedGearRatio;
            if (_desiredOptimalSpeeds.Length == 1)
            {
                fixedGearIndex = 0;
                ref float speed = ref _desiredOptimalSpeeds[0];
                fixedGearRatio = GetGearRatioFromSpeed(speed, circumference, optimalRPM) / _ReferenceGearRatios[1];
                speed = -speed;
            }
            else
            {
                fixedGearIndex = 1;
                var speed = _desiredOptimalSpeeds[1];
                fixedGearRatio = GetGearRatioFromSpeed(speed, circumference, optimalRPM) / _ReferenceGearRatios[1];
            }

            Array.Resize(ref _desiredOptimalSpeeds, _ReferenceGearRatios.Length);
            
            for (int i = 0; i < _ReferenceGearRatios.Length; i++)
            {
                if (fixedGearIndex == i)
                    continue;
                float g = fixedGearRatio * _ReferenceGearRatios[i];
                _desiredOptimalSpeeds[i] = GetSpeedFromGearRatio(g, circumference, optimalRPM);
            }
        }

        public void Compute()
        {
            float circumference = _carInfo.colliderParts.wheels[0].GetCircumference();

            _computedGearRatios = new float[_desiredOptimalSpeeds.Length];
            
            float optimalRPM = _carInfo.template.baseSpec.engine.optimalRPM;

            for (int index = 0; index < _computedGearRatios.Length; index++)
            {
                float s = _desiredOptimalSpeeds[index];
                float s_m_per_min = s * 1000.0f / 60.0f;
                float rpm_at_s = s_m_per_min / circumference;
                float g = optimalRPM / rpm_at_s;
                _computedGearRatios[index] = g;
            }

            Debug.Log("Computed.");
        }

        public void Set()
        {
            _carInfo.template.baseSpec.transmission.gearRatios = _computedGearRatios;
            Debug.Log("Ratios have been set.");
        }
    }
    #endif
}
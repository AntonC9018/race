using UnityEngine;
using static EngineCommon.Assertions;

namespace Race.Gameplay
{
    public class Speedometer : MonoBehaviour
    {
        [SerializeField] private CarProperties _carProperties;
        [SerializeField] private RadialValueDisplay _valueDisplay;
        private float _maxSpeed;

        // It's easier to wire it up dynamically.
        void Awake()
        {
            _carProperties.OnDataModelInitialized.AddListener(OnDataModelInitialized);
            _carProperties.OnDrivingStateChanged.AddListener(OnDrivingStateChanged);
        }

        public void OnDataModelInitialized(CarProperties properties)
        {
            var maxSpeed = properties.DataModel.GetMaxSpeed();
            _maxSpeed = maxSpeed;

            var displayValueRange = new ValueRange
            {
                minValue = 0,
                maxValue = maxSpeed / 1000.0f * 3600.0f,
            };

            _valueDisplay.ResetPipsAndTextsToValues(displayValueRange, largePipGap: 10.0f);
        }

        public void OnDrivingStateChanged(CarProperties properties)
        {
            float speed = properties.DataModel.GetCurrentSpeed();
            float normalizedSpeed = speed / _maxSpeed;
            float clamped = Mathf.Clamp01(normalizedSpeed);
            _valueDisplay.ResetNeedleRotation(clamped);
        }
    }
}
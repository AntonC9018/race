using UnityEngine;
using static EngineCommon.Assertions;

namespace Race.Gameplay
{
    public class Speedometer : MonoBehaviour, InitializationHelper.ISetCarProperties
    {
        [SerializeField] private RadialValueDisplay _valueDisplay;
        private CarProperties _carProperties;
        private float _maxSpeed;

        public CarProperties CarProperties
        {
            set
            {
                if (_carProperties != null)
                {
                    _carProperties.OnDrivingStateChanged.RemoveListener(OnDrivingStateChanged);
                }

                {
                    _carProperties = value;
                    value.OnDrivingStateChanged.AddListener(OnDrivingStateChanged);
                    OnDataModelInitialized(value);
                }
            }
        }

        public void OnDataModelInitialized(CarProperties properties)
        {
            var maxSpeed = properties.DataModel.GetMaxSpeed();
            _maxSpeed = maxSpeed;
            Debug.Log(maxSpeed);

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
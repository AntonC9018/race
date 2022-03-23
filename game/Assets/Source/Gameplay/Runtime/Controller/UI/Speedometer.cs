using UnityEngine;
using static EngineCommon.Assertions;

namespace Race.Gameplay
{
    public class Speedometer : MonoBehaviour, IInitialize<CarProperties>
    {
        [SerializeField] private int _maxTextCount;
        [SerializeField] private RadialValueDisplay _valueDisplay;
        private CarProperties _carProperties;
        private float _maxSpeed;

        public void Initialize(CarProperties carProperties)
        {
            if (_carProperties != null)
                _carProperties.OnDrivingStateChanged.RemoveListener(OnDrivingStateChanged);

            carProperties.OnDrivingStateChanged.AddListener(OnDrivingStateChanged);
            OnDataModelInitialized(carProperties);

            _carProperties = carProperties;
        }

        public void OnDataModelInitialized(CarProperties properties)
        {
            var maxSpeed = properties.DataModel.GetMaxSpeed();
            _maxSpeed = maxSpeed;

            // Make it so that the pips are nicely spaced out, while not cluttering the view
            // and have them snap to nice numbers divisible by 10.
            float maxSpeedInKmH;
            float largePipGap;
            {
                float maxTextCount = _maxTextCount;
                const float valuePerTextBaseline = 10.0f;
                float pipsPerText = _valueDisplay._configuration.pipsPerText;
                var maxSpeedInKmHTemp = maxSpeed / 1000.0f * 3600.0f;
                float textCountFloat = maxSpeedInKmHTemp / valuePerTextBaseline;
                float desiredScale = textCountFloat / maxTextCount;
                float valuePerPipScale = Mathf.Ceil(desiredScale);
                
                float textGap = valuePerPipScale * valuePerTextBaseline;
                largePipGap = textGap / pipsPerText;
    
                // It might be more wrong than it used to be,
                // but the visual quality is more valuable here.
                maxSpeedInKmH = Mathf.Ceil(maxSpeedInKmHTemp / textGap) * textGap + largePipGap;
            }

            var displayValueRange = new ValueRange
            {
                minValue = 0,
                maxValue = maxSpeedInKmH,
            };

            _valueDisplay.ResetPipsAndTextsToValues(displayValueRange, largePipGap);
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
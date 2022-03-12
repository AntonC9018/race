using UnityEngine;
using static EngineCommon.Assertions;

namespace Race.Gameplay
{
    public class Speedometer : MonoBehaviour
    {
        [SerializeField] private CarProperties _carProperties;
        [SerializeField] private RadialValueDisplay _valueDisplay;
        private float _maxSpeed;

        void Awake()
        {
            var maxSpeed = _carProperties.DataModel.GetMaxSpeed();
            _maxSpeed = maxSpeed;

            var displayValueRange = new ValueRange
            {
                minValue = 0,
                maxValue = maxSpeed / 1000.0f * 3600.0f,
            };

            _valueDisplay.ResetPipsAndTextsToValues(displayValueRange, largePipGap: 10.0f);
            _carProperties.OnDrivingStateChanged.AddListener(OnDrivingStateChanged);
        }

        public void OnDrivingStateChanged(CarDataModel model)
        {
            float speed = model.GetCurrentSpeed();
            float normalizedSpeed = speed / _maxSpeed;
            float clamped = Mathf.Clamp01(normalizedSpeed);
            _valueDisplay.ResetNeedleRotation(clamped);
        }
    }
}
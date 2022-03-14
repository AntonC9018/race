using UnityEngine;
using static EngineCommon.Assertions;

namespace Race.Gameplay
{
    public class Tachometer : MonoBehaviour
    {
        [SerializeField] private CarProperties _carProperties;
        [SerializeField] private RadialValueDisplay _valueDisplay;
        public ValueRange _displayValueRange;

        void Awake()
        {
            ref readonly var engine = ref _carProperties.DataModel.Spec.engine;

            _displayValueRange = new ValueRange
            {
                minValue = engine.idleRPM,
                maxValue = engine.maxRPM,
            };

            _valueDisplay.ResetPipsAndTextsToValues(_displayValueRange, largePipGap: 500.0f);
            _carProperties.OnDrivingStateChanged.AddListener(OnDrivingStateChanged);
        }

        public void OnDrivingStateChanged(CarDataModel model)
        {
            float motorRPM = model.DrivingState.motorRPM;
            float normalizedRPM = (motorRPM - _displayValueRange.minValue) / _displayValueRange.Length;
            float clamped = Mathf.Clamp01(normalizedRPM);
            _valueDisplay.ResetNeedleRotation(clamped);
        }
    }
}
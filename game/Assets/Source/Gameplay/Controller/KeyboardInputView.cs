using System;
using EngineCommon;
using UnityEngine;
using static EngineCommon.Assertions;

namespace Race.Gameplay
{
    [System.Serializable]
    public struct KeyboardInputSmoothingParameters
    {
        // 1.5, 2
        public float maxSteeringAngleInputFactorChangePerSecond;

        // This one here is used to simulate gradual input specifically.
        // We might need another such engine-specific factor.
        public float maxMotorTorqueInputFactorChangePerSecond;
    }

    public class KeyboardInputView : MonoBehaviour, ICarInputView
    {
        [SerializeField] internal KeyboardInputSmoothingParameters _smoothingParameters;
        private CarControls.PlayerActions _player;
        private CarProperties _properties;

        public void Enable(CarProperties properties)
        {
            _player = new CarControls().Player;
            _player.Enable();
            _properties = properties;
        }

        public CarMovementInputValues Movement
        {
            get
            {
                CarMovementInputValues result;
                // In case of brake, we just apply the read amount.
                // This is different for motor torque, where we want to allow gradual changes.
                // Maybe?? I'm not sure. We might want that damping here too.
                result.Brakes = _player.Backward.ReadValue<float>();

                result.Forward = MathHelper.GetValueChangedByAtMost(
                    _properties.DataModel.DrivingState.motorTorqueInputFactor,
                    desiredValue: _player.Forward.ReadValue<float>(),
                    _smoothingParameters.maxMotorTorqueInputFactorChangePerSecond * Time.deltaTime);

                result.Turn = MathHelper.GetValueChangedByAtMost(
                    _properties.DataModel.DrivingState.steeringInputFactor,
                    desiredValue: _player.Turn.ReadValue<float>(),
                    // This one might be part of the controller tho,
                    // Because the amount a wheel can turn should be constrained.
                    _smoothingParameters.maxSteeringAngleInputFactorChangePerSecond * Time.deltaTime);

                return result;
            }
        }

        public bool Clutch => _player.Clutch.ReadValue<float>() > 0;

        public GearInputType Gear
        {
            get
            {
                if (_player.GearUp.WasPerformedThisFrame())
                    return GearInputType.GearUp;
                if (_player.GearDown.WasPerformedThisFrame())
                    return GearInputType.GearDown;
                return GearInputType.None;
            }
        }
    }
}
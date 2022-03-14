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
        [SerializeField] internal InputManager _inputManager;
        private CarControls.PlayerActions Player => _inputManager.CarControls.Player;
        private CarProperties _properties;

        public void Enable(CarProperties properties)
        {
            Player.Enable();
            _properties = properties;
        }

        public CarMovementInputValues Movement
        {
            get
            {
                // I'm a little uneasy about this one.
                // I guess we just say that the movement data in the driving state is only to change in 
                // FixedUpdate(), then this is fine.
                // But I still kind of dislike that we tie ourselves to the engine like this here,
                // even though we cannot get "pure" input data without the engine backing us up.
                float timeSinceLastInput = Time.fixedDeltaTime;

                CarMovementInputValues result;
                var player = Player;
                // In case of brake, we just apply the read amount.
                // This is different for motor torque, where we want to allow gradual changes.
                // Maybe?? I'm not sure. We might want that damping here too.
                result.Brakes = player.Backward.ReadValue<float>();

                result.Forward = MathHelper.GetValueChangedByAtMost(
                    _properties.DataModel.DrivingState.motorTorqueInputFactor,
                    desiredValue: player.Forward.ReadValue<float>(),
                    _smoothingParameters.maxMotorTorqueInputFactorChangePerSecond * timeSinceLastInput);

                result.Turn = MathHelper.GetValueChangedByAtMost(
                    _properties.DataModel.DrivingState.steeringInputFactor,
                    desiredValue: player.Turn.ReadValue<float>(),
                    // This one might be part of the controller tho,
                    // Because the amount a wheel can turn should be constrained.
                    _smoothingParameters.maxSteeringAngleInputFactorChangePerSecond * timeSinceLastInput);

                return result;
            }
        }

        public bool Clutch => Player.Clutch.ReadValue<float>() > 0;

        public GearInputType Gear
        {
            get
            {
                var player = Player;
                if (player.GearUp.WasPerformedThisFrame())
                    return GearInputType.GearUp;
                if (player.GearDown.WasPerformedThisFrame())
                    return GearInputType.GearDown;
                return GearInputType.None;
            }
        }
    }
}
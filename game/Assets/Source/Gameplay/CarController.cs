using System;
using UnityEngine;
using static EngineCommon.Assertions;

namespace Race.Gameplay
{
    [System.Serializable]
    public struct CarControlLimits
    {
        public float maxSteeringAngle;
        public float maxSteeringAngleInputFactorChangePerUpdate;

        // This one here is used to simulate gradual input specifically.
        // We might need another such engine-specific factor.
        public float maxMotorTorqueInputFactorChangePerUpdate;
    }

    [System.Serializable]
    public struct GearInfo
    {
        public float gearRatio;
    }

    [System.Serializable]
    public struct CarEngineSpec
    {
        // The car takes damage above this value (??).
        // Also used in calculations.
        public float maxRPM;
        public float maxTorque;

        // At this RPM, it produces the maximum torque.
        public float optimalRPM;

        public GearInfo[] gears;
    }

    public struct CarDrivingState
    {
        // The currently applied torque, normalized between 0 and 1.
        // Increases as the user holds down the forward key.
        // The actual torque applied to the wheel is going to be rescaled by this amount.
        public float currentTorqueInputFactor;
        public float currentSteeringInputFactor;

        // The gear from which to take the gear ratio for torque calcucations.
        public int currentGear;

        // Clutch allows switching gears.
        // The engine is detatched from wheels (does not produce torque) while the clutch is active.
        // TODO: might want a flags enum.
        public bool isClutch;
    }

    [RequireComponent(typeof(CarColliderInfoComponent))]
    public class CarController : MonoBehaviour
    {
        private CarPartsInfo _carColliderInfo;
        private CarControls _carControls;

        // For now, just serialize, but should get this from the car stats.
        [SerializeField] private CarControlLimits _carControlLimits;
        [SerializeField] private CarEngineSpec _carEngineSpec;
        private CarDrivingState _carDrivingState;
        


        void Awake()
        {
            _carColliderInfo = GetComponent<CarColliderInfoComponent>().CarColliderInfo;

            // TODO: get this from the outside.
            _carControls = new CarControls();
            _carControls.Player.Enable();

            assert(_carEngineSpec.gears is not null);
            assert(_carEngineSpec.gears.Length > 0);

            _carDrivingState = new CarDrivingState
            {
                currentGear = 0,
                currentTorqueInputFactor = 0,
                isClutch = false,
            };
        }

        void FixedUpdate()
        {
            static float ClampMagnitude(float desired, float min, float max)
            {
                float desiredChangeMagnitude = Mathf.Abs(desired);
                float actualChangeMagnitude = Mathf.Clamp(desiredChangeMagnitude, min, max);
                float actualChange = Mathf.Sign(desired) * actualChangeMagnitude;
                return actualChange;
            }

            static float GetNewInputFactor(float currentFactor, float input, float max)
            {
                float desiredChange = input - currentFactor;
                float actualChange = ClampMagnitude(desiredChange, 0, max);
                return currentFactor + actualChange;
            }


            var player = _carControls.Player;
            {
                float currentTorqueFactor = GetNewInputFactor(
                    _carDrivingState.currentTorqueInputFactor,
                    // TODO: this actually does not behave correctly?
                    // Lowering the input should make it stop even faster? idk.
                    input: player.ForwardBackward.ReadValue<float>(),
                    _carControlLimits.maxMotorTorqueInputFactorChangePerUpdate);

                float currentGearRatio = _carEngineSpec.gears[_carDrivingState.currentGear].gearRatio;

                float wheelRPM;
                {
                    float wheelRPMSum = 0;
                    Span<WheelLocation> referenceWheelLocations = stackalloc WheelLocation[2]
                    {
                        WheelLocation.BackRight,
                        WheelLocation.BackLeft,
                    };
                    foreach (var refWheelLocation in referenceWheelLocations)
                    {
                        float a = _carColliderInfo.GetWheel(refWheelLocation).collider.rpm;
                        Debug.Log($"Wheel at {refWheelLocation}: {a}");
                        wheelRPMSum += a;
                    }
                    wheelRPM = wheelRPMSum / referenceWheelLocations.Length;
                }
                float currentRPM = wheelRPM / 2 * currentGearRatio;
                float deviationFromOptimalRPM = Mathf.Abs(currentRPM - _carEngineSpec.optimalRPM) / _carEngineSpec.maxRPM;
                float scale = Mathf.Clamp(1 - deviationFromOptimalRPM, 0.2f, 1.0f);
                float maximumTorqueAtCurrentRPM = _carEngineSpec.maxTorque * scale;
                float torqueApplied = maximumTorqueAtCurrentRPM * currentTorqueFactor;

                _carDrivingState.currentTorqueInputFactor = currentTorqueFactor;

                _carColliderInfo.GetWheel(WheelLocation.BackLeft).collider.motorTorque = torqueApplied;
                _carColliderInfo.GetWheel(WheelLocation.BackRight).collider.motorTorque = torqueApplied;
            }

            {
                float currentSteeringFactor = GetNewInputFactor(
                    _carDrivingState.currentSteeringInputFactor,
                    input: player.Turn.ReadValue<float>(),
                    _carControlLimits.maxSteeringAngleInputFactorChangePerUpdate);
                float actualSteeringAngle = _carControlLimits.maxSteeringAngle * currentSteeringFactor;

                _carDrivingState.currentTorqueInputFactor = currentSteeringFactor;

                _carColliderInfo.GetWheel(WheelLocation.FrontLeft).collider.steerAngle = actualSteeringAngle;
                _carColliderInfo.GetWheel(WheelLocation.FrontRight).collider.steerAngle = actualSteeringAngle;
            }

            foreach (ref readonly var wheelInfo in _carColliderInfo.wheels.AsSpan())
            {
                wheelInfo.collider.GetWorldPose(out var position, out var rotation);
                wheelInfo.visualTransform.SetPositionAndRotation(position, rotation);
            }
        }
    }
}
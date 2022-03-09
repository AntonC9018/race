using System;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
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

        // The RPM's of wheels jump drastically.
        // This factor has been introduced to smooth out these jumps.
        // Represents the max scale that the RPM can jump in a single fixed update.
        public float maxRPMJumpPerUpdate;
    }

    [System.Serializable]
    public struct GearInfo
    {
        public float gearRatio;
    }

    [System.Serializable]
    public struct CarBrakesSpec
    {
        public float brakeTorque;
    }
    
    // TODO: engine / transmission spec?
    [System.Serializable]
    public struct CarEngineSpec
    {
        // The car takes damage above this value (??).
        // Also used in calculations.
        public float maxRPM;
        public float maxTorque;

        // At this RPM, it produces the maximum torque.
        public float optimalRPM;

        // Reaches this RPM while in clutch without force applied.
        public float idleRPM;

        public float minEfficiency;

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
        public int currentGearIndex;

        // The RPM recorded on the last frame.
        public float currentRMP;

        // TODO: max engine rpm gain per update.
        // TODO: max torque gain per update.

        // Clutch allows switching gears.
        // The engine is detatched from wheels (does not produce torque) while the clutch is active.
        // TODO: might want a flags enum.
        public bool isClutch;
    }

    [RequireComponent(typeof(CarColliderInfoComponent))]
    public class CarController : MonoBehaviour
    {
        // For now, just serialize, but should get this from the car stats.
        [SerializeField] private CarControlLimits _carControlLimits;
        [SerializeField] private CarEngineSpec _carEngineSpec;
        [SerializeField] private CarBrakesSpec _carBrakesSpec;
        [SerializeField] private WheelLocation[] _motorWheelLocations;
        [SerializeField] private WheelLocation[] _brakeWheelLocations;
        [SerializeField] private WheelLocation[] _steeringWheelLocations;

        private CarPartsInfo _carPartsInfo;
        private CarControls _carControls;
        private CarDrivingState _carDrivingState;


        void Awake()
        {
            var colliderInfo = GetComponent<CarColliderInfoComponent>();
            colliderInfo.AdjustCenterOfMass();

            _carPartsInfo = colliderInfo.CarColliderInfo;

            // TODO: get this from the outside.
            _carControls = new CarControls();
            _carControls.Player.Enable();

            assert(_carEngineSpec.gears is not null);
            assert(_carEngineSpec.gears.Length > 0);

            int firstPositiveGearIndex = -1;
            for (int i = 0; i < _carEngineSpec.gears.Length; i++)
            {
                if (_carEngineSpec.gears[i].gearRatio > 0)
                {
                    firstPositiveGearIndex = i;
                    break;
                }
            }

            _carDrivingState = new CarDrivingState
            {
                currentGearIndex = firstPositiveGearIndex,
                currentTorqueInputFactor = 0,
                currentSteeringInputFactor = 0,
                currentRMP = 0,
                isClutch = false,
            };

            assert(_motorWheelLocations is not null);
            assert(_brakeWheelLocations is not null);
            assert(_steeringWheelLocations is not null);
        }

        void OnGUI()
        {
            GUILayout.BeginVertical();
            GUI.color = Color.black;
            GUILayout.Label($"currentTorqueInputFactor: {_carDrivingState.currentTorqueInputFactor}");
            GUILayout.Label($"currentSteeringInputFactor: {_carDrivingState.currentSteeringInputFactor}");
            GUILayout.Label($"currentGearIndex: {_carDrivingState.currentGearIndex}");
            GUILayout.Label($"currentRMP: {_carDrivingState.currentRMP}");

            var rigidbody = GetComponent<Rigidbody>();
            var speed = rigidbody.velocity.magnitude;
            GUILayout.Label($"speed: {speed} m/s, {speed / 1000 * 3600} km/h");

            GUILayout.EndVertical();
        }

        void FixedUpdate()
        {
            var player = _carControls.Player;

            bool isClutch;
            {
                isClutch = player.Clutch.ReadValue<float>() > 0;
                if (isClutch)
                {
                    if (player.GearUp.WasPerformedThisFrame()
                        && _carDrivingState.currentGearIndex + 1 < _carEngineSpec.gears.Length)
                    {
                        _carDrivingState.currentGearIndex += 1;
                    }
                    
                    else if (player.GearDown.WasPerformedThisFrame()
                        && _carDrivingState.currentGearIndex > 0)
                    {
                        _carDrivingState.currentGearIndex -= 1;
                    }
                }
                _carDrivingState.isClutch = isClutch;
            }

            // Acceleration
            {
                // TODO: Braking should be handled differently (replacing the current input entirely? or just a different button?)
                float input = player.ForwardBackward.ReadValue<float>();
                float currentTorqueFactor = GetNewInputFactor(
                    _carDrivingState.currentTorqueInputFactor,
                    // TODO: this actually does not behave correctly?
                    // Lowering the input should make it stop even faster? idk.
                    input,
                    _carControlLimits.maxMotorTorqueInputFactorChangePerUpdate);

                // RPM between engine and the wheels.
                float currentRPM;

                // Braking, not implemented yet.
                if (input < 0)
                {
                    currentRPM = 0;
                    
                    foreach (var brakeWheelLocation in _brakeWheelLocations)
                        _carPartsInfo.GetWheel(brakeWheelLocation).collider.brakeTorque = _carBrakesSpec.brakeTorque;
                }
                else
                {
                    foreach (var brakeWheelLocation in _brakeWheelLocations)
                        _carPartsInfo.GetWheel(brakeWheelLocation).collider.brakeTorque = 0;
                }

                if (!isClutch)
                {
                    float currentGearRatio = _carEngineSpec.gears[_carDrivingState.currentGearIndex].gearRatio;
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
                            float a = _carPartsInfo.GetWheel(refWheelLocation).collider.rpm;
                            wheelRPMSum += a;
                        }
                        wheelRPM = wheelRPMSum / referenceWheelLocations.Length;
                    }

                    {
                        // If less than idle, and not enough acceleration is being applied, could stall?
                        float currentDesiredRPM = wheelRPM / 2 * currentGearRatio;
                        float change = currentDesiredRPM - _carDrivingState.currentRMP;
                        float a = ClampMagnitude(change, 0, _carControlLimits.maxRPMJumpPerUpdate);
                        currentRPM = _carDrivingState.currentRMP + a;
                    }

                    // lagrange polinomial
                    float currentEngineEfficiency;
                    {
                        // float a = currentRPM * (_carEngineSpec.maxRPM - currentRPM);
                        // float b = _carEngineSpec.optimalRPM * (_carEngineSpec.maxRPM - _carEngineSpec.optimalRPM);

                        // We lose much precision by just dividing, so we should divide twice at least (I think).
                        // float currentClamped = Mathf.Clamp(currentRPM, 0, _carEngineSpec.maxRPM);
                        float a = currentRPM / _carEngineSpec.optimalRPM;
                        float b = (_carEngineSpec.maxRPM - currentRPM) / (_carEngineSpec.maxRPM - _carEngineSpec.optimalRPM);
                        float c = Mathf.Clamp01(a * b);

                        const float maxEfficiency = 1.0f;
                        currentEngineEfficiency = Mathf.Lerp(_carEngineSpec.minEfficiency, maxEfficiency, c);
                        Debug.Log("Efficiency: " + currentEngineEfficiency);
                    }

                    float torqueApplied = _carEngineSpec.maxTorque * currentEngineEfficiency * currentTorqueFactor
                        // In case the current gear ratio is negative, we're in a reverse gear.
                        // In this case, the motor's work should go in the other direction.
                        * Mathf.Sign(currentGearRatio);

                    foreach (var motorWheelLocation in _motorWheelLocations)
                        _carPartsInfo.GetWheel(motorWheelLocation).collider.motorTorque = torqueApplied;
                }
                else
                {
                    // While in clutch, the currently pressed amount just increases or descreases the engine RPM,
                    // while not actually affecting anything else.
                    float currentDesiredRPM = Mathf.Lerp(_carEngineSpec.idleRPM, _carEngineSpec.maxRPM, currentTorqueFactor);
                    float change = currentDesiredRPM - _carDrivingState.currentRMP;
                    float a = ClampMagnitude(change, 0, _carControlLimits.maxRPMJumpPerUpdate);
                    currentRPM = _carDrivingState.currentRMP + a;
                }

                _carDrivingState.currentTorqueInputFactor = currentTorqueFactor;
                _carDrivingState.currentRMP = currentRPM;
            }

            // Steering
            {
                float currentSteeringFactor = GetNewInputFactor(
                    _carDrivingState.currentSteeringInputFactor,
                    input: player.Turn.ReadValue<float>(),
                    _carControlLimits.maxSteeringAngleInputFactorChangePerUpdate);
                float actualSteeringAngle = _carControlLimits.maxSteeringAngle * currentSteeringFactor;

                _carDrivingState.currentSteeringInputFactor = currentSteeringFactor;

                foreach (var steeringWheelLocation in _steeringWheelLocations)
                    _carPartsInfo.GetWheel(steeringWheelLocation).collider.steerAngle = actualSteeringAngle;
            }

            // Visuals
            foreach (ref readonly var wheelInfo in _carPartsInfo.wheels.AsSpan())
            {
                wheelInfo.collider.GetWorldPose(out var position, out var rotation);
                wheelInfo.visualTransform.SetPositionAndRotation(position, rotation);
            }

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
        }
    }
}
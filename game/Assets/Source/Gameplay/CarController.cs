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
    public struct CarEngineSpec
    {
        // The car takes damage above this value (??).
        // Also used in calculations.
        public float maxRPM;
        public float maxTorque;

        // At this RPM, it produces the maximum torque.
        public float optimalRPM;

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
        
        private CarPartsInfo _carColliderInfo;
        private CarControls _carControls;
        private CarDrivingState _carDrivingState;

        void Awake()
        {
            var colliderInfo = GetComponent<CarColliderInfoComponent>();
            colliderInfo.AdjustCenterOfMass();

            _carColliderInfo = colliderInfo.CarColliderInfo;

            // TODO: get this from the outside.
            _carControls = new CarControls();
            _carControls.Player.Enable();

            assert(_carEngineSpec.gears is not null);
            assert(_carEngineSpec.gears.Length > 0);

            _carDrivingState = new CarDrivingState
            {
                currentGearIndex = 0,
                currentTorqueInputFactor = 0,
                isClutch = false,
            };
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
                float input = player.ForwardBackward.ReadValue<float>();
                float currentTorqueFactor = GetNewInputFactor(
                    _carDrivingState.currentTorqueInputFactor,
                    // TODO: this actually does not behave correctly?
                    // Lowering the input should make it stop even faster? idk.
                    input,
                    _carControlLimits.maxMotorTorqueInputFactorChangePerUpdate);

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
                        float a = _carColliderInfo.GetWheel(refWheelLocation).collider.rpm;
                        wheelRPMSum += a;
                    }
                    wheelRPM = wheelRPMSum / referenceWheelLocations.Length;
                }
                float currentRPM;
                {
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

                float torqueApplied = _carEngineSpec.maxTorque * currentEngineEfficiency * currentTorqueFactor;

                _carDrivingState.currentTorqueInputFactor = currentTorqueFactor;
                _carDrivingState.currentRMP = currentRPM;

                _carColliderInfo.GetWheel(WheelLocation.BackLeft).collider.motorTorque = torqueApplied;
                _carColliderInfo.GetWheel(WheelLocation.BackRight).collider.motorTorque = torqueApplied;
            }

            {
                float currentSteeringFactor = GetNewInputFactor(
                    _carDrivingState.currentSteeringInputFactor,
                    input: player.Turn.ReadValue<float>(),
                    _carControlLimits.maxSteeringAngleInputFactorChangePerUpdate);
                float actualSteeringAngle = _carControlLimits.maxSteeringAngle * currentSteeringFactor;

                _carDrivingState.currentSteeringInputFactor = currentSteeringFactor;

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
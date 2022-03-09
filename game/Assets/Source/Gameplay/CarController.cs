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
        public float maxSteeringAngleInputFactorChangePerSecond;

        // This one here is used to simulate gradual input specifically.
        // We might need another such engine-specific factor.
        public float maxMotorTorqueInputFactorChangePerSecond;

        // The RPM's of wheels jump drastically.
        // This factor has been introduced to smooth out these jumps.
        // Represents the max scale that the RPM can jump in a second.
        public float maxWheelRPMJumpPerSecond;
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

        // Needed so that it can move at all from stationary position.
        public float minEfficiency;

        // These two are only relevant when the clutch is applied.
        // Otherwise the RPM correlates to wheel RPM.
        public float maxIdleRPMIncreasePerSecond;
        public float maxIdleRPMDecreasePerSecond;

        public GearInfo[] gears;
    }

    public struct CarDrivingState
    {
        // The currently applied torque, normalized between 0 and 1.
        // Increases as the user holds down the forward key.
        // The actual torque applied to the wheel is going to be rescaled by this amount.
        public float motorTorqueInputFactor;
        // public float brakeTorqueInputFactor;
        public float steeringInputFactor;

        // The gear from which to take the gear ratio for torque calcucations.
        public int gearIndex;

        public float motorRPM;
        // The recorded wheel RPM (with damping applied).
        public float wheelRPM;

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
            _carDrivingState.gearIndex = firstPositiveGearIndex;

            assert(_motorWheelLocations is not null);
            assert(_brakeWheelLocations is not null);
            assert(_steeringWheelLocations is not null);
        }

        void OnGUI()
        {
            GUILayout.BeginVertical();
            GUI.color = Color.black;
            GUILayout.Label($"torqueInputFactor: {_carDrivingState.motorTorqueInputFactor}");
            GUILayout.Label($"steeringInputFactor: {_carDrivingState.steeringInputFactor}");
            GUILayout.Label($"gearIndex: {_carDrivingState.gearIndex}");
            GUILayout.Label($"motorRPM: {_carDrivingState.motorRPM}");
            GUILayout.Label($"wheelRPM: {_carDrivingState.wheelRPM}");

            var rigidbody = GetComponent<Rigidbody>();
            var speed = rigidbody.velocity.magnitude;
            GUILayout.Label($"speed: {speed} m/s, {speed / 1000 * 3600} km/h");

            GUILayout.EndVertical();
        }

        void Update()
        {
            var player = _carControls.Player;

            if (_carDrivingState.isClutch)
            {
                if (player.GearUp.WasPerformedThisFrame()
                    && _carDrivingState.gearIndex + 1 < _carEngineSpec.gears.Length)
                {
                    // Do we need the old state for ui logic?
                    _carDrivingState.gearIndex += 1;
                }
                
                else if (player.GearDown.WasPerformedThisFrame()
                    && _carDrivingState.gearIndex > 0)
                {
                    _carDrivingState.gearIndex -= 1;
                }
            }
        }

        void FixedUpdate()
        {
            var player = _carControls.Player;

            // Not done:
            // 1. timeout on gear switching and the clutch becoming active.
            // 2. possibility of stalling the engine by driving in wrong gear.
            // 3. block on reverse gear while driving forward (and the other way).
            // 4. gradual braking.
            // 5. better presentation.

            bool isClutch;
            {
                isClutch = player.Clutch.ReadValue<float>() > 0;
                _carDrivingState.isClutch = isClutch;
            }

            float wheelRPM;
            {
                float wheelRPMSum = 0;
                foreach (ref var wheelInfo in _carPartsInfo.wheels.AsSpan())
                    wheelRPMSum += wheelInfo.collider.rpm;
                float recordedWheelRPM = wheelRPMSum / _carPartsInfo.wheels.Length;
                
                // `maxWheelRPMJumpPerSecond` is needed to counteract the fact that the wheel RPM
                // that the Unity provides jumps up and down drastically, so we damp it here manually.
                float maxAllowedChange = _carControlLimits.maxWheelRPMJumpPerSecond * Time.deltaTime;
                wheelRPM = GetValueChangedByAtMost(_carDrivingState.wheelRPM, recordedWheelRPM, maxAllowedChange);
            }

            // Brakes
            {
                float input = player.Backward.ReadValue<float>();
                // In case of brake, we just apply the read amount.
                // This is different for motor torque, where we want to allow gradual changes.
                // Maybe?? I'm not sure. We might want that damping here too.
                float breakFactor = input;
                float appliedBreakTorque = breakFactor * _carBrakesSpec.brakeTorque;
                foreach (var brakeWheelLocation in _brakeWheelLocations)
                    _carPartsInfo.GetWheel(brakeWheelLocation).collider.brakeTorque = appliedBreakTorque;
            }

            // Acceleration
            {
                float input = player.Forward.ReadValue<float>();
                float motorTorqueFactor = GetValueChangedByAtMost(
                    _carDrivingState.motorTorqueInputFactor, input,
                    _carControlLimits.maxMotorTorqueInputFactorChangePerSecond * Time.deltaTime);

                float motorRPM;
                float motorTorqueApplied;

                // Clutch means the engine is detached from the wheels.
                // Then it follows a completely different se of rules.
                if (!isClutch)
                {
                    float gearRatio = _carEngineSpec.gears[_carDrivingState.gearIndex].gearRatio;
                    // RPM between the engine and the wheels.
                    // We don't do any damping here either (at least for now).
                    float desiredMotorRPM = wheelRPM / 2 * gearRatio;

                    // The RPM here should change instantly (probably).
                    // motorRPM = GetMotorRPM(desiredMotorRPM, _carEngineSpec, _carDrivingState.motorRMP);
                    motorRPM = desiredMotorRPM;
                    
                    // The idea is that the engine efficiency peaks when it's at the optimal RPM.
                    // It dies down towards the edges (0 and `maxRPM` for the wheels).
                    // We clamp it to at least `minEfficiency` so that the car can move e.g. from stationary position.
                    float engineEfficiency;
                    {
                        // float a = currentRPM * (_carEngineSpec.maxRPM - currentRPM);
                        // float b = _carEngineSpec.optimalRPM * (_carEngineSpec.maxRPM - _carEngineSpec.optimalRPM);

                        // The reason I'm using these formulas instead of the one above is because
                        // here a and b are closer to 1, so less loss of precision should occur.
                        // float currentClamped = Mathf.Clamp(currentRPM, 0, _carEngineSpec.maxRPM);

                        // TODO:
                        // Math! This does not work properly, because a second order approximation
                        // provided by the lagrangian polynomial is not good enough.
                        // We have the additional restriction that the second derivative is always negative
                        // (the function's derivative is strictly decreasing)
                        // and that the point at the optimal RPM is the extreme (the derivative is 0).
                        // 4 equations, 1 inequality -> 5 parameters, one of which is kind of free.
                        // So the approximation should ideally be a polynomial with 5 coefficients.
                        //
                        // Could also just linearly interpolate the 2 segments, but that's not going to be cool.
                        
                        float a = motorRPM / _carEngineSpec.optimalRPM;
                        float b = (_carEngineSpec.maxRPM - motorRPM) / (_carEngineSpec.maxRPM - _carEngineSpec.optimalRPM);
                        Debug.Log("motor rpm: " + motorRPM);
                        Debug.Log("a: " + a);
                        Debug.Log("b: " + b);

                        // May go beyond maxRPM and can even go negative if driving in reverse gear forwards
                        // (which should not really be allowed at all, but going beyond maxRPM is definitely possible).
                        // In such cases, the product will be below 0. The product can never go above 1 though.
                        // Nope! I think due to floating point errors it does get above 1 sometimes.
                        float c = Mathf.Clamp01(a * b);
                        // float c = a * b;
                        // if (c < 0)
                        //     c = 0;
                        Debug.Log("c: " + c);

                        const float maxEfficiency = 1.0f;
                        // Unclamped because we've contrained the c already.
                        engineEfficiency = Mathf.LerpUnclamped(_carEngineSpec.minEfficiency, maxEfficiency, c);
                        Debug.Log("Efficiency: " + engineEfficiency);
                    }

                    motorTorqueApplied = _carEngineSpec.maxTorque * engineEfficiency * motorTorqueFactor
                        // In case the current gear ratio is negative, we're in a reverse gear.
                        // In this case, the motor's work should go in the other direction.
                        * Mathf.Sign(gearRatio);
                }
                else
                {
                    // While in clutch, the currently pressed amount just increases or descreases the engine RPM,
                    // while not actually affecting anything else.
                    {
                        float desiredMotorRPM = Mathf.Lerp(_carEngineSpec.idleRPM, _carEngineSpec.maxRPM, motorTorqueFactor);
                        motorRPM = GetMotorRPM(desiredMotorRPM, _carEngineSpec, _carDrivingState.motorRPM);

                        // This one is only relevant in the clutch case, otherwise it can change instantly.
                        static float GetMotorRPM(float desiredMotorRPM, in CarEngineSpec engine, float motorRPM)
                        {
                            if (desiredMotorRPM > motorRPM)
                            {
                                float maxAllowedChange = engine.maxIdleRPMIncreasePerSecond * Time.deltaTime;
                                return Mathf.Min(desiredMotorRPM, motorRPM + maxAllowedChange);
                            }
                            else
                            {
                                float maxAllowedChange = engine.maxIdleRPMDecreasePerSecond * Time.deltaTime;
                                return Mathf.Max(desiredMotorRPM, motorRPM - maxAllowedChange);
                            }
                        }
                    }

                    // When the engine is detached from the wheels, they must not get moved by it at all.
                    motorTorqueApplied = 0;
                }

                _carDrivingState.wheelRPM = wheelRPM;
                _carDrivingState.motorTorqueInputFactor = motorTorqueFactor;
                _carDrivingState.motorRPM = motorRPM;

                foreach (var motorWheelLocation in _motorWheelLocations)
                    _carPartsInfo.GetWheel(motorWheelLocation).collider.motorTorque = motorTorqueApplied;
            }

            // Steering
            {
                float currentSteeringFactor = GetValueChangedByAtMost(
                    _carDrivingState.steeringInputFactor,
                    desiredValue: player.Turn.ReadValue<float>(),
                    _carControlLimits.maxSteeringAngleInputFactorChangePerSecond * Time.deltaTime);
                float actualSteeringAngle = _carControlLimits.maxSteeringAngle * currentSteeringFactor;

                _carDrivingState.steeringInputFactor = currentSteeringFactor;

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

            static float GetValueChangedByAtMost(float currentValue, float desiredValue, float maxChangeAllowed)
            {
                float desiredChange = desiredValue - currentValue;
                float actualChange = ClampMagnitude(desiredChange, 0, maxChangeAllowed);
                return currentValue + actualChange;
            }
        }
    }
}
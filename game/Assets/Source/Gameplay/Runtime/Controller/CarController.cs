#define COMPUTE_RPM_FROM_WHEELS

#if COMPUTE_RPM_FROM_WHEELS
#else
    #define COMPUTE_RPM_FROM_RIGIDBODY
#endif

using System;
using EngineCommon;
using UnityEngine;
using static EngineCommon.Assertions;

namespace Race.Gameplay
{
    [System.Serializable]
    public struct DataSmoothingParameters
    {
        // NOTE: Uncommented so that it does not reset in the inspector when the version is changed.
        // #if COMPUTE_RPM_FROM_WHEELS
            // The RPM's of wheels jump drastically.
            // This factor has been introduced to smooth out these jumps.
            // Represents the max scale that the RPM can jump in a second.
            public float maxWheelRPMJumpPerSecond;
        // #endif
    }

    [System.Serializable]
    public struct CarDrivingState
    {
        /// <summary>
        /// The currently applied torque, normalized between 0 and 1.
        /// Increases e.g. as the user holds down the forward key.
        /// The actual torque applied to the wheel is going to be rescaled by this amount.
        /// "throttle" is the official term for this, I think.
        /// </summary>
        public float motorTorqueInputFactor;
        public float brakeTorqueInputFactor;

        /// <summary>
        /// </summary>
        public float steeringInputFactor;

        /// <summary>
        /// The gear from which to take the gear ratio for torque calculations.
        /// </summary>
        public int gearIndex;

        /// <summary>
        /// </summary>
        public float motorRPM;

        /// <summary>
        /// The recorded wheel RPM (possibly with damping applied).
        /// </summary>
        public float wheelRPM;

        /// <summary>
        /// The engine is detatched from wheels (does not produce torque) while the clutch is active.
        /// Clutch allows switching gears.
        /// TODO: might want a flags enum.
        /// </summary>
        public bool isClutch;
    }

    public interface ICarInputView
    {
        void Enable(CarProperties properties);

        // Could split these into more interfaces if needed later, but I highly doubt it.
        CarMovementInputValues Movement { get; }
        bool Clutch { get; }
        GearInputType Gear { get; }
    }
    
    [System.Serializable]
    public struct CarMovementInputValues
    {
        public float Forward;
        public float Brakes;
        public float Turn;
    }

    public enum GearInputType
    {
        None,
        GearUp,
        GearDown,
    }

    public class CarController : MonoBehaviour
    {
        // For now, just serialize, but should get this from the car stats.
        [SerializeField] internal CarProperties _carProperties;
        [SerializeField] internal DataSmoothingParameters _dataSmoothing;
        private ICarInputView _inputView;

        void Awake()
        {
            // For now just get it from the object, but may want to do something fancy here later.
            _inputView = GetComponent<ICarInputView>();
            assert(_inputView != null, "Input view has not been provided.");
        }

        void Start()
        {
            _inputView.Enable(_carProperties);
        }

        void OnGUI()
        {
            var rigidbody = GetComponent<Rigidbody>();
            var speed = rigidbody.velocity.magnitude;
            var dataModel = _carProperties.DataModel;

            GUILayout.BeginVertical();
            GUI.color = Color.white;
            ref readonly var drivingState = ref dataModel.DrivingState;
            GUILayout.Label($"torqueInputFactor: {drivingState.motorTorqueInputFactor}");
            GUILayout.Label($"steeringInputFactor: {drivingState.steeringInputFactor}");
            GUILayout.Label($"gearIndex: {drivingState.gearIndex}");
            GUILayout.Label($"motorRPM: {drivingState.motorRPM}");
            GUILayout.Label($"wheelRPM: {drivingState.wheelRPM}");

            float speedMetersPerSecond = rigidbody.velocity.magnitude;
            float speedMetersPerMinute = speed * 60.0f;
            float circumference = dataModel.ColliderParts.wheels[0].collider.GetCircumference();
            float expectedWheelRPM = speedMetersPerMinute / circumference;

            GUILayout.Label($"expectedWheelRPM: {expectedWheelRPM}");
            GUILayout.Label($"expectedMotorRPM: {expectedWheelRPM * dataModel.Spec.transmission.gearRatios[drivingState.gearIndex]}");


            GUILayout.Label($"speed: {speed} m/s, {speed / 1000 * 3600} km/h");

            GUILayout.EndVertical();
        }

        void Update()
        {
            ref var drivingState = ref _carProperties.DataModel.DrivingState;

            // TODO: a separate event for these??
            if (drivingState.isClutch)
            {
                var gearInput = _inputView.Gear;

                if (gearInput == GearInputType.GearUp
                    && drivingState.gearIndex + 1 < _carProperties.DataModel.Spec.transmission.gearRatios.Length)
                {
                    // Do we need the old state for ui logic?
                    drivingState.gearIndex += 1;
                }
                
                else if (gearInput == GearInputType.GearDown
                    && drivingState.gearIndex > 0)
                {
                    drivingState.gearIndex -= 1;
                }
            }
        }

        void FixedUpdate()
        {
            ref readonly var colliderParts = ref _carProperties.DataModel.ColliderParts;
            ref readonly var spec = ref _carProperties.DataModel.Spec;
            ref var drivingState = ref _carProperties.DataModel.DrivingState;

            // Not done:
            // 1. timeout on gear switching and the clutch becoming active.
            // 2. possibility of stalling the engine by driving in wrong gear.
            // 3. block on reverse gear while driving forward (and the other way).
            // 4. gradual braking.
            // 5. better presentation.

            bool isClutch;
            {
                isClutch = _inputView.Clutch;
                drivingState.isClutch = isClutch;
            }

            float wheelRPM;
            {
                #if COMPUTE_RPM_FROM_WHEELS
                    float wheelRPMSum = 0;
                    foreach (ref var wheelInfo in colliderParts.wheels.AsSpan())
                        wheelRPMSum += wheelInfo.collider.rpm;
                    float recordedWheelRPM = wheelRPMSum / colliderParts.wheels.Length;
                    wheelRPM = recordedWheelRPM;
                    
                    // `maxWheelRPMJumpPerSecond` is needed to counteract the fact that the wheel RPM
                    // that the Unity provides jumps up and down drastically, so we damp it here manually.
                    float maxAllowedChange = _dataSmoothing.maxWheelRPMJumpPerSecond * Time.fixedDeltaTime;
                    wheelRPM = MathHelper.GetValueChangedByAtMost(drivingState.wheelRPM, recordedWheelRPM, maxAllowedChange);
                #else
                    float speedMetersPerSecond = colliderParts.Rigidbody.velocity.magnitude;
                    float speedMetersPerMinute = speedMetersPerSecond * 60.0f;
                    float circumference = colliderParts.wheels[0].GetCircumference();

                    // Computing the RPM directly seems ok 
                    wheelRPM = speedMetersPerMinute / circumference;
                #endif
            }

            var movementInputs = _inputView.Movement;

            // Brakes
            {
                float breakFactor = movementInputs.Brakes;
                float appliedBreakTorque = breakFactor * spec.brakes.maxTorque;
                drivingState.brakeTorqueInputFactor = appliedBreakTorque;
                foreach (var brakeWheelLocation in spec.brakeWheelLocations)
                    colliderParts.GetWheel(brakeWheelLocation).collider.brakeTorque = appliedBreakTorque;
            }

            // Acceleration
            {
                float motorRPM;
                float motorTorqueApplied;
                ref readonly var engine = ref spec.engine;

                // Clutch means the engine is detached from the wheels.
                // Then it follows a completely different se of rules.
                if (!isClutch)
                {
                    float gearRatio = spec.transmission.gearRatios[drivingState.gearIndex];
                    // RPM between the engine and the wheels.
                    // We don't do any damping here either (at least for now).
                    float desiredMotorRPM = wheelRPM * gearRatio;

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

                        // For now I'm just going to linearly interpolate the 2 segments,
                        // but I'd prefer a continuous function here.
                        // The algebra gets pretty messy, see `/concepts/engine_efficiency_equation`


                        /*
                        // Lagrangian second order approximation, does not have the desired properties.
                        float a = motorRPM / _carEngineSpec.optimalRPM;
                        float b = (_carEngineSpec.maxRPM - motorRPM) / (_carEngineSpec.maxRPM - _carEngineSpec.optimalRPM);

                        // May go beyond maxRPM and can even go negative if driving in reverse gear forwards
                        // (which should not really be allowed at all, but going beyond maxRPM is definitely possible).
                        // In such cases, the product will be below 0. The product can never go above 1 though.
                        // Nope! I think due to floating point errors it does get above 1 sometimes.
                        float c = Mathf.Clamp01(a * b);

                        // float c = a * b;
                        // if (c < 0)
                        //     c = 0;
                        */

                        float a;
                        float b;
                        float c;
                        if (motorRPM < engine.optimalRPM)
                        {
                            a = engine.optimalRPM - motorRPM;
                            b = engine.optimalRPM;
                            c = Mathf.Lerp(1, 0, a / b);
                        }
                        else
                        {
                            a = engine.maxRPM - motorRPM;
                            b = engine.maxRPM - engine.optimalRPM;
                            c = Mathf.Lerp(0, 1, a / b);
                        }

                        const float maxEfficiency = 1.0f;
                        // Unclamped because we've contrained the c already.
                        engineEfficiency = Mathf.LerpUnclamped(engine.minEfficiency, maxEfficiency, c);
                    }

                    motorTorqueApplied = engine.maxTorque * engineEfficiency * movementInputs.Forward
                        // In case the current gear ratio is negative, we're in a reverse gear.
                        // In this case, the motor's work should go in the other direction.
                        * Mathf.Sign(gearRatio);
                }
                else
                {
                    // While in clutch, the currently pressed amount just increases or descreases the engine RPM,
                    // while not actually affecting anything else.
                    {
                        float desiredMotorRPM = Mathf.Lerp(engine.idleRPM, engine.maxRPM, movementInputs.Forward);
                        motorRPM = GetMotorRPM(desiredMotorRPM, engine, drivingState.motorRPM);

                        // This one is only relevant in the clutch case, otherwise it can change instantly.
                        static float GetMotorRPM(float desiredMotorRPM, in CarEngineInfo engine, float motorRPM)
                        {
                            if (desiredMotorRPM > motorRPM)
                            {
                                float maxAllowedChange = engine.maxIdleRPMIncreasePerSecond * Time.fixedDeltaTime;
                                return Mathf.Min(desiredMotorRPM, motorRPM + maxAllowedChange);
                            }
                            else
                            {
                                float maxAllowedChange = engine.maxIdleRPMDecreasePerSecond * Time.fixedDeltaTime;
                                return Mathf.Max(desiredMotorRPM, motorRPM - maxAllowedChange);
                            }
                        }
                    }

                    // When the engine is detached from the wheels, they must not get moved by it at all.
                    motorTorqueApplied = 0;
                }

                drivingState.wheelRPM = wheelRPM;
                drivingState.motorTorqueInputFactor = movementInputs.Forward;
                drivingState.motorRPM = motorRPM;

                foreach (var motorWheelLocation in spec.motorWheelLocations)
                    colliderParts.GetWheel(motorWheelLocation).collider.motorTorque = motorTorqueApplied;
            }

            // Steering
            {
                float steeringInputFactor = movementInputs.Turn;
                float actualSteeringAngle = spec.steering.maxSteeringAngle * steeringInputFactor;
                drivingState.steeringInputFactor = steeringInputFactor;
                foreach (var steeringWheelLocation in spec.steeringWheelLocations)
                    colliderParts.GetWheel(steeringWheelLocation).collider.steerAngle = actualSteeringAngle;
            }

            // Visuals
            {
                var visualWheels = _carProperties.VisualParts.wheels;
                for (int index = 0; index < colliderParts.wheels.Length; index++)
                {
                    colliderParts.wheels[index].collider.GetWorldPose(out var position, out var rotation);
                    visualWheels[index].SetPositionAndRotation(position, rotation);
                }
            }

            _carProperties.TriggerOnDrivingStateChanged();
        }
    }
}
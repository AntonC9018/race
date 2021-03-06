#define COMPUTE_RPM_FROM_WHEELS

#if COMPUTE_RPM_FROM_WHEELS
#else
    #define COMPUTE_RPM_FROM_RIGIDBODY
#endif

using System;
using EngineCommon;
using Kari.Plugins.Flags;
using Race.Gameplay.Generated;
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

        [NiceFlags]
        public enum Flags
        {
            /// <summary>
            /// The engine is detatched from wheels (does not produce torque) while the clutch is active.
            /// Clutch allows switching gears.
            /// </summary>
            Clutch = 1 << 0,

            /// <summary>
            /// The car is disabled while respawning.
            /// </summary>
            Disabled = 1 << 1,
        }

        /// <summary>
        /// </summary>
        public Flags flags;
    }

    public interface ICarInputView
    {
        void ResetTo(CarProperties carProperties);
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
        [SerializeField] internal DataSmoothingParameters _dataSmoothing;
        private CarProperties _carProperties;
        private ICarInputView _inputView;

        /// <summary>
        /// The input view must have been already reset to the car properties at this point.
        /// </summary>
        public void Initialize(CarProperties carProperties, ICarInputView inputView)
        {
            assert(inputView != null, "Input view has not been provided.");
            _inputView = inputView;
            _carProperties = carProperties;

            // Should the initial gear be set here?
            var dataModel = carProperties.DataModel;
            var gearRatios = dataModel.Spec.transmission.gearRatios;
            int firstPositiveGearIndex = dataModel.Spec.transmission.GetIndexOfFirstPositiveGear();
            assert(firstPositiveGearIndex != -1, "No positive gears were found");
            dataModel.DrivingState.gearIndex = firstPositiveGearIndex;
        }

    #if false
        void OnGUI()
        {
            var rigidbody = GetComponent<Rigidbody>();
            var speed = rigidbody.velocity.magnitude;

            GUILayout.BeginVertical();
            GUI.color = Color.white;
            ref readonly var drivingState = ref _carProperties.DataModel.DrivingState;
            GUILayout.Label($"torqueInputFactor: {drivingState.motorTorqueInputFactor}");
            GUILayout.Label($"steeringInputFactor: {drivingState.steeringInputFactor}");
            GUILayout.Label($"gearIndex: {drivingState.gearIndex}");
            GUILayout.Label($"motorRPM: {drivingState.motorRPM}");
            GUILayout.Label($"wheelRPM: {drivingState.wheelRPM}");

            GUILayout.Label($"speed: {speed} m/s, {speed / 1000 * 3600} km/h");

            GUILayout.EndVertical();
        }
    #endif

        private void OnGearInput()
        {
            ref var drivingState = ref _carProperties.DataModel.DrivingState;

            // TODO: a separate event for these??
            if (drivingState.flags.Has(CarDrivingState.Flags.Clutch))
            {
                var gearInput = _inputView.Gear;

                if (gearInput == GearInputType.GearUp
                    && drivingState.gearIndex + 1 < _carProperties.DataModel.Spec.transmission.gearRatios.Length)
                {
                    // Do we need the old state for ui logic?
                    drivingState.gearIndex += 1;
                    _carProperties.TriggerOnGearShifted();
                }
                
                else if (gearInput == GearInputType.GearDown
                    && drivingState.gearIndex > 0)
                {
                    drivingState.gearIndex -= 1;
                    _carProperties.TriggerOnGearShifted();
                }
            }
        }

        void FixedUpdate()
        {
            ref readonly var colliderParts = ref _carProperties.DataModel.ColliderParts;
            ref readonly var spec = ref _carProperties.DataModel.Spec;
            ref var drivingState = ref _carProperties.DataModel.DrivingState;

            if (drivingState.flags.Has(CarDrivingState.Flags.Disabled))
                return;

            // Not done:
            // 1. timeout on gear switching and the clutch becoming active.
            // 2. possibility of stalling the engine by driving in wrong gear.
            // 3. block on reverse gear while driving forward (and the other way).
            // 4. gradual braking.
            // 5. better presentation.

            bool isClutch;
            {
                isClutch = _inputView.Clutch;
                drivingState.flags.Set(CarDrivingState.Flags.Clutch, isClutch);
            }

            // We process input events in FixedUpdate (see the input system settings).
            // TODO: may want to abstract this.
            OnGearInput();

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
                    wheelRPM = MathHelper.GetValueChangedByAtMost((float) drivingState.wheelRPM, recordedWheelRPM, maxAllowedChange);
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
                // Then it follows a completely different set of rules.
                if (!isClutch)
                {
                    float genericGearRatio = spec.transmission.gearRatios[drivingState.gearIndex];
                    float gearRatio = CarDataModelHelper.AdjustGearRatioToCarWheels(genericGearRatio, colliderParts);

                    // RPM between the engine and the wheels.
                    // We don't do any damping here either (at least for now).
                    float desiredMotorRPM = wheelRPM * gearRatio;

                    // The RPM here should change instantly (probably).
                    // motorRPM = GetMotorRPM(desiredMotorRPM, _carEngineSpec, _carDrivingState.motorRMP);
                    motorRPM = desiredMotorRPM;

                    float engineEfficiency = CarDataModelHelper.GetEngineEfficiency(motorRPM, engine);

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
                var visualWheels = _carProperties.DataModel.VisualParts.wheels;
                for (int index = 0; index < colliderParts.wheels.Length; index++)
                {
                    colliderParts.wheels[index].collider.GetWorldPose(out var position, out var rotation);
                    ref var visualWheel = ref visualWheels[index];
                    visualWheel.transform.SetPositionAndRotation(position, rotation * visualWheel.initialRotation);
                }
            }

            _carProperties.TriggerOnDrivingStateChanged();
        }
    }
}
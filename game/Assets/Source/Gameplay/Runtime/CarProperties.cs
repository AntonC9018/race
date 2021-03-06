using System;
using EngineCommon;
using Race.Gameplay.Generated;
using UnityEngine;
using UnityEngine.Events;
using static EngineCommon.Assertions;

namespace Race.Gameplay
{
    // A fixed data model works good for me, because I can just come here and add things if I need.
    // More than that, it's simply data, so no cruft.
    // We could have a more extensible data model with a gameobject with components,
    // or just an array with opaque objects where you'd look up things by their ids,
    // or an autogenerated thing, or a dictionary with string keys.
    // There are many ways to do this dynamically, so it's not a problem to do it this way
    // if we even happen to need it.

    /// <summary>
    /// </summary>
    [System.Serializable]
    public class CarDataModel
    {
        /// <summary>
        /// Info about the car spec, or the configuration.
        /// Includes info about the engine, transmission, wheels.
        /// Does not include any actual game objects or colliders of sorts, it's just plain data.
        /// </summary>
        public readonly CarSpecInfo _spec;

        /// <summary>
        /// Current RPM, wheel RPM, gear and things like that.
        /// </summary>
        public CarDrivingState _drivingState;

        /// <summary>
        /// The info (metadata) attached to the car, taken directly from the game object. 
        /// </summary>
        public readonly CarInfoComponent _infoComponent;

        /// <summary>
        /// The transform is pretty hard to get at without the assumption
        /// that it's on the same object that CarProperties is on.
        /// </summary>
        public readonly Transform _transform;

        public CarDataModel(CarSpecInfo spec, CarInfoComponent infoComponent, Transform transform)
        {
            _spec = spec;
            _drivingState = new CarDrivingState();
            _infoComponent = infoComponent;
            _transform = transform;
        }

        public ref CarDrivingState DrivingState => ref _drivingState;
        public Transform Transform => _transform;

        /// <summary>
        /// We do not allow it to change at runtime.
        /// </summary>
        public ref readonly CarSpecInfo Spec => ref _spec;
        public ref readonly CarColliderParts ColliderParts => ref _infoComponent.colliderParts;
        public ref readonly CarVisualParts VisualParts => ref _infoComponent.visualParts;
    }

    public static class CarDataModelHelper
    {
        public static int GetIndexOfFirstPositiveGear(in this CarTransmissionInfo transmission)
        {
            for (int i = 0; i < transmission.gearRatios.Length; i++)
            {
                if (transmission.gearRatios[i] > 0)
                    return i;
            }
            return -1;
        }
        
        public static void StopCar(Transform carTransform, CarProperties properties)
        {
            var carDataModel = properties.DataModel;
            {
                var rb = carDataModel.ColliderParts.rigidbody;
                rb.isKinematic = true;
                rb.velocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;

                foreach (ref var wheel in carDataModel.ColliderParts.wheels.AsSpan())
                {
                    var collider = wheel.collider;
                    collider.brakeTorque = float.MaxValue;
                    collider.motorTorque = 0;
                }
            }
            {
                ref var drivingState = ref carDataModel.DrivingState;
                drivingState.flags.Set(CarDrivingState.Flags.Disabled);
            }
            properties.TriggerOnDrivingToggled();
        }

        public static void RestartDisabledDriving(CarProperties properties)
        {
            var carDataModel = properties.DataModel;

            {
                ref var s = ref carDataModel.DrivingState;
                
                assert(s.flags.Has(CarDrivingState.Flags.Disabled));
                
                s.gearIndex = carDataModel.Spec.transmission.GetIndexOfFirstPositiveGear();
                s.flags.Unset(CarDrivingState.Flags.Disabled);
                s.motorTorqueInputFactor = 0;
                s.brakeTorqueInputFactor = 0;
                s.steeringInputFactor = 0;
                s.motorRPM = 0;
                s.wheelRPM = 0;
            }

            {
                var rb = carDataModel.ColliderParts.rigidbody;
                rb.isKinematic = false;

                foreach (ref var wheel in carDataModel.ColliderParts.wheels.AsSpan())
                {
                    var collider = wheel.collider;
                    collider.brakeTorque = 0;
                    collider.motorTorque = 0;
                }
            }

            properties.TriggerOnDrivingStateChanged();
            properties.TriggerOnDrivingToggled();
        }

        public static void ResetPositionAndRotationOfBackOfCar(Transform carTransform, CarDataModel carDataModel, Vector3 targetPosition, Quaternion targetRotation)
        {
            Vector3 additionalDisplacement = 
                GetUpDisplacementVector(carDataModel) + GetForwardDisplacementVector(carDataModel);
            var position = targetPosition + targetRotation * additionalDisplacement;
            carTransform.SetPositionAndRotation(position, targetRotation);
        }
        
        public static void ResetPositionAndRotationOfCenterOfCar(Transform carTransform, CarDataModel carDataModel, Vector3 targetPosition, Quaternion targetRotation)
        {
            Vector3 additionalDisplacement = GetUpDisplacementVector(carDataModel);
            var position = targetPosition + targetRotation * additionalDisplacement;
            carTransform.SetPositionAndRotation(position, targetRotation);
        }

        private static Vector3 GetForwardDisplacementVector(CarDataModel carDataModel)
        {
            var halfLength = carDataModel.GetBodySize().z;
            var forward = halfLength * Vector3.forward;
            return forward;
        }

        private static Vector3 GetUpDisplacementVector(CarDataModel carDataModel)
        {
            var elevation = carDataModel._infoComponent.elevationSuchThatWheelsAreLevelWithTheGround;
            var up = elevation * Vector3.up;
            return up;
        }


        // Since the gear ratio is expressed for a generic wheel (radius of 1),
        // it gets multiplied by the wheel radius.
        // Think of it as though the wheel of the car were connected to the transmission
        // via another wheel of radius 1. If the larger wheel does 1 revolution, the smaller wheel
        // will do Radius revolutions. Then go the actual gears, which would rescale it further.
        public static float AdjustGearRatioToCarWheels(float genericGearRatio, float wheelRadius)
        {
            return genericGearRatio * wheelRadius;
        }

        public static float AdjustGearRatioToCarWheels(float genericGearRatio, in CarColliderParts parts)
        {
            return AdjustGearRatioToCarWheels(genericGearRatio, parts.wheels[0].collider.radius);
        }

        /// <summary>
        /// Returns the max speed estimate in m/s.
        /// </summary>
        public static float GetMaxSpeed(this CarDataModel dataModel)
        {
            var maxMotorRPM = dataModel.Spec.engine.maxRPM;
            ref var wheel = ref dataModel.ColliderParts.wheels[0].collider;
            var maxGearRatio = AdjustGearRatioToCarWheels(dataModel.Spec.transmission.gearRatios[^1], wheel.radius);
            var circumferenceOfWheel = wheel.GetCircumference();
            var maxWheelRPM = maxMotorRPM / maxGearRatio;
            var maxSpeed = FromRPMToSpeed(maxWheelRPM, circumferenceOfWheel);
            return maxSpeed;
        }

        /// <summary>
        /// Returns the speed in m/s.
        /// </summary>
        public static float FromRPMToSpeed(float rpm, float circumference)
        {
            return rpm * circumference / 60.0f;
        }

        /// <summary>
        /// Returns the speed in m/s.
        /// </summary>
        public static float FromRPMToSpeed(float rpm, in CarPart<WheelCollider> wheel)
        {
            return FromRPMToSpeed(rpm, wheel.collider.GetCircumference());
        }

        /// <summary>
        /// Returns the current vehicle speed in m/s.
        /// The speed is calculated based on the wheel RPM.
        /// </summary>
        public static float GetCurrentSpeed(this CarDataModel dataModel)
        {
            // float speed = model.ColliderParts.Rigidbody.velocity.magnitude;
            return FromRPMToSpeed(dataModel.DrivingState.wheelRPM, dataModel.ColliderParts.wheels[0]);
        }

        const float maxEngineEfficiency = 1.0f;

        // The idea is that the engine efficiency peaks when it's at the optimal RPM.
        // It dies down towards the edges (0 and `maxRPM` for the wheels).
        public static float GetEngineEfficiency(float motorRPM, in CarEngineInfo engine)
        {
            // I think in real cars the function should be more sophisticated.
            float a;
            float b;
            float c;

            if (motorRPM < engine.optimalRPM)
            {
                a = engine.optimalRPM - motorRPM;
                b = engine.optimalRPM;
                c = Mathf.Lerp(maxEngineEfficiency, engine.efficiencyAtIdleRPM, a / b);
            }
            else
            {
                a = engine.maxRPM - motorRPM;
                b = engine.maxRPM - engine.optimalRPM;
                c = Mathf.Lerp(engine.efficiencyAtMaxRPM, maxEngineEfficiency, a / b);
            }

            return c;
        }

        // The inverse of GetEngineEfficiency.
        // TODO: unittest for keeping the two in sync.
        /// <summary>
        /// By the spec, gives estimates rather than precise results.
        /// Does not depend on torque.
        /// </summary>
        public static float GetLowEngineRPMAtEngineEfficiency(float efficiency, in CarEngineInfo engine)
        {
            float a = efficiency - engine.efficiencyAtIdleRPM;
            float b = maxEngineEfficiency - engine.efficiencyAtIdleRPM;
            return Mathf.Lerp(engine.idleRPM, engine.optimalRPM, a / b);
        }

        /// <summary>
        /// By the spec, gives estimates rather than precise results.
        /// Does not depend on torque.
        /// </summary>
        public static float GetHighEngineRPMAtEngineEfficiency(float efficiency, in CarEngineInfo engine)
        {
            float a = efficiency - engine.efficiencyAtMaxRPM;
            float b = maxEngineEfficiency - engine.efficiencyAtMaxRPM;
            return Mathf.Lerp(engine.maxRPM, engine.optimalRPM, a / b);
        }

        public static Vector3 GetBodySize(this CarDataModel dataModel)
        {
            return dataModel.ColliderParts.body.collider.size;
        }

        public static float DampValueDependingOnSpeed(
            float desiredValue,
            float currentValue,
            float allowedValuePerSecondAtMinimumSpeed,
            float currentSpeed,
            float maxSpeed)
        {
            const float hardcodedPowerFactor = 9;
            var exponent = 1.0f + 1.0f / maxSpeed * hardcodedPowerFactor;
            currentSpeed = MathHelper.ClampMagnitude(currentSpeed, 0, maxSpeed);
            var dampedFactor = Mathf.Pow(exponent, -currentSpeed);

            const float allowedTurnChangePerSecondAtMaximumSpeed = 0.0001f;
            var allowedChangeFactor = Mathf.Lerp(allowedTurnChangePerSecondAtMaximumSpeed, allowedValuePerSecondAtMinimumSpeed, dampedFactor);
            var allowedChange = allowedChangeFactor * Time.fixedDeltaTime;

            float actualValueFactor = MathHelper.GetValueChangedByAtMost(currentValue, desiredValue, allowedChange);

            return actualValueFactor;
        }

        public static float DampTurnInputDependingOnCurrentSpeed(
            CarDataModel carDataModel,
            float desiredTurnFactor,
            float allowedTurnChangePerSecondAtMinimumSpeed)
        {
            var currentSpeed = CarDataModelHelper.GetCurrentSpeed(carDataModel);
            var maxSpeed = CarDataModelHelper.GetMaxSpeed(carDataModel);
            float currentTurnFactor = carDataModel.DrivingState.steeringInputFactor;

            return DampValueDependingOnSpeed(
                desiredTurnFactor,
                currentTurnFactor,
                allowedTurnChangePerSecondAtMinimumSpeed,
                currentSpeed,
                maxSpeed);
        }

        public static bool IsDrivingDisabled(this CarDataModel carDataModel)
        {
            return carDataModel.DrivingState.flags.Has(CarDrivingState.Flags.Disabled);
        }
        public static bool IsDrivingEnabled(this CarDataModel carDataModel)
        {
            return carDataModel.DrivingState.flags.DoesNotHave(CarDrivingState.Flags.Disabled);
        }
    }

    public class CarProperties : MonoBehaviour
    {
        private CarDataModel _dataModel;
        public CarDataModel DataModel => _dataModel;

        public void Initialize(CarDataModel dataModel)
        {
            _dataModel = dataModel;
            
            {
                ref readonly var spec = ref _dataModel.Spec;
                assert(spec.motorWheelLocations is not null);
                assert(spec.brakeWheelLocations is not null);
                assert(spec.steeringWheelLocations is not null);
                assert(spec.transmission.gearRatios is not null);
            }
        }

        // TODO: A separate event object could be helpful.
        public UnityEvent<CarProperties> OnDrivingStateChanged;
        public UnityEvent<CarProperties> OnDrivingToggled;
        public UnityEvent<CarProperties> OnGearShifted;

        public void TriggerOnDrivingStateChanged()
        {
            OnDrivingStateChanged.Invoke(this);
        }

        public void TriggerOnDrivingToggled()
        {
            OnDrivingToggled.Invoke(this);
        }

        public void TriggerOnGearShifted()
        {
            OnGearShifted.Invoke(this);
        }
    }
}
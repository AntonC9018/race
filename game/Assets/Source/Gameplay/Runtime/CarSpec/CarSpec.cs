using System.Runtime.CompilerServices;
using UnityEngine;

namespace Race.Gameplay
{
    [System.Serializable]
    public struct CarSpecInfo
    {
        public CarBrakesInfo brakes;
        public CarEngineInfo engine;
        public CarTransmissionInfo transmission;
        public CarSteeringInfo steering;
        public WheelLocation[] motorWheelLocations;
        public WheelLocation[] brakeWheelLocations;
        public WheelLocation[] steeringWheelLocations;
    }

    [System.Serializable]
    public struct CarTransmissionInfo
    {
        /// <summary>
        /// engineRPM / wheelRPM
        /// </summary>
        public float[] gearRatios;
    }

    [System.Serializable]
    public struct CarBrakesInfo
    {
        public float maxTorque;
    }

    [System.Serializable]
    public struct CarSteeringInfo
    {
        public float maxSteeringAngle;
    }
    
    [System.Serializable]
    public struct CarEngineInfo
    {

        /// <summary>
        /// Maximum working RPM of the engine.
        /// The car takes damage above this value (??).
        /// Used in engine efficiency calculations.
        /// </summary>
        public float maxRPM;

        /// <summary>
        /// Torque excerted at maximum efficiency.
        /// </summary>
        public float maxTorque;

        /// <summary>
        /// At this RPM, it produces the maximum torque.
        /// </summary>
        public float optimalRPM;

        /// <summary>
        /// Reaches this RPM while in clutch without force applied.
        /// </summary>
        public float idleRPM;

        /// <summary>
        /// Needed so that it can move at all from stationary position.
        /// </summary>
        public float efficiencyAtIdleRPM;

        /// <summary>
        /// </summary>
        public float efficiencyAtMaxRPM;
        
        /// <summary>
        // These two are only relevant when the clutch is applied.
        // Otherwise the RPM correlates to wheel RPM.
        /// </summary>
        public float maxIdleRPMIncreasePerSecond;
        public float maxIdleRPMDecreasePerSecond;
    }

    /// <summary>
    /// Indices of the corresponding wheels in wheel arrays.
    /// </summary>
    public enum WheelLocation
    {
        BackLeft = 0,
        BackRight = WheelHelper.RightBit,
        FrontLeft = WheelHelper.FrontBit,
        FrontRight = WheelHelper.FrontBit | WheelHelper.RightBit,
    }
    
    public static class WheelHelper
    {
        // Hack: one can't hide enum members in the editor?? Why is there no attribute for this?
        public const WheelLocation RightBit = (WheelLocation) 1;
        public const WheelLocation FrontBit = (WheelLocation) 2;

        /// <summary>
        /// The wheels have to be named this way.
        /// </summary>
        public static readonly string[] WheelNames;
        static WheelHelper()
        {
            WheelNames = new string[4];
            WheelNames[(int) WheelLocation.BackLeft]   = "back_left";
            WheelNames[(int) WheelLocation.BackRight]  = "back_right";
            WheelNames[(int) WheelLocation.FrontLeft]  = "front_left";
            WheelNames[(int) WheelLocation.FrontRight] = "front_right";
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float GetCircumference(this WheelCollider wheel)
        {
            return wheel.radius * 2 * Mathf.PI;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float GetCircumference(this in CarPart<WheelCollider> wheel)
        {
            return GetCircumference(wheel.collider);
        }
    }
}
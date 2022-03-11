using System.Runtime.CompilerServices;
using UnityEngine;

namespace Race.Gameplay
{
    /// <summary>
    /// Allows defining these properties in the inspector and share them between cars.
    /// </summary>
    [CreateAssetMenu(
        fileName = "New " + nameof(CarSpecInfoObject),
        menuName = nameof(CarSpecInfoObject),
        // Second grouping.
        order = 51)]
    public class CarSpecInfoObject : ScriptableObject
    {
        public CarSpecInfo info;

        // TODO: autogenerate
        // public ref CarBrakesInfo Brakes => ref info.brakes;
        // public ref CarEngineInfo Engine => ref info.engine;
        // public ref CarTransmissionInfo Transmission => ref info.transmission;
        // public ref WheelLocation[] MotorWheelLocations => ref info.motorWheelLocations;
        // public ref WheelLocation[] BrakeWheelLocations => ref info.brakeWheelLocations;
        // public ref WheelLocation[] SteeringWheelLocations => ref info.steeringWheelLocations;
    }
}
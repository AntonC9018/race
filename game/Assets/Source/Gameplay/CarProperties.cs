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
        public CarSpecInfoObject _spec;

        /// <summary>
        /// Current RPM, wheel RPM, gear and things like that.
        /// </summary>
        public CarDrivingState _drivingState;

        /// <summary>
        /// The gameobjects that are not linked to visuals directly.
        /// </summary>
        public CarColliderParts _colliderParts;

        /// <summary>
        /// We do not allow it to change at runtime.
        /// </summary>
        public ref readonly CarSpecInfo Spec => ref _spec.info;
        public ref CarDrivingState DrivingState => ref _drivingState;
        public ref readonly CarColliderParts ColliderParts => ref _colliderParts;
    }

    public class CarProperties : MonoBehaviour
    {
        // For now, show it in the inspector.
        // In the end it should be set up dynamically.
        [SerializeField] internal CarDataModel _dataModel;
        public CarDataModel DataModel => _dataModel;

        [SerializeField] internal CarVisualParts _visualParts;
        public ref readonly CarVisualParts VisualParts => ref _visualParts;
        
        // This one is needed mostly for syncronization.
        // Like running the code that depends on the new model values after they've actually been updated.
        public UnityEvent<CarDataModel> OnDrivingStateChanged;

        public void TriggerOnDrivingStateChanged()
        {
            OnDrivingStateChanged.Invoke(DataModel);
        }

        void Awake()
        {
            CarColliderSetupHelper.AdjustCenterOfMass(DataModel.ColliderParts);

            ref readonly var spec = ref DataModel.Spec;
            var gearRatios = spec.transmission.gearRatios;
            assert(gearRatios is not null);
            assert(gearRatios.Length > 0);

            int firstPositiveGearIndex = -1;
            for (int i = 0; i < gearRatios.Length; i++)
            {
                if (gearRatios[i] > 0)
                {
                    firstPositiveGearIndex = i;
                    break;
                }
            }
            DataModel.DrivingState.gearIndex = firstPositiveGearIndex;

            assert(spec.motorWheelLocations is not null);
            assert(spec.brakeWheelLocations is not null);
            assert(spec.steeringWheelLocations is not null);
        }

        void Setup()
        {
            TriggerOnDrivingStateChanged();
        }
    }
}
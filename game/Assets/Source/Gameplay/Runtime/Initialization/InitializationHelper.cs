using UnityEngine;
using static EngineCommon.Assertions;

namespace Race.Gameplay
{
    public static class InitializationHelper
    {
        public static void InitializePlayerInput(
            GameObject car,
            Gameplay.CarProperties carProperties,
            CameraControl cameraControl,
            IInputViewManager viewFactory)
        {
            // For now just get these from the object, but these should be created
            // on demand for each new local player.
            // TODO: something, don't know what yet.
            assert(viewFactory != null);
            ICameraInputView cameraInputView = viewFactory.CreateCameraInputView(0, carProperties);
            ICarInputView carInputView = viewFactory.CreateCarInputView(0, carProperties);

            assert(cameraInputView != null);
            assert(carInputView != null);

            cameraControl.Initialize(car.transform, cameraInputView);

            var carController = car.GetComponent<CarController>();
            InitializeCarController(carInputView, carController, carProperties);
        }

        public static void InitializeCarController(
            ICarInputView carInputView, CarController carController, Gameplay.CarProperties carProperties)
        {
            assert(carController != null);
            assert(carInputView is not null);
            assert(carProperties != null);

            carInputView.ResetTo(carProperties);
            carController.Initialize(carProperties, carInputView);
        }

        public static void FinalizeCarPropertiesInitialization(
            Gameplay.CarProperties carProperties,
            CarInfoComponent infoComponent,
            Transform transform,
            in CarSpecInfo carSpec)
        {
            CarColliderSetupHelper.AdjustCenterOfMass(
                ref infoComponent.colliderParts, infoComponent.template.centerOfMassAdjustmentParameters);

            var carDataModel = new Gameplay.CarDataModel(carSpec, infoComponent, transform);
            carProperties.Initialize(carDataModel);
        }

        // TDOO:
        // Could use some dynamic dependency injection for the widgets.
        // For now, inject manually with a mini-implementation.
        public static void InitializeUI(Transform ui, CarProperties properties)
        {
            var components = ui.GetComponentsInChildren<ISetCarProperties>(includeInactive: false);
            foreach (var component in components)
                component.CarProperties = properties;
        }

        public interface ISetCarProperties
        {
            CarProperties CarProperties { set; }
        }


        public static void FinalizeCarPropertiesInitializationWithDefaults(
            Gameplay.CarProperties carProperties, CarInfoComponent infoComponent, Transform transform)
        {
            var carSpec = infoComponent.template.baseSpec;
            FinalizeCarPropertiesInitialization(carProperties, infoComponent, transform, carSpec);
        }

        public static void InitializePlayerInputAndInjectDependencies(
            CommonInitializationStuff stuff,
            CameraControl cameraControl,
            GameObject car,
            CarProperties carProperties)
        {
            InitializationHelper.InitializePlayerInput(car, carProperties, cameraControl, stuff.inputViewFactory);
            InitializationHelper.InitializeUI(stuff.diRootTransform, carProperties);
        }
        
        public static (IStaticTrack, RaceManager) InitializeTrackAndRaceManagerFromTrackQuad(
            DriverInfo[] driverInfos, Transform trackQuad, IDelay delay)
        {
            var (track, trackWidth) = TrackHelper.CreateFromQuad(trackQuad);

            // For now, do hacks and produce garbage.
            // TODO: refactor
            var raceManager = new RaceManager();
            raceManager.Initialize(driverInfos, track, delay);

            var grid = new GridPlacementStrategy();
            grid.Reset(track, trackWidth, driverInfos);
            raceManager.PlaceParticipants(grid);

            return (track, raceManager);
        }
    }
}
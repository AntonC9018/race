using UnityEngine;
using static EngineCommon.Assertions;

namespace Race.Gameplay
{
    public interface IInitialize<T>
    {
        void Initialize(T value);
    }

    // Note: 90% of this code can be replaced using a dependency injection framework.
    // Or with advanced code generation.
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

        // Could use some dynamic dependency injection for the widgets.
        // For now, inject manually with a mini-implementation.
        public static void InjectDependency<T>(Transform diRoot, T value)
        {
            // Inject into self
            // {
            //     var c = diRoot.GetComponent<IInitialize<T>>();
            //     if (c != null)
            //         c.Initialize(value);
            // }

            // Inject into children
            var components = diRoot.GetComponentsInChildren<IInitialize<T>>(includeInactive: false);
            foreach (var component in components)
                component.Initialize(value);
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
            InitializePlayerInput(car, carProperties, cameraControl, stuff.inputViewFactory);
            InjectDependency(stuff.diRootTransform, carProperties);
            
            carProperties.TriggerOnDrivingToggled();
        }
        
        public static Transform FindTrackTransform(Transform mapTransform)
        {
            return mapTransform.Find("track");
        }
        
        public static Transform FindRaceLogicTransform(Transform rootTransform)
        {
            return rootTransform.Find("race_logic");
        }

        public static TrackRaceInfo CreateTrackWithInfo(Transform trackTransform, TrackLimitsConfiguration trackLimitsConfiguration)
        {
            return TrackHelper.CreateFromQuad(trackTransform, trackLimitsConfiguration.actualToVisualWidthRatio);
        }
    }
}
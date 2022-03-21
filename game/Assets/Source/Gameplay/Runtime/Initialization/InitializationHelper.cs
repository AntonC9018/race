using UnityEngine;
using static EngineCommon.Assertions;

namespace Race.Gameplay
{
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

        public interface ISetCarProperties
        {
            CarProperties CarProperties { set; }
        }

        public static void InjectDependency(Transform diRoot, CarProperties properties)
        {
            var inject = new InjectCarProperties(properties);
            InjectDependecies<ISetCarProperties, InjectCarProperties>(diRoot, inject);
        }

        public interface ISetRaceProperties
        {
            RaceProperties RaceProperties { set; }
        }

        public static void InjectDependency(Transform diRoot, RaceProperties properties)
        {
            var inject = new InjectRaceProperties(properties);
            InjectDependecies<ISetRaceProperties, InjectRaceProperties>(diRoot, inject);
        }

        private struct InjectCarProperties : IInject<ISetCarProperties>
        {
            private CarProperties properties;

            public InjectCarProperties(CarProperties properties)
            {
                this.properties = properties;
            }

            public void Inject(ISetCarProperties where)
            {
                where.CarProperties = properties;
            }
        }

        private readonly struct InjectRaceProperties : IInject<ISetRaceProperties>
        {
            private readonly RaceProperties properties;

            public InjectRaceProperties(RaceProperties properties)
            {
                this.properties = properties;
            }

            public void Inject(ISetRaceProperties where)
            {
                where.RaceProperties = properties;
            }
        }
        
        // TDOO:
        // Could use some dynamic dependency injection for the widgets.
        // For now, inject manually with a mini-implementation.
        //
        // As you can see from the usage, making it generic does not achieve much.
        public interface IInject<I>
        {
            void Inject(I where);
        }

        public static void InjectDependecies<Interface, Inject>(Transform diRoot, Inject inject) where Inject : IInject<Interface>
        {
            var components = diRoot.GetComponentsInChildren<Interface>(includeInactive: false);
            foreach (var component in components)
                inject.Inject(component);
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
        }

        public static TrackRaceInfo CreateTrackWithInfo(Transform trackTransform, float howMuchVisualWidthInActualWidth)
        {
            var (track, actualWidth) = TrackHelper.CreateFromQuad(trackTransform);
            var visualWidth = howMuchVisualWidthInActualWidth * actualWidth;

            return new TrackRaceInfo
            {
                track = track,
                actualWidth = actualWidth,
                visualWidth = visualWidth,
            };
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
            return InitializationHelper.CreateTrackWithInfo(trackTransform, trackLimitsConfiguration.howMuchVisualWidthInActualWidth);
        }
    }
}
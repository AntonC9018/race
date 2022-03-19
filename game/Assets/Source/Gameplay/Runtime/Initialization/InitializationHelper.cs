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

            InitializePlayerInput(
                car, carProperties, cameraControl, cameraInputView, carInputView);
        }

        public static void InitializePlayerInput(
            GameObject car,
            Gameplay.CarProperties carProperties,
            CameraControl cameraControl,
            ICameraInputView cameraInputView,
            ICarInputView carInputView)
        {
            assert(cameraInputView != null);
            assert(carInputView != null);

            {
                cameraControl.Initialize(car.transform, cameraInputView);
            }
            {
                var carController = car.GetComponent<CarController>();
                InitializeCarController(carInputView, carController, carProperties);
            }
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
        
        private static void InitializeBotInput(
            GameObject car, Gameplay.CarProperties carProperties)
        {
            // TODO: settings for difficulty and such
            var carInputView = new BotInputView();

            var carController = car.GetComponent<CarController>();
            Gameplay.InitializationHelper.InitializeCarController(carInputView, carController, carProperties);
        }

        public static void FinalizeCarPropertiesInitialization(
            Gameplay.CarProperties carProperties,
            CarInfoComponent infoComponent,
            in CarSpecInfo carSpec)
        {
            CarColliderSetupHelper.AdjustCenterOfMass(
                ref infoComponent.colliderParts, infoComponent.template.centerOfMassAdjustmentParameters);

            var carDataModel = new Gameplay.CarDataModel(carSpec, infoComponent);
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
            Gameplay.CarProperties carProperties, CarInfoComponent infoComponent)
        {
            var carSpec = infoComponent.template.baseSpec;
            FinalizeCarPropertiesInitialization(carProperties, infoComponent, carSpec);
        }
    }
}
using UnityEngine;
using Race.Gameplay;
using Race.Garage;
using System;
using static EngineCommon.Assertions;

namespace Race.SceneTransition
{
    [System.Serializable]
    public struct PlayerInfo
    {
        public int carIndex;
        public Garage.CarDataModel carDataModel;
        public UserDataModel userDataModel;
    }
   
    [System.Serializable]
    public struct BotInfo
    {
        public int carIndex;
    }

    public ref struct GameplayInitializationInfo
    {
        // We don't pass arrays because we don't need to keep them in memory.
        // TODO: Could pass IEnumerable's?
        public Span<PlayerInfo> playerInfos;
        public Span<BotInfo> botInfos;
    }

    [System.Serializable]
    public struct StatsConversionRates
    {
        // for now keep it const
        public const float c_torqueFactor = 1.0f;
        public readonly float torqueFactor => c_torqueFactor;
    }

    // TODO: this class does too many things
    public class Transition : MonoBehaviour, ITransitionToGameplaySceneFromGarage
    {
        [SerializeField] private GameObject _cameraControlPrefab;

        private static CarSpecInfo GetEngineSpecFromStatsAndTemplate(
            in CarStats currentStats,
            in StatsConversionRates rates,
            in Race.Gameplay.CarSpecInfo template)
        {
            // Initialize by copying (it's a struct).
            var carSpec = template;

            // We don't do a full copy, we only copy what we know is going to change.
            // Right now the only reference type that gets changed is the gear ratios.
            // Wheel locations for example still point to the template ones.
            {
                ref var g = ref carSpec.transmission.gearRatios;
                g = g[..];
            }
            
            float motorRPMAtOldTorque;
            {
                float a = currentStats.accelerationModifier;
                float torqueBaseline = template.engine.maxTorque;
                float newTorque = a * rates.torqueFactor + torqueBaseline;

                // At optimalRPM the engine gave T torque.
                // Now it will give T' torque at that same point.
                // We shift it by dN to get the new desired RPM, such that at the old RPM it stays at T.  
                float previousTorque = torqueBaseline;
                // How much of the new torque is enough to get the previous torque.
                float neededEfficiencyForOldTorque = previousTorque / newTorque;

                motorRPMAtOldTorque = CarDataModelHelper.GetLowEngineRPMAtEngineEfficiency(neededEfficiencyForOldTorque, template.engine);

                carSpec.engine.maxTorque = newTorque;
            }
            
            foreach (ref var g in carSpec.transmission.gearRatios.AsSpan())
                g = template.engine.optimalRPM / (g * motorRPMAtOldTorque);

            return carSpec;
        }

        // TODO:
        private GameObject SpawnCar(Transform parent, int carIndex)
        {
            return null;
        }

        private static void ApplyColor(Color color, CarInfoComponent infoComponent)
        {
            infoComponent.visualParts.meshRenderer.material.color = color;
        }

        public void InitializePlayerCar(GameObject playerCar)
        {
            var carProperties = playerCar.GetComponent<Gameplay.CarProperties>();
            assert(carProperties != null, "The car prefab must contain a `CarProperties` component");
            var infoComponent = playerCar.GetComponent<CarInfoComponent>();
            FinalizeCarPropertiesInitializationWithDefaults(carProperties, infoComponent);
            InitializePlayerInput(playerCar, carProperties);
        }

        // Initialization always turns into a mess.
        public void InitializeGameplaySceneWithConfiguration(Transform root, in GameplayInitializationInfo initInfo)
        {
            assert(initInfo.playerInfos.Length == 1, "For now only one player is allowed");

            var playerInfos = initInfo.playerInfos;

            for (int playerIndex = 0; playerIndex < playerInfos.Length; playerIndex++)
            {
                ref var playerInfo = ref playerInfos[playerIndex];
                var car = SpawnCar(root, playerInfo.carIndex);

                // 1
                var carProperties = car.GetComponent<Gameplay.CarProperties>();
                assert(carProperties != null, "The car prefab must contain a `CarProperties` component");
                var infoComponent = car.GetComponent<CarInfoComponent>();

                // 2
                CarSpecInfo carSpec = GetEngineSpecFromStatsAndTemplate(
                    currentStats: playerInfo.carDataModel.statsInfo.currentStats,
                    rates: new StatsConversionRates(),
                    template: infoComponent.template.baseSpec);

                // TODO:
                // The mesh renderer should be in a separate metadata component,
                // or should be accessed in a standard way (there are other ways too, via interfaces).
                ApplyColor(playerInfo.carDataModel.mainColor, infoComponent);

                FinalizeCarPropertiesInitialization(carProperties, infoComponent, carSpec);
                InitializePlayerInput(car, carProperties);
            }

            var botInfos = initInfo.botInfos;

            for (int botIndex = 0; botIndex < botInfos.Length; botIndex++)
            {
                var car = SpawnCar(root, botInfos[botIndex].carIndex);

                var carProperties = car.GetComponent<Gameplay.CarProperties>();
                assert(carProperties != null, "The car prefab must contain a `CarProperties` component");
                var infoComponent = car.GetComponent<CarInfoComponent>();

                FinalizeCarPropertiesInitializationWithDefaults(carProperties, infoComponent);
                InitializeBotInput(car, carProperties);
            }
        }

        private static void FinalizeCarPropertiesInitializationWithDefaults(
            Gameplay.CarProperties carProperties, CarInfoComponent infoComponent)
        {
            var carSpec = infoComponent.template.baseSpec;
            FinalizeCarPropertiesInitialization(carProperties, infoComponent, carSpec);
        }

        private static void InitializeBotInput(
            GameObject car, Gameplay.CarProperties carProperties)
        {
            // TODO: settings for difficulty and such
            var carInputView = new BotInputView();

            var carController = car.GetComponent<CarController>();
            InitializeCarController(carInputView, carController, carProperties);
        }

        private void InitializePlayerInput(
            GameObject car, Gameplay.CarProperties carProperties)
        {
            // For now just get these from the object, but these should be created
            // on demand for each new local player.
            // TODO: something, don't know what yet.
            var factory = GetComponent<IInputViewFactory>();
            assert(factory != null);

            ICameraInputView cameraInputView = factory.CreateCameraInputView(0, carProperties);
            assert(cameraInputView != null);

            ICarInputView carInputView = factory.CreateCarInputView(0, carProperties);
            assert(carInputView != null);

            {
                var cameraControlGameObject = GameObject.Instantiate(_cameraControlPrefab);
                var cameraControl = cameraControlGameObject.GetComponent<CameraControl>();
                cameraControl.Initialize(car.transform, cameraInputView);
            }
            {
                var carController = car.GetComponent<CarController>();
                InitializeCarController(carInputView, carController, carProperties);
            }
        }

        private static void InitializeCarController(
            ICarInputView carInputView, CarController carController, Gameplay.CarProperties carProperties)
        {
            assert(carController != null);
            assert(carInputView is not null);
            assert(carProperties != null);

            carInputView.ResetTo(carProperties);
            carController.Initialize(carProperties, carInputView);
        }

        // I feel like initialization is a good cause for the code generator.
        // Adding features with this code is going to be a massive pain.
        // The initialization needs some actual thought.

        private static void FinalizeCarPropertiesInitialization(
            Gameplay.CarProperties carProperties,
            CarInfoComponent infoComponent,
            in CarSpecInfo carSpec)
        {
            // 3
            CarColliderSetupHelper.AdjustCenterOfMass(
                ref infoComponent.colliderParts, infoComponent.template.centerOfMassAdjustmentParameters);

            // 4
            {
                var carDataModel = new Gameplay.CarDataModel(carSpec, infoComponent);
                carProperties.Initialize(carDataModel);
            }
        }

        /// <summary>
        /// </summary>
        // TODO:
        // This will take a few other things eventually.
        // This one is only called through interface, because the Garage assembly must not reference
        // this script directly.
        public void TransitionToGameplaySceneFromGarage(
            // This will have to be refactored when multiple users can participate.
            // It's good to keep the flexibility for possible multiplayer later, but 
            // it will still need tweeking if added.
            Garage.CarProperties carProperties,
            Garage.UserDataModel userDataModel)
        {
            GameplayInitializationInfo info;

            var playerInfo = new PlayerInfo
            {
                carIndex = carProperties.CurrentCarIndex,
                carDataModel = carProperties.CurrentCarInfo.dataModel,
                userDataModel = userDataModel,
            };
            // TODO: create a span from the thing directly.
            // https://docs.microsoft.com/en-us/dotnet/api/system.runtime.interopservices.memorymarshal.createspan?view=net-6.0
            info.playerInfos = new PlayerInfo[] { playerInfo }.AsSpan();

            var botInfo = new BotInfo
            {
                carIndex = 0,
            };
            info.botInfos = new BotInfo[] { botInfo }.AsSpan();

            // TODO:
            // Create the scene from addressable? Or just create it dynamically?
            // I actually think keeping the scene content within subobjects would have been way simpler.
            // So like not loading new scenes at all, just hiding or unhiding the subobjects,
            // perhaps removing certain objects or creating new ones.
            Transform sceneRoot = null;

            // How do we integrate the loading screen here?
            InitializeGameplaySceneWithConfiguration(sceneRoot, info);
        }
    }
}

using UnityEngine;
using Race.Gameplay;
using System;
using static EngineCommon.Assertions;

namespace Race.Gameplay
{
    public readonly struct PlayerDriverInfo
    {
        // The car is disabled at this point in time.
        public readonly GameObject car;
        public readonly CarProperties carProperties;

        public PlayerDriverInfo(GameObject car, CarProperties carProperties)
        {
            this.car = car;
            this.carProperties = carProperties;
        }
    }

    public readonly struct BotDriverInfo
    {
        // The car is disabled at this point in time.
        public readonly GameObject car;
        public readonly CarProperties carProperties;

        public BotDriverInfo(GameObject car, CarProperties carProperties)
        {
            this.car = car;
            this.carProperties = carProperties;
        }
    }

    public struct GameplayInitializationInfo
    {
        public Transform rootTransform;
        public PlayerDriverInfo[] playerDriverInfos;
        public BotDriverInfo[] botDriverInfos;
    }

    public interface IGameplayInitialization
    {
        IEnableDisableInput Initialize(in GameplayInitializationInfo info);
    }

    public class GameplayInitialization : MonoBehaviour, IGameplayInitialization
    {
        [SerializeField] private GameObject _cameraControlPrefab;
        [SerializeField] private KeyboardInputViewFactory _factory;
        private GameplayInitializationInfo _initializationInfo;

        public IEnableDisableInput Initialize(in GameplayInitializationInfo info)
        {
            _initializationInfo = info;

            assert(info.playerDriverInfos.Length == 1);
            assert(info.botDriverInfos.Length == 1);

            var cameraControlGameObject = GameObject.Instantiate(_cameraControlPrefab);
            cameraControlGameObject.transform.SetParent(info.rootTransform, worldPositionStays: false);

            var cameraControl = cameraControlGameObject.GetComponent<CameraControl>();

            var carContainer = new GameObject("car_container").transform;
            carContainer.SetParent(info.rootTransform, worldPositionStays: false);

            // TODO:
            // 1. Map
            // 2. Spawn points on the map
            // 3. Place the cars at those points on init
            // 4. Set the road data for the bot
            // 5. other stuff.

            {
                ref var playerInfo = ref info.playerDriverInfos[0];
                var car = playerInfo.car;
                var carProperties = playerInfo.carProperties;

                InitializationHelper.InitializePlayerInput(car, carProperties, cameraControl, _factory);

                car.transform.SetParent(carContainer, worldPositionStays: false);
                car.name = "player";
            }

            {
                ref var botInfo = ref info.botDriverInfos[0];
                var car = botInfo.car;
                var carProperties = botInfo.carProperties;

                {
                    // TODO: settings for difficulty and such
                    var carInputView = new BotInputView();

                    var carController = car.GetComponent<CarController>();
                    Gameplay.InitializationHelper.InitializeCarController(carInputView, carController, carProperties);
                }

                car.transform.SetParent(carContainer, worldPositionStays: false);
                car.name = "bot";
            }

            return _factory as IEnableDisableInput;
        }
    }
}
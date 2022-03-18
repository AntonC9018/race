using UnityEngine;
using Race.Gameplay;
using System;
using static EngineCommon.Assertions;

namespace Race.Gameplay
{
    public readonly struct PlayerDriverInfo
    {
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
        public readonly GameObject car;
        public readonly CarProperties carProperties;

        public BotDriverInfo(GameObject car, CarProperties carProperties)
        {
            this.car = car;
            this.carProperties = carProperties;
        }
    }

    public ref struct GameplayInitializationInfo
    {
        public Transform rootTransform;
        public Span<PlayerDriverInfo> playerDriverInfos;
        public Span<BotDriverInfo> botDriverInfos;
    }

    public interface IGameplayInitialization
    {
        IEnableDisableInput Initialize(in GameplayInitializationInfo info);
    }

    public class GameplayInitialization : MonoBehaviour
    {
        [SerializeField] private GameObject _cameraControlPrefab;
        [SerializeField] private KeyboardInputViewFactory _factory;

        public IEnableDisableInput Initialize(in GameplayInitializationInfo info)
        {
            assert(info.playerDriverInfos.Length == 1);
            assert(info.botDriverInfos.Length == 1);

            var cameraControlGameObject = GameObject.Instantiate(_cameraControlPrefab);
            cameraControlGameObject.transform.SetParent(info.rootTransform, worldPositionStays: false);

            var cameraControl = cameraControlGameObject.GetComponent<CameraControl>();

            {
                ref var playerInfo = ref info.playerDriverInfos[0];
                var car = playerInfo.car;
                var carProperties = playerInfo.carProperties;

                InitializationHelper.InitializePlayerInput(playerInfo.car, playerInfo.carProperties, cameraControl, _factory);
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
            }

            return _factory as IEnableDisableInput;
        }
    }
}
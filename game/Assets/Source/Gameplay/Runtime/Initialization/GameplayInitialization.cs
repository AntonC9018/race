using UnityEngine;
using Race.Gameplay;
using System;
using static EngineCommon.Assertions;
using System.Linq;
using Kari.Plugins.Terminal;

namespace Race.Gameplay
{
    public readonly struct DriverInfo
    {
        // The car is disabled at this point in time.
        public readonly GameObject car;
        public Transform transform => car.transform;
        public readonly CarProperties carProperties;

        public DriverInfo(GameObject car, CarProperties carProperties)
        {
            this.car = car;
            this.carProperties = carProperties;
        }
    }

    public struct GameplayExternalInitializationInfo
    {
        public Transform rootTransform;
        public DriverInfo[] driverInfos;
        public int playerCount;
        public int botCount;

        // For now, we only allow simple quad track.
        public Transform trackQuadTransform;
        public GameObject mapGameObject;


        // Encapsulate it for now
        public GameplayExternalInitializationInfo(
            Transform rootTransform,
            DriverInfo[] playerInfos,
            DriverInfo[] botInfos,
            Transform trackQuad,
            GameObject mapGameObject)
        {
            this.rootTransform = rootTransform;
            this.driverInfos = playerInfos.Concat(botInfos).ToArray();
            this.playerCount = playerInfos.Length;
            this.botCount = botInfos.Length;
            this.trackQuadTransform = trackQuad;
            this.mapGameObject = mapGameObject;
        }

        public Span<DriverInfo> PlayerInfos => driverInfos.AsSpan(0, playerCount);
        public Span<DriverInfo> BotInfos => driverInfos.AsSpan(playerCount, botCount);

    }

    public interface IGameplayInitialization
    {
        IEnableDisableInput Initialize(in GameplayExternalInitializationInfo info);
    }

    public class GameplayInitialization : MonoBehaviour, IGameplayInitialization
    {
        [SerializeField] private CommonInitializationStuff _commonStuff;
        [SerializeField] private GameObject _cameraControlPrefab;

        private TrackManager _trackManager;
        private GameplayExternalInitializationInfo _initializationInfo;


        public IEnableDisableInput Initialize(in GameplayExternalInitializationInfo info)
        {
            _initializationInfo = info;

            assert(info.PlayerInfos.Length == 1);
            assert(info.BotInfos.Length == 1);

            var carContainer = new GameObject("car_container").transform;
            carContainer.SetParent(info.rootTransform, worldPositionStays: false);

            // Initialize track
            var (track, _trackManager) = InitializationHelper.InitializeTrackAndTrackManagerFromTrackQuad(
                info.driverInfos, info.trackQuadTransform);
            info.mapGameObject.SetActive(true);

            // Initialize players & UI
            {
                ref var playerInfo = ref info.PlayerInfos[0];
                var car = playerInfo.car;
                var carProperties = playerInfo.carProperties;

                car.transform.SetParent(carContainer, worldPositionStays: false);
                car.name = "player";

                CameraControl cameraControl;
                {
                    var cameraControlGameObject = GameObject.Instantiate(_cameraControlPrefab);
                    cameraControlGameObject.transform.SetParent(info.rootTransform, worldPositionStays: false);
                    cameraControl = cameraControlGameObject.GetComponent<CameraControl>();
                }

                InitializationHelper.InitializePlayerInputAndInjectDependencies(
                    _commonStuff, cameraControl, car, carProperties);

                car.SetActive(true);
            }

            // Initialize bots
            {
                ref var botInfo = ref info.BotInfos[0];
                var car = botInfo.car;
                var carProperties = botInfo.carProperties;
                var carController = car.GetComponent<CarController>();

                car.transform.SetParent(carContainer, worldPositionStays: false);
                car.name = "bot";

                {
                    // TODO: settings for difficulty and such
                    var carInputView = new BotInputView();
                    carInputView.Track = track;
                    Gameplay.InitializationHelper.InitializeCarController(carInputView, carController, carProperties);
                }

                car.SetActive(true);
            }

            return _commonStuff.inputViewFactory as IEnableDisableInput;
        }


        [Command(Name = "flip", Help = "Flips a car upside down.")]
        public static void FlipOver(
            [Argument("Which participant to flip over")] int participantIndex = 0)
        {
            var initialization = GameObject.FindObjectOfType<GameplayInitialization>();
            if (initialization == null)
            {
                Debug.LogError("The initialization could not be found");
                return;
            }

            if (participantIndex < 0 || participantIndex >= initialization._initializationInfo.driverInfos.Length)
            {
                Debug.Log($"The participant index {participantIndex} was outside the bound of the participant array");
                return;
            }

            var t = initialization._initializationInfo.driverInfos[participantIndex].transform;
            t.rotation = Quaternion.AngleAxis(180, Vector3.forward);
            t.position += Vector3.up * 3;
        }
    }
}
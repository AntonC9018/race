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
        public DriverInfo[] playerInfos;
        public DriverInfo[] botInfos;
        public GameObject mapGameObject;
    }

    public interface IGameplayInitialization
    {
        IEnableDisableInput Initialize(in GameplayExternalInitializationInfo info);
    }

    public class GameplayInitialization : MonoBehaviour, IGameplayInitialization
    {
        [SerializeField] private CommonInitializationStuff _commonStuff;
        [SerializeField] private GameObject _cameraControlPrefab;
        [SerializeField] private RaceProperties _raceProperties;
        private Transform _rootTransform;


        public IEnableDisableInput Initialize(in GameplayExternalInitializationInfo info)
        {

            _initializationInfo = info;

            assert(info.playerInfos.Length == 1);
            assert(info.botInfos.Length == 1);

            var carContainer = new GameObject("car_container").transform;
            carContainer.SetParent(info.rootTransform, worldPositionStays: false);

            // Initialize track
            info.mapGameObject.SetActive(true);
            InitializeRaceProperties(_raceProperties, info);

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

            RaceDataModelHelper.PlaceParticipants(_raceProperties.DataModel);

            return _commonStuff.inputViewFactory as IEnableDisableInput;
        }

        private static void InitializeRaceProperties(RaceProperties raceProperties, in GameplayExternalInitializationInfo info)
        {
            var model = raceProperties.DataModel;

            {
                var mapTransform = info.mapGameObject.transform;
                model.mapTransform = mapTransform;

                var trackTransform = mapTransform.Find("track");
                model.trackTransform = trackTransform;

                {
                    ref var driver = ref model.participants.driver;
                    driver.Reset(info.playerInfos, info.botInfos);
                    RaceDataModelHelper.ResizeTrackParticipantDataToParticipantDriverData(ref model.participants);
                }

                var (track, actualWidth) = TrackHelper.CreateFromQuad(trackTransform);

                const float visualWidthScale = 1.2f;
                var visualWidth = visualWidthScale * actualWidth;

                model.trackInfo = new TrackRaceInfo
                {
                    track = track,
                    actualWidth = actualWidth,
                    visualWidth = visualWidth,
                };
            }
        }

        // TODO: This needs refactoring, but for now, just tick it manually here.
        // `RaceDataModel` with the participants, events for death, winning etc.
        // It should definitely not be done in initialization.
        void Update()
        {
            _raceManager.Update();
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
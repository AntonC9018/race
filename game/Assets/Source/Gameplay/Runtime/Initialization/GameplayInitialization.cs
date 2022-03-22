using UnityEngine;
using static EngineCommon.Assertions;
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
        public DriverInfo[] playerInfos;
        public DriverInfo[] botInfos;

        public GameObject mapGameObject;
        public Transform rootTransform;
        public ITransitionFromGameplayToGarage transitionHandler;
    }

    public interface IGameplayInitialization
    {
        IEnableDisableInput Initialize(in GameplayExternalInitializationInfo info);
    }

    public class GameplayInitialization : MonoBehaviour, IGameplayInitialization
    {
        [SerializeField] private CommonInitializationStuffComponent _commonStuff;
        [SerializeField] private GameObject _cameraControlPrefab;
        [SerializeField] private GameplayToGarageTransitionManager _transitionManager;
        private Transform _rootTransform;

        public IEnableDisableInput Initialize(in GameplayExternalInitializationInfo info)
        {
            ref var commonStuff = ref _commonStuff.stuff;
            
            var carContainer = new GameObject("car_container");
            var carContainerTransform = carContainer.transform;
            // carContainer.SetParent(info.rootTransform, worldPositionStays: false);

            // Initialize track & race participants
            var raceModel = new RaceDataModel();
            {
                // `ref` is used for the sake of symmerty with structs (it's a class)
                ref var model = ref raceModel;

                var mapTransform = info.mapGameObject.transform;
                model.mapTransform = mapTransform;

                var trackTransform = InitializationHelper.FindTrackTransform(mapTransform);
                model.trackTransform = trackTransform;

                RaceDataModelHelper.SetParticipants(model, info.playerInfos, info.botInfos);

                var trackInfo = InitializationHelper.CreateTrackWithInfo(trackTransform, commonStuff.trackLimits);
                model.trackInfo = trackInfo;
            }

            // Initialize race logic
            var raceProperties = commonStuff.raceProperties;
            {
                var raceLogicTransform = commonStuff.raceLogicTransform;
                raceProperties.Initialize(raceModel);

                InitializationHelper.InjectDependency(raceLogicTransform, raceProperties);

                // For now, initialize the update thing manually.
                var updateTracker = commonStuff.raceUpdateTracker;
                assert(updateTracker != null);

                updateTracker.Initialize(raceProperties, commonStuff.respawnDelay, _transitionManager);
            }

            // Initialize players & UI
            {
                assert(info.playerInfos.Length == 1);
                ref var playerInfo = ref info.playerInfos[0];
                var car = playerInfo.car;
                var carProperties = playerInfo.carProperties;

                car.transform.SetParent(carContainerTransform, worldPositionStays: false);
                car.name = "player";

                CameraControl cameraControl;
                {
                    var cameraControlGameObject = GameObject.Instantiate(_cameraControlPrefab);
                    cameraControlGameObject.transform.SetParent(carContainerTransform, worldPositionStays: false);

                    cameraControl = cameraControlGameObject.GetComponent<CameraControl>();
                }

                InitializationHelper.InitializePlayerInputAndInjectDependencies(
                    commonStuff, cameraControl, car, carProperties);

                car.SetActive(true);
            }

            // Initialize bots
            for (int i = 0; i < info.botInfos.Length; i++)
            {
                ref var botInfo = ref info.botInfos[i];
                var car = botInfo.car;
                var carProperties = botInfo.carProperties;
                var carController = car.GetComponent<CarController>();

                car.transform.SetParent(carContainerTransform, worldPositionStays: false);
                car.name = "bot";

                {
                    // TODO: settings for difficulty and such
                    var carInputView = new BotInputView();
                    carInputView.RaceProperties = raceProperties;
                    carInputView.OwnIndex = i + info.playerInfos.Length;

                    Gameplay.InitializationHelper.InitializeCarController(carInputView, carController, carProperties);
                }

                car.SetActive(true);
            }

            RaceDataModelHelper.PlaceParticipants(raceProperties.DataModel);
            info.mapGameObject.SetActive(true);

            var finalizeInfo = new FinalizeGameplayInfo
            {
                carContainer = carContainer,
                mapContainer = info.mapGameObject,
                transitionHandler = info.transitionHandler,
            };
            _transitionManager.FinalizeInfo = finalizeInfo;

            return (IEnableDisableInput) commonStuff.inputViewFactory;
        }


        [Command(Name = "flip", Help = "Flips a car upside down.")]
        public static void FlipOver(
            [Argument("Which participant to flip over")] int participantIndex = 0)
        {
            var raceProperties = GameObject.FindObjectOfType<RaceProperties>();
            if (raceProperties == null)
            {
                Debug.LogError("RaceProperties could not be found");
                return;
            }

            var driverInfos = raceProperties.DataModel.participants.driver.infos;
            if (participantIndex < 0 || participantIndex >= driverInfos.Length)
            {
                Debug.Log($"The participant index {participantIndex} was outside the bound of the participant array");
                return;
            }

            var t = driverInfos[participantIndex].transform;
            t.rotation = Quaternion.AngleAxis(180, Vector3.forward);
            t.position += Vector3.up * 3;
        }
    }
}
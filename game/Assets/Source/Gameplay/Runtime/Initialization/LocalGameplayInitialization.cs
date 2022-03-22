using UnityEngine;
using Race.Gameplay;
using System;
using static EngineCommon.Assertions;

namespace Race.Gameplay
{
    // TODO:
    // For now keep it here, but the transition script should be separated
    // into the transition and initialization parts, then I could move this
    // into the Gameplay assembly. 

    // The purpose of this script is to initialize the gameplay scene
    // if we happen to load the game from that scene.
    // Only useful for the editor.
    public class LocalGameplayInitialization : MonoBehaviour
    {
        [SerializeField] private CommonInitializationStuffComponent _commonStuff;
        [SerializeField] private GameObject _car;
        [SerializeField] private CameraControl _cameraControl;
        [SerializeField] private Transform _trackQuad;

        private class _RaceEndedHandler : IOnRaceEnded
        {
            public void OnRaceEnded(int winnerIndex, RaceProperties raceProperties)
            {
                Debug.Log("The scenes transitions are not implemented in the local gameplay version.");
            }
        }

        void Start()
        {
            if (_car == null)
                return;

            ref var commonStuff = ref _commonStuff.stuff;

            assert(commonStuff.inputViewFactory != null);
            assert(_cameraControl != null);
            assert(commonStuff.diRootTransform != null);
            assert(commonStuff.raceProperties != null);

            // Initialize player
            var playerCar = _car;
            var carProperties = playerCar.GetComponent<Gameplay.CarProperties>();
            {
                var infoComponent = playerCar.GetComponent<CarInfoComponent>();
                Gameplay.InitializationHelper.FinalizeCarPropertiesInitializationWithDefaults(carProperties, infoComponent, playerCar.transform);

                InitializationHelper.InitializePlayerInputAndInjectDependencies(
                    commonStuff, _cameraControl, playerCar, carProperties);
            }

            commonStuff.inputViewFactory.EnableAllInput();

            // Initialize track
            var raceModel = new RaceDataModel();
            {
                // `ref` is used for the sake of symmerty with structs (it's a class)
                ref var model = ref raceModel;

                var mapTransform = _trackQuad.parent;
                model.mapTransform = mapTransform;

                var trackTransform = _trackQuad;
                model.trackTransform = trackTransform;

                {
                    var driverInfos = new[] { new DriverInfo(playerCar, carProperties), };
                    RaceDataModelHelper.SetParticipants(model, driverInfos, Array.Empty<DriverInfo>());
                }
                
                var trackInfo = InitializationHelper.CreateTrackWithInfo(trackTransform, commonStuff.trackLimits);
                model.trackInfo = trackInfo;
            }
            
            var raceProperties = commonStuff.raceProperties;
            raceProperties.Initialize(raceModel);

            // Initialize race logic
            {
                var raceLogicTransform = commonStuff.raceLogicTransform;
                assert(raceLogicTransform != null);

                InitializationHelper.InjectDependency(raceLogicTransform, raceProperties);
                
                commonStuff.raceUpdateTracker.Initialize(raceProperties, commonStuff.respawnDelay, new _RaceEndedHandler());
            }
        }
    }
}
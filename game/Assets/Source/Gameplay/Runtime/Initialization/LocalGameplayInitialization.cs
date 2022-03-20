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
        [SerializeField] private CommonInitializationStuff _commonStuff;
        [SerializeField] private GameObject _car;
        [SerializeField] private CameraControl _cameraControl;
        [SerializeField] private Transform _trackQuad;
        private TrackManager _trackManager;

        void Start()
        {
            if (_car == null)
                return;

            var commonStuff = _commonStuff;

            assert(commonStuff.inputViewFactory != null);
            assert(_cameraControl != null);
            assert(commonStuff.diRootTransform != null);
            assert(_trackManager == null);

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
            {
                var driverInfos = new[] { new DriverInfo(playerCar, carProperties), };
                var (_, _trackManager) = InitializationHelper.InitializeTrackAndTrackManagerFromTrackQuad(
                    driverInfos, _trackQuad);
            }
        }
    }
}
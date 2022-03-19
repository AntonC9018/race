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
        [SerializeField] private GameObject _car;
        [SerializeField] private KeyboardInputViewFactory _inputViewFactory;
        [SerializeField] private CameraControl _cameraControl;
        [SerializeField] private Transform _uiTransform;

        void Start()
        {
            if (_car == null)
                return;
            
            var playerCar = _car;
            var carProperties = playerCar.GetComponent<Gameplay.CarProperties>();
            {
                var infoComponent = playerCar.GetComponent<CarInfoComponent>();
                Gameplay.InitializationHelper.FinalizeCarPropertiesInitializationWithDefaults(carProperties, infoComponent);
                InitializationHelper.InitializePlayerInput(playerCar, carProperties, _cameraControl, _inputViewFactory);
                _inputViewFactory.EnableAllInput();
            }
            InitializationHelper.InitializeUI(_uiTransform, carProperties);
        }
    }
}
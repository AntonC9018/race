using UnityEngine;
using Race.Gameplay;
using System;
using static EngineCommon.Assertions;

namespace Race.SceneTransition
{
    // TODO:
    // For now keep it here, but the transition script should be separated
    // into the transition and initialization parts, then I could move this
    // into the Gameplay assembly. 

    // The purpose of this script is to initialize the gameplay scene
    // if we happen to load the game from that scene.
    // Only useful for the editor.
    public class GameplayInitialization : MonoBehaviour
    {
        [SerializeField] private GameObject _car;
        [SerializeField] private Transition _transition;

        void Start()
        {
            if (_car == null)
                return;
            
            assert(_transition != null);
            _transition.InitializePlayerCar(_car);
        }
    }
}
using UnityEngine;

namespace Race.Gameplay
{
    public interface ITransitionFromGameplayToGarage
    {
        void TransitionFromGameplayToGarage();
    }

    public struct FinalizeGameplayInfo
    {
        public GameObject carContainer;
        public GameObject mapContainer;
        public ITransitionFromGameplayToGarage transitionHandler;
    }

    public class GameplayToGarageTransitionManager : MonoBehaviour, IOnRaceEnded
    {
        public FinalizeGameplayInfo _finalizeInfo;
        public FinalizeGameplayInfo FinalizeInfo { set { _finalizeInfo = value; } }

        public void OnRaceEnded(int winnerIndex, RaceProperties raceProperties)
        {
            if (winnerIndex < raceProperties.DataModel.participants.driver.playerCount)
                Debug.Log("Congratulations, player won.");

            // Transition to garage scene
            HandleTransitionToGarageScene(raceProperties);
        }

        private void HandleTransitionToGarageScene(RaceProperties raceProperties)
        {
            // Have to clear all events, since we reinitialize on start.
            raceProperties.RemoveAllListenersToAllEvents();
            
            // These are always renewed.
            GameObject.Destroy(_finalizeInfo.carContainer);
            GameObject.Destroy(_finalizeInfo.mapContainer);

            _finalizeInfo.transitionHandler.TransitionFromGameplayToGarage();
        }
    }
}
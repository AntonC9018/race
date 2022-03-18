using UnityEngine;
using static EngineCommon.Assertions;

namespace Race.Garage
{
    public class StartButton : MonoBehaviour
    {
        // Cannot serialize interfaces directly.
        // But this hack works.
        [SerializeField] private MonoBehaviour _transition;
        private ITransitionToGameplaySceneFromGarage Transition => (ITransitionToGameplaySceneFromGarage) _transition; 
        
        [SerializeField] private CarProperties _carProperties;
        [SerializeField] private UserProperties _userProperties;

        void OnValidate()
        {
            assert(_transition == null || _transition is ITransitionToGameplaySceneFromGarage);
        }

        public void OnButtonClicked()
        {
            Transition.TransitionToGameplaySceneFromGarage(_carProperties, _userProperties.DataModel);
        }
    }
}
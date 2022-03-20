using UnityEngine;
using static EngineCommon.Assertions;

namespace Race.Gameplay
{
    #if UNITY_EDITOR
    public class CarColliderSetupComponent : MonoBehaviour
    {
        [SerializeField] internal GameObject _wheelColliderPrefab;
        [SerializeField] internal Transform _targetRootTransform;
        [SerializeField] internal CarInfoComponent _carToUpdate;

        // TODO: should really be a button, but Unity can't do that with just a UDA.
        [ContextMenuItem("Create default colliders", nameof(CreateDefaultColliders))]
        [ContextMenuItem("Update elevation", nameof(UpdateElevation))]
        public bool RightClickMe;

        void CreateDefaultColliders()
        {
            CarColliderSetupHelper.CreateDefaultColliders(_carToUpdate, _targetRootTransform, _wheelColliderPrefab);
            Debug.Log("The colliders have been set up.");
        }

        void UpdateElevation()
        {
            // I'm just going to put this in here for now.
            _carToUpdate.elevationSuchThatWheelsAreLevelWithTheGround = _targetRootTransform.localPosition.y;
        }

        // TODO
        void FindColliders()
        {
            CarColliderSetupHelper.FindColliders(_carToUpdate, _targetRootTransform);
            Debug.Log("The colliders have been set up.");
        }
    }
    #endif
}

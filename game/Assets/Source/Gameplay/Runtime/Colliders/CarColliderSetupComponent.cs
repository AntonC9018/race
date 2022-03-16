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
        public bool RightClickMe;

        void CreateDefaultColliders()
        {
            CarColliderSetupHelper.CreateDefaultColliders(_carToUpdate, _targetRootTransform, _wheelColliderPrefab);
            Debug.Log("The colliders have been set up.");
        }
    }
    #endif
}

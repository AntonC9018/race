using UnityEngine;
using static EngineCommon.Assertions;

namespace Race.Gameplay
{
    // I expect that another script would get this info, then forget about the component.
    // So this script just helps with the creation of the colliders.
    #if UNITY_EDITOR
    public class CarColliderSetupComponent : MonoBehaviour
    {
        [SerializeField] internal GameObject _wheelColliderPrefab;
        [SerializeField] internal Transform _targetRootTransform;
        [SerializeField] internal CarProperties _propertiesToUpdate;

        // TODO: should really be a button, but Unity can't do that with just a UDA.
        [ContextMenuItem("Create default colliders", nameof(CreateDefaultColliders))]
        public bool RightClickMe;

        void CreateDefaultColliders()
        {
            CarColliderSetupHelper.CreateDefaultColliders(_propertiesToUpdate, _targetRootTransform, _wheelColliderPrefab);
            Debug.Log("The colliders have been set up.");
        }
    }
    #endif
}

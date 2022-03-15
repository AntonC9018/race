using UnityEngine;

namespace Race.Gameplay
{
    [System.Serializable]
    [CreateAssetMenu(
        fileName = "New " + nameof(CarPrefabInfo),
        menuName = nameof(CarPrefabInfo),
        // Second grouping.
        order = 51)]
    public class CarPrefabInfo : ScriptableObject
    {
        public CarColliderSetupHelper.CenterOfMassAdjustmentParameters centerOfMassAdjustmentParameters;
        public CarSpecInfo initialSpec;
        public CarColliderParts colliderParts;
        public CarVisualParts visualParts;
    }
}
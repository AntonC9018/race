using UnityEngine;

namespace Race.Garage
{
    [System.Serializable]
    public struct GarageCommonInitializationStuff
    {
        public CarProperties carProperties;
        public UserProperties userProperties;
        public Transform diRoot;
    }

    public class GarageCommonInitializationStuffComponent : MonoBehaviour
    {
        public GarageCommonInitializationStuff stuff;   
    }
}
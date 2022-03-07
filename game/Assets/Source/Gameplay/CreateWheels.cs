using UnityEngine;
using static EngineCommon.Assertions;

namespace Race.Gameplay
{
    public enum WheelLocation
    {
        RightBit = 1,
        FrontBit = 2,

        BackLeft = 0,
        BackRight = RightBit,
        FrontLeft = FrontBit,
        FrontRight = FrontBit | RightBit,
    }
    
    public static class WheelsHelper
    {
        public static readonly string[] _WheelNames;
        static WheelsHelper()
        {
            _WheelNames = new string[4];
            _WheelNames[(int) WheelLocation.BackLeft]   = "back_left";
            _WheelNames[(int) WheelLocation.BackRight]  = "back_right";
            _WheelNames[(int) WheelLocation.FrontLeft]  = "front_left";
            _WheelNames[(int) WheelLocation.FrontRight] = "front_right";
        }

        // 1. Blender wrong orientation of the car (need to flip x and z)
        // 2. Apply the scale of the wheels so that it's 0.
        // 3. query the "model" child (instead of Chilren[0]).
        // 4. The model prefab exported from blender is repositioned from 0. WHAT? move it within the parent instead.
        // 5. Make the wheel's collider into a prefab, because it has quite a lot of properties.
        public static Transform CreateWheelsGroup(Transform parent, Transform meshContainer, Rigidbody rigidbody)
        {
            float radius;
            Vector3 center;
            {
                var firstWheel = meshContainer.Find(_WheelNames[0]);
                assert(firstWheel != null);
                var wheelBounds = firstWheel.transform.GetComponent<MeshFilter>().sharedMesh.bounds;
                // REALLY fragile! What if the model is not rotated? we can't make sure.
                radius = (wheelBounds.extents - wheelBounds.center).y;
                center = wheelBounds.center;
            }

            var container = new GameObject("container");
            var containerTransform = container.transform;
            containerTransform.hierarchyCapacity = _WheelNames.Length;
            containerTransform.SetParent(parent, worldPositionStays: false);
            
            for (int i = 0; i < _WheelNames.Length; i++)
            {
                var wheelName = _WheelNames[i];
                var wheel = new GameObject(wheelName);
                var wheelTransform = wheel.transform;
                var meshWheelTransform = meshContainer.Find(wheelName);

                {
                    wheelTransform.SetPositionAndRotation(meshWheelTransform.position, meshWheelTransform.rotation);
                    // wheelTransform.position = meshWheelTransform.position;
                    // wheelTransform.localRotation = meshWheelTransform.localRotation;
                    wheelTransform.SetParent(containerTransform, worldPositionStays: true);
                }
                {
                    var wheelCollider = wheel.AddComponent<WheelCollider>();
                    wheelCollider.radius = radius;
                    wheelCollider.center = new Vector3(center.x, center.y + wheelCollider.suspensionDistance / 2, center.z);
                }
            }

            return containerTransform;
        }
    }
    public class CreateWheels : MonoBehaviour
    {
        // hack
        [ContextMenuItem("Run", "Run")]
        [SerializeField] private Transform _containerTransform;

        void Run()
        {
            var parent = transform;
            
            var rigidbody = GetComponent<Rigidbody>();
            assert(rigidbody != null);
            
            // TODO: breadth-first seach by name?? or just say that the model prefab goes first.
            var wheels = parent.GetChild(0).Find("wheels");
            assert(wheels != null);

            WheelsHelper.CreateWheelsGroup(parent, wheels, rigidbody);
        }
    }
}

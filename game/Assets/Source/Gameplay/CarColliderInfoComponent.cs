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
    
    public static class WheelHelper
    {
        public static readonly string[] WheelNames;
        static WheelHelper()
        {
            WheelNames = new string[4];
            WheelNames[(int) WheelLocation.BackLeft]   = "back_left";
            WheelNames[(int) WheelLocation.BackRight]  = "back_right";
            WheelNames[(int) WheelLocation.FrontLeft]  = "front_left";
            WheelNames[(int) WheelLocation.FrontRight] = "front_right";
        }
    }

    [System.Serializable]
    public struct ColliderInfo<TCollider>
    {
        public Transform transform;
        public TCollider collider;
    }

    [System.Serializable]
    public class CarColliderInfo
    {
        public Transform container;
        public ColliderInfo<BoxCollider> body;

        // The wheels are positioned according to WheelLocation.
        public ColliderInfo<WheelCollider>[] wheels;

        public ref ColliderInfo<WheelCollider> GetWheel(WheelLocation location)
        {
            return ref wheels[(int) location];
        }
    }

    // I expect that another script would get this info, then forget about the component.
    // So this script just helps with the creation of the colliders.
    // "But the runtime overhead!". Well, I don't know how to handle that, really.

    public class CarColliderInfoComponent : MonoBehaviour
    {
        // hack
        [ContextMenuItem("Create default colliders", nameof(CreateDefaultColliders))]
        [SerializeField] internal CarColliderInfo _carColliderInfo;
        [SerializeField] internal GameObject _wheelColliderPrefab;

        public CarColliderInfo CarColliderInfo => _carColliderInfo;

        private void CreateDefaultColliders()
        {
            // Parents
            Transform parent;
            Transform carTransform;
            {
                var transform = this.transform;
                parent = new GameObject("colliders").transform;
                parent.SetParent(transform, worldPositionStays: false);
                _carColliderInfo.container = parent;

                assert(transform.childCount >= 1, "We expect the parent object to contain the car model.");
                carTransform = transform.GetChild(0);
            }

            // Wheels
            {
                var wheels = carTransform.Find("wheels");
                assert(wheels != null, "Must have a `wheels` child with wheels.");

                var outColliderInfos = new ColliderInfo<WheelCollider>[4];
                CreateWheelColliderGameObjectsFromWheelMeshes(parent, wheels, _wheelColliderPrefab, outColliderInfos);
                _carColliderInfo.wheels = outColliderInfos;

                // 5. Make the wheel's collider into a prefab, because it has quite a lot of properties.
                static void CreateWheelColliderGameObjectsFromWheelMeshes(
                    Transform parent,
                    Transform wheelMeshesContainer,
                    GameObject wheelColliderPrefab,
                    ColliderInfo<WheelCollider>[] outColliderInfos)
                {
                    var wheelNames = WheelHelper.WheelNames;
                    
                    // Validation
                    {
                        assert(outColliderInfos.Length == wheelNames.Length);
                        assert(wheelMeshesContainer.childCount == outColliderInfos.Length);
                        assert(wheelColliderPrefab.transform.childCount == 0,
                            "We expect the wheels to have no children.");
                        {
                            var collider = wheelColliderPrefab.GetComponent<WheelCollider>();
                            assert(collider != null, "The prefab must have a wheel collider");

                            // Don't need this check, because it finds the rigidbody only at runtime anyway.
                            // assert(collider.enabled == false,
                            //     "Disable the wheel collider of the prefab, because it only works when there's a rigidbody.");
                        }
                    }

                    // May want to factor this part out if we'll need to reuse it.
                    float radius;
                    Vector3 center;
                    {
                        var firstWheel = wheelMeshesContainer.GetChild(0);
                        var meshFilter = firstWheel.GetComponent<MeshFilter>();
                        assert(meshFilter != null, "No mesh filter on the model's wheel.");

                        var wheelBounds = meshFilter.sharedMesh.bounds;
                        // REALLY fragile! What if the model is not rotated? we can't make sure.
                        // Though, actually, the wheel colliders always assume +Z is front, so they must not be rotated
                        // (unless the model itself has been rotated, but I cannot visualize that.
                        // TODO: may want to assert these are the same for all wheels, because that's what this code expects.
                        radius = (wheelBounds.extents - wheelBounds.center).y;
                        center = wheelBounds.center;
                    }

                    var containerTransform = new GameObject("wheels").transform;
                    // The wheel objects have no children.
                    containerTransform.hierarchyCapacity = wheelNames.Length;
                    containerTransform.SetParent(parent, worldPositionStays: false);
                    
                    // We'll probably also want to sort the given hierarchy?
                    for (int i = 0; i < wheelNames.Length; i++)
                    {
                        var wheelName = wheelNames[i];
                        
                        var wheelColliderGameObject = GameObject.Instantiate(wheelColliderPrefab);
                        wheelColliderGameObject.name = wheelName;

                        var wheelColliderTransform = wheelColliderGameObject.transform;
                        var meshWheelTransform = wheelMeshesContainer.Find(wheelName);

                        {
                            // TODO: will this work if the rotation is not identity?
                            wheelColliderTransform.SetPositionAndRotation(meshWheelTransform.position, meshWheelTransform.rotation);
                            // wheelTransform.position = meshWheelTransform.position;
                            // wheelTransform.localRotation = meshWheelTransform.localRotation;
                            wheelColliderTransform.SetParent(containerTransform, worldPositionStays: true);
                        }

                        var wheelCollider = wheelColliderGameObject.GetComponent<WheelCollider>();
                        {
                            wheelCollider.radius = radius;
                            wheelCollider.center = new Vector3(center.x, center.y + wheelCollider.suspensionDistance / 2, center.z);
                            // In case it was disabled.
                            wheelCollider.enabled = true;
                        }

                        outColliderInfos[i] = new ColliderInfo<WheelCollider>
                        {
                            transform = wheelColliderTransform,
                            collider = wheelCollider,
                        };
                    }
                }
            }

            // Body BoxCollider
            {
                var bodyMeshTransform = carTransform.Find("body");
                assert(bodyMeshTransform != null, "Must have a `body` child.");

                var body = new GameObject("body");
                var bodyTransform = body.transform;
                bodyTransform.SetParent(parent, worldPositionStays: false);

                var meshFilter = bodyMeshTransform.GetComponent<MeshFilter>();
                assert(meshFilter != null, "The model child must have a mesh filter with the mesh.");

                var mesh = meshFilter.sharedMesh;
                var meshBounds = mesh.bounds;

                var collider = body.AddComponent<BoxCollider>();
                {
                    collider.center = meshBounds.center;
                    collider.size = meshBounds.size;
                }

                _carColliderInfo.body = new ColliderInfo<BoxCollider>()
                {
                    transform = bodyTransform,
                    collider = collider,
                };
            }
        }

        public void AdjustCenterOfMass()
        {
            var centerOfMassAdjustmentVector = GetCenterOfMassAdjustmentVector();

            var rigidbody = GetComponent<Rigidbody>();
            assert(rigidbody != null);
            // As far as I understand, centerOfMass is a runtime thing only (not serialized).
            assert(rigidbody.centerOfMass == Vector3.zero, "What? already adjusted?");
            
            rigidbody.centerOfMass += centerOfMassAdjustmentVector;

            Vector3 GetCenterOfMassAdjustmentVector()
            {
                var bodyCollider = _carColliderInfo.body.collider;
                assert(bodyCollider != null);

                // I expect this to get more complicated eventually
                // (e.g. make the back heavier, adjust according to wheels, etc.)
                // For now, we only lower it a bit for stability.
                float loweringRatio = 0.3f;
                float altitudeLowering = -bodyCollider.bounds.size.y * loweringRatio / 2;

                return new Vector3(0, altitudeLowering, 0);
            }
        }
    }
}

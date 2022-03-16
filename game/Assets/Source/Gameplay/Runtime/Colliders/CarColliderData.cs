using UnityEngine;
using static EngineCommon.Assertions;

namespace Race.Gameplay
{
    [System.Serializable]
    public struct CarVisualParts
    {
        public Transform body;
        public Transform[] wheels;

        // TODO:
        // This one is shared between the two scenes.
        // It should be factored out into another common component.
        public MeshRenderer meshRenderer;
    }

    [System.Serializable]
    public struct CarPart<TCollider>
    {
        public Transform transform;
        public TCollider collider;
    }

    [System.Serializable]
    public struct CarColliderParts
    {
        public Transform container;
        public CarPart<BoxCollider> body;
        public readonly Rigidbody Rigidbody => body.collider.attachedRigidbody; 

        // The wheels are positioned according to WheelLocation.
        public CarPart<WheelCollider>[] wheels;

        public readonly ref CarPart<WheelCollider> GetWheel(WheelLocation location)
        {
            return ref wheels[(int) location];
        }
    }

    public static class CarColliderSetupHelper
    {
        // I expect this to get more complicated eventually
        // (e.g. make the back heavier, adjust according to wheels, etc.)
        // For now, we only lower it a bit for stability.
        [System.Serializable]
        public struct CenterOfMassAdjustmentParameters
        {
            [Range(0, 2)]
            public float loweringRatio; 
        }

        public static void AdjustCenterOfMass(ref this CarColliderParts colliderParts, in CenterOfMassAdjustmentParameters parameters)
        {
            Vector3 centerOfMassAdjustmentVector;
            {
                var bodyCollider = colliderParts.body.collider;
                assert(bodyCollider != null);

                float loweringRatio = parameters.loweringRatio;
                float altitudeLowering = -bodyCollider.bounds.size.y * loweringRatio / 2;

                centerOfMassAdjustmentVector = new Vector3(0, altitudeLowering, 0);
            }

            var rigidbody = colliderParts.Rigidbody;
            assert(rigidbody != null);
            // As far as I understand, centerOfMass is a runtime thing only (not serialized).
            // assert(rigidbody.centerOfMass == Vector3.zero, "What? already adjusted?");
            
            rigidbody.ResetCenterOfMass();
            rigidbody.centerOfMass += centerOfMassAdjustmentVector;
        }

        #if UNITY_EDITOR
        public static void CreateDefaultColliders(CarInfoComponent infoComponent, Transform rootTransform, GameObject wheelPrefab)
        {
            // TODO: currently broken, DataModel does not exist in the editor
            ref var colliderParts = ref infoComponent.colliderParts;
            ref var visualParts = ref infoComponent.visualParts;
            // Parents
            Transform parent;
            Transform carModelTransform;
            {
                var transform = rootTransform;
                parent = new GameObject("colliders").transform;
                parent.SetParent(transform, worldPositionStays: false);
                colliderParts.container = parent;

                assert(transform.childCount >= 1, "We expect the parent object to contain the car model.");
                carModelTransform = transform.GetChild(0);
            }

            // Wheels
            {
                var wheels = carModelTransform.Find("wheels");
                assert(wheels != null, "Must have a `wheels` child with wheels.");

                var outWheelColliderParts = new CarPart<WheelCollider>[4];
                var outWheelVisualParts = new Transform[4];
                CreateWheelColliderGameObjectsFromWheelMeshes(
                    parent, wheels, wheelPrefab, outWheelColliderParts, outWheelVisualParts);
                colliderParts.wheels = outWheelColliderParts;
                visualParts.wheels = outWheelVisualParts;

                // 5. Make the wheel's collider into a prefab, because it has quite a lot of properties.
                static void CreateWheelColliderGameObjectsFromWheelMeshes(
                    Transform parent,
                    Transform visualWheelsContainer,
                    GameObject wheelColliderPrefab,
                    CarPart<WheelCollider>[] outColliderParts,
                    Transform[] outVisualParts)
                {
                    var wheelNames = WheelHelper.WheelNames;
                    
                    // Validation
                    {
                        assert(outColliderParts.Length == wheelNames.Length);
                        assert(visualWheelsContainer.childCount == outColliderParts.Length);
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
                        var firstWheel = visualWheelsContainer.GetChild(0);
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
                        var meshWheelTransform = visualWheelsContainer.Find(wheelName);

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

                        outColliderParts[i] = new CarPart<WheelCollider>
                        {
                            transform = wheelColliderTransform,
                            collider = wheelCollider,
                        };

                        outVisualParts[i] = meshWheelTransform;
                    }
                }
            }

            // Body BoxCollider
            {
                var bodyMeshTransform = carModelTransform.Find("body");
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

                visualParts.body = bodyMeshTransform;

                colliderParts.body = new CarPart<BoxCollider>()
                {
                    transform = bodyTransform,
                    collider = collider,
                };
            }
        }
        #endif
    }
}
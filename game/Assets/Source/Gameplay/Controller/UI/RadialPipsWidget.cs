using System;
using UnityEngine;
using UnityEngine.UI;

namespace Race.Gameplay
{
    [System.Serializable]
    public struct SpeedRange
    {
        public float minSpeed;
        public float maxSpeed;
    }

    [System.Serializable]
    public struct PipInfo
    {
        // This count is bugging me a little... it's the only thing that's going to change
        // depending on the speed ranges of the car.
        // So it should probably be separated out.
        public int count;
        
        /// <summary>
        /// The width of the pip, expressed in ~~radians~~ degrees.
        /// The actual pixel width of the pips will be dependent on the angle range.
        /// </summary>
        public float angleWidthDegrees;
        public float AngleWidth => Mathf.Deg2Rad * angleWidthDegrees;
        public float heightInRadii;
    }

    [System.Serializable]
    public struct PipConfiguration
    {
        public PipInfo largePipInfo;
        public PipInfo smallPipInfo;
        public float pipEdgeOffsetInRadii;
    }

    public class RadialPipsWidget : MaskableGraphic
    {
        [SerializeField] internal Texture _pipTexture;
        [SerializeField] internal RadialDisplayVisualConfiguration _visualConfiguration;
        [SerializeField] internal PipConfiguration _pipConfiguration;

        public ref PipConfiguration PipConfiguration => ref _pipConfiguration;

        /// <summary>
        /// Call after resetting PipConfiguration.
        /// </summary>
        public void InvalidatePipInfo()
        {
            SetVerticesDirty();
        }

        protected override void OnRectTransformDimensionsChange()
        {
            base.OnRectTransformDimensionsChange();
            SetVerticesDirty();
            SetMaterialDirty();
        }
        
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();

            if (_pipConfiguration.largePipInfo.count == 0)
                return;

            var transform = (RectTransform) this.transform;
            var bounds = transform.rect;
            var radius = Mathf.Min(bounds.yMax - bounds.yMin, bounds.xMax - bounds.xMin) / 2;
            float circumference = radius * 2 * Mathf.PI;

            float angleRangeLength = _visualConfiguration.AngleRangeLength;

            Vector2 GetPixelDimensions(in PipInfo pipInfo)
            {
                float normalizedWidth = pipInfo.AngleWidth / angleRangeLength;
                float width = circumference * normalizedWidth;
                float height = radius * pipInfo.heightInRadii;
                return new Vector2(width, height);
            }
            ref readonly var largePipInfo = ref _pipConfiguration.largePipInfo;
            ref readonly var smallPipInfo = ref _pipConfiguration.smallPipInfo;
            Vector2 largePipSize = GetPixelDimensions(largePipInfo);
            Vector2 smallPipSize = GetPixelDimensions(smallPipInfo);
            float pipEdgePixelOffset = _pipConfiguration.pipEdgeOffsetInRadii * radius;

            float numLargeSegmentsPerLargePip = largePipInfo.count + -1;
            float angleIncreasePerLargePip = _visualConfiguration.SignedAngleRangeLength / numLargeSegmentsPerLargePip;
            float numSmallSegmentsPerLargePip = smallPipInfo.count + 1;
            float angleIncreasePerSmallPip = angleIncreasePerLargePip / numSmallSegmentsPerLargePip;

            float distanceToLargePipCenterFromCircleCenter = radius - pipEdgePixelOffset - largePipSize.y / 2;
            float distanceToSmallPipCenterFromCircleCenter = radius - pipEdgePixelOffset - smallPipSize.y / 2;

            for (int i = 0; i < largePipInfo.count; i++)
            {
                float largePipAngle = _visualConfiguration.MinAngle + angleIncreasePerLargePip * i;
                AddPip(largePipSize, distanceToLargePipCenterFromCircleCenter, largePipAngle);

                if (i != largePipInfo.count - 1)
                {
                    for (int j = 1; j <= smallPipInfo.count; j++)
                    {
                        float smallPipAngle = largePipAngle + j * angleIncreasePerSmallPip;
                        AddPip(smallPipSize, distanceToSmallPipCenterFromCircleCenter, smallPipAngle);
                    }
                }

                void AddPip(Vector2 size, float distanceToCenter, float angle)
                {
                    Vector2 normal = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                    // rotated by 90 degrees counterclockwise.
                    Vector2 perpendicular = new Vector2(-normal.y, normal.x);

                    // Relative to center.
                    Vector2 pipOffset = normal * distanceToCenter;
                    Vector2 pipPosition = pipOffset + bounds.center;

                    Vector2 halfHeightVector = size.y / 2 * normal;
                    Vector2 halfWidthVector = size.x / 2 * perpendicular;

                    Span<Vector2> positions = stackalloc Vector2[4];
                    // top-left
                    positions[0] = pipPosition + halfHeightVector + halfWidthVector;
                    // top-right
                    positions[1] = pipPosition + halfHeightVector - halfWidthVector;
                    // bottom-right
                    positions[2] = pipPosition - halfHeightVector - halfWidthVector;
                    // bottom-left
                    positions[3] = pipPosition - halfHeightVector + halfWidthVector;

                    Span<Vector2> uvs = stackalloc Vector2[4];
                    uvs[0] = new Vector2(1, 1);
                    uvs[1] = new Vector2(1, 0);
                    uvs[2] = new Vector2(0, 0);
                    uvs[3] = new Vector2(0, 1);

                    int currentVertexIndex = vh.currentVertCount;

                    for (int i = 0; i < 4; i++)
                    {
                        var vertex = new UIVertex
                        {
                            uv0 = uvs[i],
                            position = positions[i],
                            color = this.color,
                        };

                        vh.AddVert(vertex);
                    }

                    // Clockwise winding order.
                    vh.AddTriangle(currentVertexIndex + 0, currentVertexIndex + 1, currentVertexIndex + 2);
                    vh.AddTriangle(currentVertexIndex + 0, currentVertexIndex + 2, currentVertexIndex + 3);
                }
            }
        }
    }
}

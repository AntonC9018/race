using System;
using EngineCommon;
using UnityEngine;
using UnityEngine.UI;

namespace Race.Gameplay
{
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

            var circle = ((RectTransform) transform).GetCircleInfo();
            var info = GetGeometryInfo(circle);
            var largePipsOffsetEnumerator = info.EnumerateLargePipOffsets();

            while (largePipsOffsetEnumerator.MoveNext())
            {
                var largeCurrent = largePipsOffsetEnumerator.Current;
                AddPip(info.largePipInfo.size, largeCurrent.offset, largeCurrent.normal);
                
                if (info.HasSmallPipsAt(largePipsOffsetEnumerator.Index))
                {
                    foreach (var smallInfo in info.EnumerateSmallPipOffsets(largeCurrent.angle))
                        AddPip(info.smallPipInfo.size, smallInfo.offset, smallInfo.normal);
                }
            }

            void AddPip(Vector2 size, Vector2 offset, Vector2 normal)
            {
                Vector2 pipPosition = offset + circle.center;
                Vector2 perpendicular = normal.RotateCounterClockwiseQuarterCircle();
                Vector2 heightVector = size.y * normal;
                Vector2 halfWidthVector = size.x / 2 * perpendicular;

                Span<Vector2> positions = stackalloc Vector2[4];
                // top-left
                positions[0] = pipPosition + heightVector + halfWidthVector;
                // top-right
                positions[1] = pipPosition + heightVector - halfWidthVector;
                // bottom-right
                positions[2] = pipPosition - halfWidthVector;
                // bottom-left
                positions[3] = pipPosition + halfWidthVector;

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

        // TODO:
        // Too much code required for this really simple iterator thing.
        // Now I really do want to use that span library.
        public struct PipOffsetEnumerator
        {
            internal float _distanceToBottomFromCircleCenter;
            internal CircleHelper.RadialInterpolation _radialInterpolationPrimitive;

            public PipOffsetEnumerator(float distanceToBottomFromCircleCenter, CircleHelper.RadialInterpolation radialInterpolationPrimitive)
            {
                _distanceToBottomFromCircleCenter = distanceToBottomFromCircleCenter;
                _radialInterpolationPrimitive = radialInterpolationPrimitive;
            }

            public PipOffsetEnumerator GetEnumerator() => this;

            public static PipOffsetEnumerator Empty => new PipOffsetEnumerator(0, CircleHelper.RadialInterpolation.Empty);

            public struct Result
            {
                public float angle;
                public Vector2 normal;
                public Vector2 offset;
            }
            public Result Current
            {
                get
                {
                    var angle = _radialInterpolationPrimitive.Current;
                    var normal = CircleHelper.GetNormal(angle);
                    return new Result
                    {
                        angle = angle,
                        normal = normal,
                        offset = normal * _distanceToBottomFromCircleCenter,
                    };
                }
            }
            public bool MoveNext() => _radialInterpolationPrimitive.MoveNext();
            public int Index => _radialInterpolationPrimitive.Index;
        }

        public struct PipPositioningInfo
        {
            public float minAngle;
            
            public struct Pip
            {
                public Vector2 size;
                public int count;
                public float angleIncrease;
                public float distanceToBottom;
            }
            public Pip largePipInfo;
            public Pip smallPipInfo;

            public PipOffsetEnumerator EnumerateLargePipOffsets()
            {
                return new PipOffsetEnumerator(
                    largePipInfo.distanceToBottom,
                    CircleHelper.InterpolateAngles(minAngle, largePipInfo.angleIncrease, largePipInfo.count));
            }

            public PipOffsetEnumerator EnumerateSmallPipOffsets(float fromAngle)
            {
                return new PipOffsetEnumerator(
                    smallPipInfo.distanceToBottom,
                    CircleHelper.InterpolateAngles(
                        fromAngle + smallPipInfo.angleIncrease, smallPipInfo.angleIncrease, smallPipInfo.count));
            }

            internal bool HasSmallPipsAt(int largePipIndex)
            {
                return largePipIndex != largePipInfo.count - 1;
            }
        }

        public PipPositioningInfo GetGeometryInfo(in CircleInfo circleInfo)
        {
            return _GetGeometryInfo(circleInfo, _visualConfiguration, _pipConfiguration);
        }

        public static PipPositioningInfo _GetGeometryInfo(
            in CircleInfo circleInfo,
            RadialDisplayVisualConfiguration visualConfiguration,
            in PipConfiguration pipConfiguration)
        {
            PipPositioningInfo result;

            float angleRangeLength = visualConfiguration.AngleRangeLength;
            float pipEdgePixelOffset = pipConfiguration.pipEdgeOffsetInRadii * circleInfo.radius;

            PipPositioningInfo.Pip GetInitialPipInfo(in PipInfo pipInfo, in CircleInfo circle)
            {
                PipPositioningInfo.Pip pip;

                float normalizedWidth = pipInfo.AngleWidth / angleRangeLength;
                float width = circle.radius * 2 * Mathf.PI * normalizedWidth;
                float height = circle.radius * pipInfo.heightInRadii;
                pip.size = new Vector2(width, height);

                pip.count = pipInfo.count;
                pip.distanceToBottom = circle.radius - pipEdgePixelOffset - pip.size.y;
                
                // Zeroed out, because uninitializing is not allowed in C#
                pip.angleIncrease = 0;
                
                return pip;
            }

            float angleIncreasePerLargePip;
            {
                var large = GetInitialPipInfo(pipConfiguration.largePipInfo, circleInfo);

                float numLargePipSegments = large.count + -1;
                angleIncreasePerLargePip = visualConfiguration.SignedAngleRangeLength / numLargePipSegments;
                large.angleIncrease = angleIncreasePerLargePip;

                result.largePipInfo = large;
            }

            {
                var small = GetInitialPipInfo(pipConfiguration.smallPipInfo, circleInfo);

                float numSmallSegmentsPerLargePip = small.count + 1;
                small.angleIncrease = angleIncreasePerLargePip / numSmallSegmentsPerLargePip;

                result.smallPipInfo = small;
            }

            result.minAngle = visualConfiguration.MinAngle;

            return result;
        }
    }
}

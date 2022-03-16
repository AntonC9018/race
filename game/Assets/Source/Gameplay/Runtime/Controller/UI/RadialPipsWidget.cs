using System;
using EngineCommon;
using UnityEngine;
using UnityEngine.UI;
using static EngineCommon.Assertions;

namespace Race.Gameplay
{
    [System.Serializable]
    public struct PipInfo
    {
        // This count is bugging me a little... it's the only thing that's going to change
        // depending on the speed ranges of the car.
        // So it should probably be separated out.
        [Min(0)]
        public int count;
        
        /// <summary>
        /// Expressed in the percentage from the space alloted to the segment the pip is in.
        /// </summary>
        [Range(0, 1)]
        public float width;

        [Range(0, 1)]
        public float heightInRadii;
    }

    [System.Serializable]
    public struct PipConfiguration
    {
        public PipInfo largePipInfo;
        public PipInfo smallPipInfo;

        [Range(0, 1)]
        public float pipEdgeOffsetInRadii;
    }

    public class RadialPipsWidget : MaskableGraphic
    {
        [SerializeField] internal Texture _pipTexture;
        [SerializeField] internal RadialDisplayVisualConfiguration _visualConfiguration;
        [SerializeField] internal PipConfiguration _pipConfiguration;
        public ref PipConfiguration PipConfiguration => ref _pipConfiguration;

        public override Texture mainTexture => _pipTexture != null ? s_WhiteTexture : _pipTexture;

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

        public readonly struct PipPositioningInfo
        {
            public readonly float minAngle;
            public readonly Pip largePipInfo;
            public readonly Pip smallPipInfo;
            public struct Pip
            {
                public Vector2 size;
                public int count;
                public float anglePerSegment;
                public float distanceToBottom;
            }

            public PipPositioningInfo(float minAngle, Pip largePipInfo, Pip smallPipInfo)
            {
                this.minAngle = minAngle;
                this.largePipInfo = largePipInfo;
                this.smallPipInfo = smallPipInfo;
            }

            public PipOffsetEnumerator EnumerateLargePipOffsets()
            {
                return new PipOffsetEnumerator(
                    largePipInfo.distanceToBottom,
                    CircleHelper.InterpolateAngles(minAngle, largePipInfo.anglePerSegment, largePipInfo.count));
            }

            public PipOffsetEnumerator EnumerateSmallPipOffsets(float fromAngle)
            {
                return new PipOffsetEnumerator(
                    smallPipInfo.distanceToBottom,
                    CircleHelper.InterpolateAngles(
                        fromAngle + smallPipInfo.anglePerSegment, smallPipInfo.anglePerSegment, smallPipInfo.count));
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
            float angleRangeLength = visualConfiguration.AngleRangeLength;
            float pipEdgePixelOffset = pipConfiguration.pipEdgeOffsetInRadii * circleInfo.radius;

            PipPositioningInfo.Pip GetPipInfo(
                in PipInfo pipInfo, in CircleInfo circle,
                float allotedAngle, int segmentCount)
            {
                assert(segmentCount > 0);

                PipPositioningInfo.Pip result;

                float anglePerSegment = allotedAngle / segmentCount;
                result.anglePerSegment = anglePerSegment;

                float angleWidth = pipInfo.width * anglePerSegment;
                float width = Mathf.Abs(circle.radius * angleWidth);
                float height = circle.radius * pipInfo.heightInRadii;
                result.size = new Vector2(width, height);

                result.count = pipInfo.count;
                result.distanceToBottom = circle.radius - pipEdgePixelOffset - result.size.y;
                
                return result;
            }

            PipPositioningInfo.Pip large;
            {
                ref readonly var a = ref pipConfiguration.largePipInfo;
                large = GetPipInfo(a, circleInfo,
                    allotedAngle: visualConfiguration.SignedAngleRangeLength,
                    segmentCount: Math.Max(a.count - 1, 1));
            }

            PipPositioningInfo.Pip small;
            {
                ref readonly var a = ref pipConfiguration.smallPipInfo;
                small = GetPipInfo(a, circleInfo,
                    allotedAngle: large.anglePerSegment,
                    segmentCount: a.count + 1);
            }

            return new PipPositioningInfo(visualConfiguration.MinAngle, large, small);
        }
    }
}

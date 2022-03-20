using UnityEngine;
using static EngineCommon.Assertions;
using EngineCommon;

namespace Race.Gameplay
{
    public class StraightTrack : IStaticTrack
    {
        private readonly Vector3 _startPoint;
        private readonly Vector3 _endPoint;
        private readonly float _roadWidth;

        public StraightTrack(Vector3 startPoint, Vector3 endPoint, float roadWidth)
        {
            _startPoint = startPoint;
            _endPoint = endPoint;
            _roadWidth = roadWidth;
        }

        public RoadSegment StartingSegment => 0;

        [System.Diagnostics.Conditional("UNITY_ASSERTIONS")]
        private static void AssertPointValid(RoadPoint point)
        {
            assert(point.IsInsideTrack);
            assert(point.segment == 0);
        }

        public Vector3 GetRoadCenterPointFromProgress(float progress)
        {
            Vector3 a = _endPoint - _startPoint;
            return progress * a;
        }

        public CurvatureInfo GetCurvatureAtRoadPoint(RoadPoint point)
        {
            AssertPointValid(point);

            // no turns
            Vector3 tangent = (_endPoint - _startPoint).normalized;
            return new CurvatureInfo(tangent, Vector3.positiveInfinity);
        }

        public Vector3 GetRoadMiddlePosition(RoadPoint point)
        {
            AssertPointValid(point);
            return (_endPoint - _startPoint) * point.position + _startPoint;
        }

        public RoadPoint GetRoadPointAt(Vector3 position)
        {
            Vector3 startToEnd = _endPoint - _startPoint;

            float progress;
            {
                Vector3 b = position - _startPoint;
                float p = Vector3.Dot(startToEnd, b);
                progress = p / startToEnd.magnitude;
            }
            
            // Is outside track?
            {
                Vector3 off = position - startToEnd * progress;
                if (off.magnitude > _roadWidth / 2)
                    return RoadPoint.CreateOutsideTrack(segment: 0);
            }

            // Clamping is part of contract.
            float clampedProgress = Mathf.Clamp01(progress);

            return new RoadPoint(segment: 0, clampedProgress);
        }

        public float GetTotalProgress(RoadPoint currentPoint)
        {
            AssertPointValid(currentPoint);
            return currentPoint.position;
        }

        public RoadPoint UpdateRoadPoint(RoadPoint point, Vector3 newPosition)
        {
            AssertPointValid(point);
            return GetRoadPointAt(newPosition);
        }

        public Vector3 GetRoadNormal(RoadPoint point)
        {
            var a = _startPoint;
            var b = _endPoint;
            
            // We assume roll = 0
            var perp = a.With(y: 0);
            
            var c = b - a;
            return Vector3.Cross(perp, c).normalized;
        }

        public Quaternion GetRegularRotation(RoadPoint point)
        {
            AssertPointValid(point);

            var tangent = (_endPoint - _startPoint).normalized;
            var projectedLength = tangent.With(y: 0).magnitude;
            var angle = Mathf.Atan2(tangent.y, projectedLength);
            var perp = _startPoint.With(y: 0).normalized;
            
            return Quaternion.AngleAxis(angle, perp);
        }
    }
}
using UnityEngine;
using static EngineCommon.Assertions;
using EngineCommon;

namespace Race.Gameplay
{
    public class StraightTrack : IStaticTrack
    {
        internal readonly Vector3 _startPosition;
        internal readonly Vector3 _endPosition;
        internal readonly float _roadWidth;

        public StraightTrack(Vector3 startPoint, Vector3 endPoint, float roadWidth)
        {
            _startPosition = startPoint;
            _endPosition = endPoint;
            _roadWidth = roadWidth;
        }

        public RoadSegment StartingSegment => 0;

        [System.Diagnostics.Conditional("UNITY_ASSERTIONS")]
        private static void AssertPointValid(RoadPoint point)
        {
            assert(point.IsInsideTrack);
            assert(point.segment == 0);
        }

        public CurvatureInfo GetCurvatureAtRoadPoint(RoadPoint point)
        {
            AssertPointValid(point);

            // no turns
            Vector3 tangent = (_endPosition - _startPosition).normalized;
            return new CurvatureInfo(tangent, Vector3.positiveInfinity);
        }

        public Vector3 GetRoadMiddlePosition(RoadPoint point)
        {
            AssertPointValid(point);
            return (_endPosition - _startPosition) * point.position + _startPosition;
        }

        public RoadPoint GetRoadPointAt(Vector3 position)
        {
            Vector3 startToEnd = _endPosition - _startPosition;
            Vector3 startToPos = position - _startPosition;

            float progress;
            {
                float p = Vector3.Dot(startToEnd, startToPos);
                progress = p / (startToEnd.sqrMagnitude);
            }
            
            // Is outside track?
            {
                Vector3 off = startToPos - startToEnd * progress;
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


        private Vector3 GetPerp(Vector3 diff)
        {
            return new Vector3(diff.z, 0, -diff.x);
        }

        public UnitVectors GetUnitVectors(RoadPoint point)
        {
            AssertPointValid(point);

            var a = _endPosition - _startPosition;
            var tangent = a.normalized;
            var perp = GetPerp(tangent);
            // I guess Unity uses the left hand for this, because it ends up flipped.
            var normal = Vector3.Cross(a, perp).normalized;

            return new UnitVectors(tangent, perp, normal);
        }

        public Quaternion GetRegularRotation(RoadPoint point)
        {
            var vecs = GetUnitVectors(point);
            return Quaternion.LookRotation(vecs.tangent, vecs.normal);
        }
    }
}
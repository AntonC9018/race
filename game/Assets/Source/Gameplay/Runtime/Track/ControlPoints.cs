using Kari.Plugins.DataObject;
using UnityEngine;

using RoadSegmentID = System.Int32;

namespace Race.Gameplay
{
    public static class TrackHelper
    {
        public static TrackRaceInfo CreateFromQuad(Transform quad, float actualToVisualWidthRatio)
        {
            var transform = quad.transform;
            var center = transform.position;
            var scale = transform.localRotation * transform.localScale;
            var length = scale.z;
            var width = scale.x;

            // hack: does not handle slopes
            var halfLengthVector = new Vector3(0, 0, length / 2);

            var startPoint = center - halfLengthVector;
            var endPoint = center + halfLengthVector;

            var visualWidth = width;
            var actualWidth = visualWidth * actualToVisualWidthRatio;

            return new TrackRaceInfo
            {
                actualWidth = actualWidth,
                visualWidth = visualWidth,
                track = new StraightTrack(startPoint, endPoint, actualWidth),
            };
        }

        public static Vector3 GetRoadNormal(this IStaticTrack track, RoadPoint point)
        {
            return track.GetUnitVectors(point).normal;
        }
    }

    public readonly struct UnitVectors
    {
        // aka forward
        public readonly Vector3 tangent;
        // aka right
        public readonly Vector3 perpendicular;
        // aka up
        public readonly Vector3 normal;

        public UnitVectors(Vector3 tangent, Vector3 perpendicular, Vector3 normal)
        {
            this.tangent = tangent;
            this.normal = normal;
            this.perpendicular = perpendicular;
        }

        public void Deconstruct(out Vector3 tangent, out Vector3 perpendicular, out Vector3 normal)
        {
            tangent = this.tangent;
            perpendicular = this.perpendicular;
            normal = this.normal;
        }
    }

    /// <summary>
    /// Allows querying connected road segments.
    /// The methods must be pure, aka always return the same results for the same inputs.
    /// </summary>
    public interface IStaticTrack
    {
        RoadSegment StartingSegment { get; }
        
        /// <summary>
        /// Use this function to get the road segment from the current position.
        /// If the position is outside the track in any direction, returns a point outside the track.
        /// Such a point may still contain the correct segment id, which you can check by testing the IsValid property.
        /// </summary>
        RoadPoint GetRoadPointAt(Vector3 position);

        /// <summary>
        /// The input point must be a valid point inside the track.
        /// </summary>
        Vector3 GetRoadMiddlePosition(RoadPoint point);

        /// <summary>
        /// The input point must be a valid point inside the track.
        /// </summary>
        UnitVectors GetUnitVectors(RoadPoint point);

        /// <summary>
        /// The input point must be a valid point inside the track.
        /// </summary>
        Quaternion GetRegularRotation(RoadPoint point);

        /// <summary>
        /// Tries updating the given point, such that if the point went off the current segment,
        /// the segment ID will be updated accordingly.
        /// The input point must be a valid point inside the track.
        /// Returns the same result as `GetRoadPointAt`, but is unambiguous, because of the extra given information.
        /// </summary>
        RoadPoint UpdateRoadPoint(RoadPoint point, Vector3 newPosition);

        /// <summary>
        /// `progress` is a number between 0 and 1.
        /// The input point must be a valid point inside the track.
        /// </summary>
        float GetTotalProgress(RoadPoint currentPoint);

        /// <summary>
        /// About curvature: https://www.wikiwand.com/en/Curvature
        /// The input point must be a valid point inside the track.
        /// </summary>
        CurvatureInfo GetCurvatureAtRoadPoint(RoadPoint point);
    }

    [DataObject]
    public readonly partial struct RoadSegment
    {
        public readonly RoadSegmentID id;

        public RoadSegment(RoadSegmentID id)
        {
            this.id = id;
        }

        public static implicit operator RoadSegment(RoadSegmentID id) => new RoadSegment(id);
        public static implicit operator RoadSegmentID(RoadSegment segment) => segment.id;
    }

    public readonly struct RoadPoint
    {
        /// <summary>
        /// Opaque ID used to speed up queries.
        /// </summary>
        public readonly RoadSegment segment;

        // TODO:
        // It's not clear whether this representation will be convenient for every implementation.
        // Might want to make the interface below generic.

        /// <summary>
        /// The position within this road segment, normalized between 0 and 1.
        /// </summary>
        public readonly float position;

        public bool IsOutsideTrack => position < 0;
        public bool IsInsideTrack => !IsOutsideTrack;
        public bool IsValid => segment != (RoadSegmentID)(-1);

        public RoadPoint(RoadSegment segment, float position)
        {
            this.segment = segment;
            this.position = position;
        }

        public static RoadPoint CreateOutsideTrack(int segment = -1)
        {
            return new RoadPoint(segment, -1);
        }

        public static RoadPoint CreateStartOf(int segment)
        {
            return new RoadPoint(segment, 0);
        }

        public static RoadPoint CreateEndOf(int segment)
        {
            return new RoadPoint(segment, 1);
        }

        public static bool operator>(RoadPoint a, RoadPoint b)
        {
            if (a.segment > b.segment)
                return true;

            return a.segment == b.segment && a.position > b.position;
        }
     
        public static bool operator<(RoadPoint a, RoadPoint b)
        {
            if (a.segment < b.segment)
                return true;
                
            return a.segment == b.segment && a.position < b.position;
        }
    }

    public readonly struct CurvatureInfo
    {
        // Should this always be normalized?
        public readonly Vector3 tangent;
        public readonly Vector3 perp;

        public float Curvature => perp.magnitude;

        public CurvatureInfo(Vector3 tangent, Vector3 perp)
        {
            this.tangent = tangent;
            this.perp = perp;
        }
    }
}
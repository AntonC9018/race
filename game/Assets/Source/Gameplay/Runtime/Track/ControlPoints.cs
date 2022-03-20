using Kari.Plugins.DataObject;
using UnityEngine;

using RoadSegmentID = System.Int32;

namespace Race.Gameplay
{
    /// <summary>
    /// Allows querying connected road segments.
    /// The methods must be pure, aka always return the same results for the same inputs.
    /// </summary>
    public interface IStaticTrack
    {
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
        Vector3 GetRoadNormal(RoadPoint point);

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
        public static implicit operator RoadSegmentID(RoadSegment id) => id;
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
            return new RoadPoint(0, -1);
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
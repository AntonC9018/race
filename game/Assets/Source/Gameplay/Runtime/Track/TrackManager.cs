using UnityEngine;

namespace Race.Gameplay
{
    public interface IPosition
    {
        Vector3 Position { get; }
    }

    public class TrackManager
    {
        private RoadPoint[] _participantPositions;
        private RoadPoint[] _participantCheckpoints;
        private IStaticTrack _track;

        public Vector3 GetCheckpointPosition(int participantIndex)
        {
            return _track.GetRoadMiddlePosition(_participantCheckpoints[participantIndex]);
        }

        public bool IsUpsideDown(int participantIndex, Transform participantTransform)
        {
            var location = _participantPositions[participantIndex];
            Vector3 roadNormal = _track.GetRoadNormal(location);
            float upAmount = Vector3.Dot(participantTransform.up, roadNormal);
            const float requiredUpAmount = -0.2f;
            return upAmount < requiredUpAmount;
        }

        public readonly struct UpdateInfo
        {
            public readonly bool shouldReturnToCheckpoint;

            public UpdateInfo(bool shouldReturnToCheckpoint)
            {
                this.shouldReturnToCheckpoint = shouldReturnToCheckpoint;
            }
        }

        public UpdateInfo UpdatePosition(int participantIndex, Transform participantTransform)
        {
            ref var location = ref _participantPositions[participantIndex];
            if (location.IsOutsideTrack)
                return new UpdateInfo(shouldReturnToCheckpoint: true);

            location = _track.UpdateRoadPoint(location, participantTransform.position);
            if (location.IsOutsideTrack)
                return new UpdateInfo(shouldReturnToCheckpoint: true);

            return new UpdateInfo(shouldReturnToCheckpoint: IsUpsideDown(participantIndex, participantTransform));
        }

        public void ReturnToCheckpoint(int participantIndex, out Vector3 newPosition, out Quaternion newRotation)
        {
            ref var location = ref _participantPositions[participantIndex];
            var checkpoint = _participantCheckpoints[participantIndex];
            
            location = checkpoint;
            
            newPosition = _track.GetRoadMiddlePosition(checkpoint);
            newRotation = _track.GetRegularRotation(checkpoint);
        }
    }
}
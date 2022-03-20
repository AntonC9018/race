using System;
using UnityEngine;

namespace Race.Gameplay
{
    public interface IPosition
    {
        Vector3 Position { get; }
    }

    public class TrackManager : MonoBehaviour
    {
        private LowLevelTrackManager _underlyingManager;
        private Vector3 initialPosition;


        public void Initialize(Transform playerTransform, CarProperties playerCarProperties)
        {
            var track = MakeTrack();
            IStaticTrack MakeTrack()
            {
                var transform = this.transform;
                var center = transform.position;
                var scale = transform.rotation * transform.localScale;
                var length = scale.z;
                var width = scale.x;

                // hack: does not handle slopes
                var halfLengthVector = new Vector3(0, 0, length / 2);

                var startPoint = center - halfLengthVector;
                var endPoint = center + halfLengthVector;

                return new StraightTrack(startPoint, endPoint, width);
            }

            _underlyingManager = new LowLevelTrackManager();
            _underlyingManager.Reset(track, 1);

            {
                _underlyingManager.GetPositionAndRotation(participantIndex: 0, out var p, out var r);
                CarDataModelHelper.RestartCar(playerTransform, playerCarProperties, p, r);
            }
        }
    }

    public class LowLevelTrackManager
    {
        internal RoadPoint[] _participantPositions;
        internal RoadPoint[] _participantCheckpoints;
        internal IStaticTrack _track;

        public void Reset(IStaticTrack track, int count)
        {
            Array.Resize(ref _participantPositions, count);
            Array.Resize(ref _participantCheckpoints, count);
            _track = track;

            {
                var p = new RoadPoint(track.StartingSegment, 0);
                for (int i = 0; i < count; i++)
                {
                    _participantCheckpoints[i] = p;
                    _participantPositions[i] = p;
                }
            }
        }

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

            bool isUpsideDown = IsUpsideDown(participantIndex, participantTransform);
            if (!isUpsideDown)
            {
                // TODO: maybe snap
                _participantCheckpoints[participantIndex] = location;
            }
                
            return new UpdateInfo(shouldReturnToCheckpoint: isUpsideDown);
        }

        public void GetPositionAndRotation(int participantIndex, out Vector3 position, out Quaternion rotation)
        {
            var location = _participantPositions[participantIndex];
            position = _track.GetRoadMiddlePosition(location);
            rotation = _track.GetRegularRotation(location);
        }

        public void ReturnToCheckpoint(int participantIndex)
        {
            _participantPositions[participantIndex] = _participantCheckpoints[participantIndex];
        }
    }
}
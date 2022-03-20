using EngineCommon;
using UnityEngine;

using static EngineCommon.Assertions;

namespace Race.Gameplay
{
    public interface ICarPlacementStrategy
    {
        void Reset(IStaticTrack track, float trackWidth, ParticipantInfo[] participants);
        (Vector3 position, Quaternion rotation) PlaceCar(int carIndex);
    }

    public class GridPlacementStrategy : ICarPlacementStrategy
    {
        private Vector2 _cellSize;
        private int _colCount;

        // Assume the start is linear.
        // The system cannot handle arbitrary turns and road segment sizes without
        // any assumptions of the underlying track.
        private Vector3 _startingPosition;
        private Vector3 _tangentDirection;
        private Vector3 _perpendicularDirection;
        private Quaternion _startingRotation;

        public void Reset(IStaticTrack track, float trackWidth, ParticipantInfo[] participants)
        {
            assert(track is not null);
            assert(participants is not null);

            {
                var start = RoadPoint.CreateStartOf(track.StartingSegment);
                var rotation = track.GetRegularRotation(start);
                var position = track.GetRoadMiddlePosition(start);

                _startingPosition = position - trackWidth / 2 * _perpendicularDirection;
                _startingRotation = rotation;
                _tangentDirection = rotation * Vector3.forward;
                _perpendicularDirection = rotation * Vector3.right;
            }

            float maxWidth = 0;
            {
                float maxLength = 0;
                for (int i = 0; i < participants.Length; i++)
                {
                    ref readonly var participant = ref participants[i];
                    var size = participant.carProperties.DataModel.ColliderParts.body.collider.size;

                    maxWidth = Mathf.Max(size.x, maxWidth);
                    maxLength = Mathf.Max(size.z, maxLength);
                }

                int numCarsPerRow = Mathf.FloorToInt(trackWidth / maxWidth);
                if (numCarsPerRow == 0)
                    numCarsPerRow = 1;

                _colCount = numCarsPerRow;

                float lengthEachCellUntilMultiple = (trackWidth % maxWidth) / numCarsPerRow;
                _cellSize = new Vector2(maxWidth + lengthEachCellUntilMultiple, maxLength);
                // _rowCount = MathHelper.CeilDivide(participants.Length, numCarsPerRow);
            }
        }

        public (Vector3 position, Quaternion rotation) PlaceCar(int carIndex)
        {
            int row = carIndex / _colCount;
            int column = carIndex % _colCount;

            Vector3 position = _startingPosition;
            position += _cellSize.y * row * _tangentDirection;
            position += _cellSize.x * column * _perpendicularDirection;

            return (position, _startingRotation);
        }
    }
}